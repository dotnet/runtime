using System;
using System.Linq;
using System.IO;

namespace ExceptionFilterTestLauncher
{
    class Program
    {
        public static object newObject = null;
        public static bool SubTest(int i)
        {
            return newObject.GetHashCode() == i;
        }

        public static bool HandleException(Exception e)
        {
            return true;
        }

        public static bool Test2(int i)
        {
            bool test = true;
            try
            {
                test = SubTest(i);
            }
            catch (Exception e) when (!HandleException(e))
            {
            }
            return test;
        }

        static void Main(string[] args)
        {
            try
            {
                bool result = Test2(12345);
            }
            catch (Exception e)
            {
                // Before bug 46661 was fixed, the when would cut the stack trace, so Test(int) wouldn't show up
                if(!e.StackTrace.Contains("SubTest"))
                    throw new Exception("Stack trace doesn't reference SubTest function. Current stacktrace is " + e.StackTrace.ToString());
                else
                    // Correct result
                    Environment.Exit(0);
            }
            throw new Exception("Exception should have been caught!");
        }
    }
}