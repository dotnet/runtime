// C# Hello World

using System;

public class Test
{
    public static void TestRun()
    {
        int i;
        int len = 50;
        var rand = new Random();
        while (len != 0)
        {
            i = rand.Next(0, 1);
            len /= 2;
        }
    }
    
    public static void Main()
    {
        TestRun();
    }
}