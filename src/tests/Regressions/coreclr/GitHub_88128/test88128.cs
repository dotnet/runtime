using System;
using System.Reflection;

public class test88113
{
    public static int Main()
    {
        try
        {
            throw new Exception("boo");
        }
        catch (Exception ex2)
        {
            Console.WriteLine($"1: {ex2}");
            try
            {
                throw;
            }
            catch (Exception ex3)
            {
                Console.WriteLine($"2: {ex3}");
            }
        }

        return 100;
    }
}
