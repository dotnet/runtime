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
        int a = 0x1200;
        return (a | 0x00FF) == 0x12FF ? 0 : 18;
    }
}
