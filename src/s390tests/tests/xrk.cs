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
        int b = 0xAA;
        return (a ^ b) == 0x55 ? 0 : 15;
    }
}
