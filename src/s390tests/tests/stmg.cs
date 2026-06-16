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
        long b = 200L;
        long c = 300L;
        return (a == 100L && b == 200L && c == 300L) ? 0 : 41;
    }
}
