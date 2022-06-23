using System;

namespace TestInterface
{
    public interface Interface
    {
        void Test();
    }

    public class Class
    {
        public static void Test()
        {
            Console.WriteLine("TestInterface.Class.Test");
        }
    }
}