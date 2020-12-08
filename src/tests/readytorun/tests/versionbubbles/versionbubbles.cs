using System;

public class Program 
{
    public static int Main() 
    {
        return RunTest();
    }

    public static int RunTest()
    {

        Helper helper = new Helper();
        string lastMethodName = String.Empty;

        lastMethodName = helper.GetLastMethodName();

        if((System.Environment.GetEnvironmentVariable("LargeVersionBubble") == null))
        {
            // Cross-Assembly inlining is only allowed in multi-module version bubbles
            Console.WriteLine("Large Version Bubble is disabled.");
            Console.WriteLine("PASS");
            return 100;
        }
        
        Console.WriteLine("Large Version Bubble is enabled.");
        // Helper returns the name of the method in the last stack frame
        // Check to see if the method has been inlined cross-module
        if (lastMethodName != "GetLastMethodName")
        {
            // method in helper.cs has been inlined
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL");
            return 101;
        }
    }
}
