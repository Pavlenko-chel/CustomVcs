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
    }}