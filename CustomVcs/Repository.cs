using System.Security.Cryptography;
using System.Text;

namespace CustomVcs
{
    public class Repository
    {
        private readonly string _repoDir;
        private readonly string _objectsDir;
        private readonly string _commitsDir;
        private readonly string _indexFilePath;
        private readonly string _headFilePath;
        private readonly string _logFilePath;
        public Repository(string repoDir)
        {
            _repoDir = repoDir;
            _objectsDir = Path.Combine(_repoDir,
            "objects");
            _commitsDir = Path.Combine(_repoDir,
            "commits");
            _indexFilePath = Path.Combine(_repoDir,
            "index.txt");
            _headFilePath = Path.Combine(_repoDir,
            "head.txt");
            _logFilePath = Path.Combine(_repoDir,
            "log.txt");
        }

        public void Init()
        {
            if (Directory.Exists(_repoDir))
            {
                Console.WriteLine($"Repository {_repoDir} already is initialized");
                return;
            }
            Directory.CreateDirectory(_repoDir);
            Directory.CreateDirectory(_objectsDir);
            Directory.CreateDirectory(_commitsDir);
            File.WriteAllText(_indexFilePath, string.Empty);
            File.WriteAllText(_headFilePath, string.Empty);
            File.WriteAllText(_logFilePath, string.Empty);
            Console.WriteLine($"Repository {_repoDir} has been initialized");
        }

        /// <summary>
        /// Проверяет, существует ли директория репозитория.
        /// Если директория не найдена, выбрасывает исключение.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private void EnsureInitialized()
        {
            if (!Directory.Exists(_repoDir))
                throw new Exception($"{_repoDir} not found");
        }

        private static string ComputeHash(byte[] data)
        {
            using (var sha = SHA1.Create())
            {
                var hashBytes = sha.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-","").ToLower();
            }
        }

        public void Add(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} not found");
            EnsureInitialized();
            var content = File.ReadAllBytes(filePath);
            var hash = ComputeHash(content);
            var blobDir = Path.Combine(_objectsDir, hash[..2]);
            var blobPath = Path.Combine(blobDir, hash[2..]);
            var indexLines = new List<string>();
            indexLines.AddRange(File.ReadAllLines(_indexFilePath));
            var index = indexLines.FindIndex(l => l.StartsWith(filePath + " "));
            if (index > 0 && indexLines[index].Contains($"{filePath} {hash}"))
                return;
            if (index == -1)
                indexLines.Add($"{filePath} {hash}");
            else
                indexLines[index] = $"{filePath} {hash}";
            File.WriteAllLines(_indexFilePath, indexLines);
            if (!Directory.Exists(blobDir))
            {
                Directory.CreateDirectory(blobDir);
                File.WriteAllBytes(blobPath, content);
            }
        }
        public void Commit(string message)
        {
            EnsureInitialized();
            if (!File.Exists(_indexFilePath))
                throw new FileNotFoundException("Index is missing, nothing to commit");
                var indexLines = File.ReadAllLines(_indexFilePath);
            if (indexLines.Length == 0)
            {
                Console.WriteLine("Nothing to commit");
                return;
            }
            // Шаг 1: Загрузка родительского tree
            var currentTree = LoadParentTree();
            // Шаг 2: Применение изменений из index (добавления/изменения)
            ApplyIndexChanges(currentTree, indexLines);
            // Шаг 3: Обнаружение и удаление удалённых файлов
            RemoveDeletedFiles(currentTree, indexLines);
            // Шаг 4: Создание нового tree
            var newTreeHash = CreateTreeObject(currentTree);
            // Шаг 5: Создание commit-объекта
            var commitHash = CreateCommitObject(newTreeHash, message);
            // Шаг 6: Обновление HEAD, очистка index и обновление log
            UpdateReferences(commitHash, message);
            Console.WriteLine($"Committed {commitHash}");
        }

        private Dictionary<string, string> LoadParentTree()
        {
            var currentTree = new Dictionary<string, string>();
            var parentHash = string.Empty;

            if (!File.Exists(_headFilePath))
                return currentTree;

            parentHash = File.ReadAllText(_headFilePath).Trim();

            if (string.IsNullOrEmpty(parentHash))
                return currentTree;

            var parentCommitPath = Path.Combine(_commitsDir, parentHash);

            if (!File.Exists(parentCommitPath))
                return currentTree;

            var parentCommitLines = File.ReadAllLines(parentCommitPath);
            var parentTreeHash = ExtractTreeHash(parentCommitLines);

            if (string.IsNullOrEmpty(parentTreeHash))
                return currentTree;

            var parentTreePath = Path.Combine(_commitsDir, parentTreeHash);

            if (!File.Exists(parentTreePath))
                return currentTree;

            var parentTreeLines = File.ReadAllLines(parentTreePath);
            for (int i = 0; i < parentTreeLines.Length; i++)
            {
                string? treeLine = parentTreeLines[i];
                var parts = treeLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    continue;

                var fileName = parts[0];
                var blobHash = parts[1];
                currentTree[fileName] = blobHash;
            }

            return currentTree;
        }

        private void ApplyIndexChanges(Dictionary<string, string> currentTree, string[] indexLines)
        {
            foreach (var indexLine in indexLines)
            {
                var parts = indexLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    continue;

                var fileName = parts[0];
                var blobHash = parts[1];

                currentTree[fileName] = blobHash;
            }
        }

        private void RemoveDeletedFiles(Dictionary<string, string> currentTree, string[] indexLines)
        {
            var parentTreeFiles = new HashSet<string>(currentTree.Keys);

            var workingDirectoryFiles = Directory.GetFiles(Directory.GetCurrentDirectory())
                .Select(Path.GetFileName)
                .Where(f => !f.StartsWith(".mygit"))
                .ToHashSet();

            foreach (var file in parentTreeFiles.ToList())
            {
                if (!workingDirectoryFiles.Contains(file) && !indexLines.Any(line => line.StartsWith(file + " ")))
                {
                    currentTree.Remove(file);
                    Console.WriteLine($"File deleted: {file}");
                }
            }
        }

        private string CreateTreeObject(Dictionary<string, string> currentTree)
        {
            var newTreeLines = currentTree.Select(kvp => $"{kvp.Key} {kvp.Value}");
            var newTreeContent = string.Join("\n", newTreeLines);
            var newTreeBytes = Encoding.UTF8.GetBytes(newTreeContent);
            var newTreeHash = ComputeHash(newTreeBytes);
            var newTreePath = Path.Combine(_commitsDir, newTreeHash);

            if (!File.Exists(newTreePath))
                File.WriteAllBytes(newTreePath, newTreeBytes);

            return newTreeHash;
        }

        private string CreateCommitObject(string treeHash, string message)
        {
            var parentHash = File.ReadAllText(_headFilePath).Trim();

            var sb = new StringBuilder();
            sb.AppendLine($"tree {treeHash}");
            if (!string.IsNullOrEmpty(parentHash))
                sb.AppendLine($"parent {parentHash}");
            sb.AppendLine($"date {DateTime.UtcNow.ToString("u")}");
            sb.AppendLine($"message {message}");

            var commitBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var commitHash = ComputeHash(commitBytes);
            var commitPath = Path.Combine(_commitsDir, commitHash);

            if (!File.Exists(commitPath))
                File.WriteAllBytes(commitPath, commitBytes);

            return commitHash;
        }

        private void UpdateReferences(string commitHash, string message)
        {
            File.WriteAllText(_headFilePath, commitHash);
            File.WriteAllText(_indexFilePath, "");
            File.AppendAllText(_logFilePath, $"{commitHash} {message} {DateTime.UtcNow:u}\n");
        }

        private string ExtractTreeHash(string[] commitLines)
        {
            foreach (var line in commitLines)
            {
                if (line.StartsWith("tree "))
                    return line.Substring(5).Trim();
            }

            return string.Empty;
        }
    }
}