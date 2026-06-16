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
        long a = 100L;
        long b = 100L;
        long c = 50L;
        return (a == b && a > c) ? 0 : 44;
    }
}
