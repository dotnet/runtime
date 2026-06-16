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
        int a = 0xF0;
        int b = 0x0F;
        return (a | b) == 0xFF ? 0 : 13;
    }
}
