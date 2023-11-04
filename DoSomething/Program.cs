using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoSomething
{
    internal class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(i);
                System.Threading.Thread.Sleep(200);
                //if (i == 6)
                //{
                //    throw new Exception("intended");
                //}
                //Task.Delay(TimeSpan.FromMilliseconds(200)).Wait();
            }
        }
    }
}
