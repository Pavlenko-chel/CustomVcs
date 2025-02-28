namespace CustomVcs
{
    public class Program
    {
        private const string RepoDir = ".CustomVcs";
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }
            var repository = new Repository(RepoDir);
            var command = Parse(args[0]);
            try
            {
                switch (command)
                {
                    case CommandType.Init:
                        repository.Init();
                        break;
                    case CommandType.Add:
                        repository.Add(args[1]);
                        break;
                    case CommandType.Commit:
                        var message = GetMessage(args);
                        repository.Commit(message);
                        break;
                    case CommandType.Checkout:
                        repository.Checkout(args[1]);
                        break;
                    case CommandType.Log:
                        repository.ShowLog();
                        break;
                    case CommandType.Print:
                        repository.PrintInfo();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        public static CommandType Parse(string command)
        {
            return command.ToLower() switch
            {
                "init" => CommandType.Init,
                "add" => CommandType.Add,
                "commit" => CommandType.Commit,
                "log" => CommandType.Log,
                "checkout" => CommandType.Checkout,
                "printinfo" => CommandType.Print,
            };
        }

        private static string GetMessage(string[] args)
        {
            var message = string.Empty;

            // Проверяем, что аргументов достаточно
            if (args.Length >= 3 && args[1] == "-m")
            {
                // Сообщение коммита может быть в кавычках
                // Проверяем, начинается ли сообщение с кавычек
                if (args[2].StartsWith("\"") && args[2].EndsWith("\""))
                {
                    // Убираем кавычки
                    message = args[2].Trim('"');
                }
                else
                {
                    // Если кавычек нет, то просто берем аргумент как есть
                    message = args[2];
                }
            }

            return message;
        }

    }
}