using System;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public String Test(int val)
    {
        return ((Object) val).ToString();
    }

    static int Main(string[] args)
    {
        int exitStatus = -1;

        Program p = new Program();

        String result = p.Test(42);
	if (result == "42")
        {
            Console.WriteLine("=== PASSED ===");
            exitStatus = 100;
        }
        else
        {
            Console.WriteLine("result should be 42, is= " + result);
            Console.WriteLine("+++ FAILED +++");
        }
        return exitStatus;
    }
}
