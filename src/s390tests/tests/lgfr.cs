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
        int  a = -1;
        long r = (long)a;
        return r == -1L ? 0 : 88;
    }
}
