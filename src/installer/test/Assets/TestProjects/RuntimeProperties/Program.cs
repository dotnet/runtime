using System;

namespace RuntimeProperties
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));

            foreach (string propertyName in args)
            {
                Console.WriteLine($"AppContext.GetData({propertyName}) = {System.AppContext.GetData(propertyName)}");
            }
        }
    }
}
