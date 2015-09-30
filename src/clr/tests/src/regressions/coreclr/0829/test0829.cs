using System;

public class Test
{
    public static int Main()
    {
        try
        {
            decimal i = new decimal(Single.MaxValue);

            Console.WriteLine("!!!ERROR-001: Expected exeption not thrown. Result's value: " + i.ToString());
            Console.WriteLine("FAIL");
            return 99;
        }
        catch (OverflowException)
        {
            Console.WriteLine("Pass");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("!!!ERROR-XXX: Unexpected exception : " + e);
            Console.WriteLine("FAIL");
            return 101;
        }
    }
}