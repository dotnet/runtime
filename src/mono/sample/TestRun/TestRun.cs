using System;
using System.Threading.Tasks;
namespace TestRun
{
    class TestRun{
        static void test1()
        {
            throw new TaskCanceledException();
        }

        static void test()
        {
            try{
                test1();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in Test");
            }
            throw new TaskCanceledException();
        }

        static void Main(){
            try{
                test();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error");
            }
        }
    }
}