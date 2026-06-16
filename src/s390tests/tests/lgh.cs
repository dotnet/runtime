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
        short a = 0x1234;
        long  r = a;
        return r == 0x1234L ? 0 : 28;
    }
}
