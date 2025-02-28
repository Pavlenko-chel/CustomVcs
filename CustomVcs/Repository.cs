using System.Security.Cryptography;

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
    }}