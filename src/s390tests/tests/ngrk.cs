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
        long a = 0xFFL;
        long b = 0x0FL;
        return (a & b) == 0x0FL ? 0 : 12;
    }
}
