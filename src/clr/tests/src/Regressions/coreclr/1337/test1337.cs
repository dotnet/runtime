using System;
using System.Text;

public class Test
{
    public static int Main()
    {
        try
        {
            Encoding myEncoding = Encoding.GetEncoding("foo");

            Console.WriteLine("!!!ERROR-001: Encoding created unexpectedly. Expected: ArgumentException, Actual: " + myEncoding.WebName);
            Console.WriteLine("FAIL");
            return 99;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("Pass");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("!!!ERROR-002: Unexpected exception : " + e);
            Console.WriteLine("FAIL");
            return 101;
        }
    }
}