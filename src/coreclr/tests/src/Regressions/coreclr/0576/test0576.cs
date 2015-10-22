using System;
using System.Globalization;

public class Test
{
    public static int Main()
    {
        try
        {
            CultureInfo ci = new CultureInfo("hu-HU");

            string str1 = "Foobardzsdzs";
		    string str2 = "rddzs";
            int result = ci.CompareInfo.IndexOf(str1, str2, CompareOptions.Ordinal);
            int expected = -1;

            if (result != expected)
            {
                Console.WriteLine("!!!ERROR-001: Unexpected index. Expected: " + expected.ToString() + ", Actual: " + result.ToString());
                Console.WriteLine("FAIL");
                return 98;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("!!!ERROR-XXX: Unexpected exception : " + e);
            Console.WriteLine("FAIL");
            return 101;
        }
        Console.WriteLine("Pass");
        return 100;
    }
}