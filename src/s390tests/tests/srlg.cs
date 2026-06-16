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
        ulong a = 0x8000000000000000UL;
        return (a >> 1) == 0x4000000000000000UL ? 0 : 54;
    }
}
