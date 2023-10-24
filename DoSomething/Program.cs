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
                System.Threading.Thread.Sleep(2000);
                //Task.Delay(TimeSpan.FromMilliseconds(200)).Wait();
            }
        }
    }
}
