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
        long a = 1000L;
        long b = 10L;
        return (a / b) == 100L ? 0 : 77;
    }
}
