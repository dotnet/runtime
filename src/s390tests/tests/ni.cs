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
        int a = 0xFF;
        return (a & 0x0F) == 0x0F ? 0 : 21;
    }
}
