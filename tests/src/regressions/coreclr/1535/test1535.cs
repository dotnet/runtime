using System;
using System.Globalization;

public class Test
{
    public static int Main()
    {
        try
        {
            string str = "16:24 AM";
            DateTime dt = DateTime.ParseExact(str, "HH:mm tt", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None);

            Console.WriteLine("!!!ERROR-001: Test parsed unexpectedly. Expected: FormatException, Actual: " + dt.ToString());
            Console.WriteLine("FAIL");
            return 99;
        }
        catch (FormatException)
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