using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumpyBase;

namespace StatsModels
{
    public class t1
    {
        public int attr1 { set; get; }
        public int attr2;
        public string str;
        public t1(int a = 1,int b =2) 
        {
            this.attr1 = a;
            this.attr2 = b;
            this.str = "hello world";
        }        
    }

    public class Test
    {
        static void Main()
        {
            t1 test1 = new t1();

            var pi = test1.GetType().GetProperty("attr1");
            var pi2 = test1.GetType().GetMember("attr1");
            var pi3 = test1.GetType().GetMember("attr2");
            if(pi!=null){Console.WriteLine("1 is Yes");}
            if (pi2 != null) { Console.WriteLine("1 is Yes"); }
            if (pi3 != null) { Console.WriteLine("2 is Yes"); }

            Console.WriteLine("\nTEST DONE, Press any key to exit.");
            Console.ReadKey();
        }
    }
}
