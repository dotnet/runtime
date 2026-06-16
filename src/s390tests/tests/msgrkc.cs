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
        long a = 10L;
        long b = 12L;
        return (a * b) == 120L ? 0 : 6;
    }
}
