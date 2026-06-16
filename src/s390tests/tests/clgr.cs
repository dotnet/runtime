using System;

public class S390xInstructionTest
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        ulong a = 100UL;
        ulong b = 50UL;
        return (a > b) ? 0 : 46;
    }
}
