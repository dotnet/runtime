using System;

public class S390xStructTest
{
    public enum Level
    {
        Low,      // 0
        Medium,   // 1
        High      // 2
    }

    public static Level s390xHw()
    {
        return Level.Medium;
    }

    public static int Main()
    {
        Level result  = s390xHw();
        Console.WriteLine(result);
        return (int)result == 1 ? 0 : 1; 
    }
}

