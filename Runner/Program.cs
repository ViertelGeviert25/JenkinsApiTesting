namespace Runner
{
    public class Program
    {
        private static void Log(string message)
        {
            var fs = File.Open(@"H:\testdata\jenkindoutput.log", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using StreamWriter w = new StreamWriter(fs);
            w.BaseStream.Seek(0, SeekOrigin.End);
            message = DateTime.Now.ToString() + "  " + message;
            Console.WriteLine(message);
            w.WriteLine(message);
            w.Close();
        }

        public static int DoSomething(string[] cliArgs)
        {
            //Log("Doing something... ");
            var exitCode = 0;

            var taskId = cliArgs.FirstOrDefault(a => a.StartsWith("--taskId=")) ?? string.Empty;
            //if (taskId == null)
            //{
            //    Console.WriteLine($"Exit code is {exitCode}");
            //    Log($"{taskId} finished!");
            //    return exitCode;
            //}
            //else
            //{
            taskId = taskId.Substring(taskId.IndexOf("--taskId=") + 9);
            Log($"Task id is {taskId}. Doing something...");
            var random = new Random();
            var secs = random.Next(3, 15);
            Log($"taskid {taskId} waiting for {secs} seconds");
            //System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5)).Wait(); 
            //System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(secs)).Wait();
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(secs));
            //}

            //var retry = cliArgs.FirstOrDefault(a => a.StartsWith("--retry="));
            //if (!string.IsNullOrEmpty(retry))
            //{
            //    var retryCount = int.Parse(retry.Substring(retry.IndexOf("--retry=") + 8));
            //    if (retryCount == 3)
            //    {
            //        Log($"{taskId} finished!");
            //        return 0;
            //    }
            //}

            //if (taskId == "000037380621")
            //{
            //    Console.WriteLine($"Exit code is {11}");
            //    Log($"{taskId} finished!");
            //    Console.WriteLine($"Exit code is {11}");
            //    return 11; // simulate connection error
            //}

            //_ = int.TryParse($"{taskId.LastOrDefault()}", out exitCode);
            //Console.WriteLine($"Exit code is {exitCode}");
            exitCode = 0;
            Log($"{taskId} finished!");
            return exitCode;
        }

        public static void GetRandomHexStrings()
        {
            var random = new Random();
            var hexStrList = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                int num = random.Next();
                string hexString = "0000" + num.ToString("X") + ((i % 4) == 0 ? "0" : string.Empty);
                hexStrList.Add(hexString);
            }
            Console.WriteLine(string.Join(",", hexStrList));
        }

        public static int Main(string[] args)
        {
            return DoSomething(args);

            //GetRandomHexStrings();
            //Console.WriteLine();
            //Console.ReadKey();
            //return 0;
        }
    }
}
