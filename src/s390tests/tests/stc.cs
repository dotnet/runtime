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
        byte a = 0x42;
        return a == 0x42 ? 0 : 34;
    }
}
