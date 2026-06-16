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
        long a = 111L;
        long b = 222L;
        long c = 333L;
        return (a == 111L && b == 222L && c == 333L) ? 0 : 42;
    }
}
