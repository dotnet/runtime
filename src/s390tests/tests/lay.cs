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
        int base_  = 5;
        int offset = 3;
        return (base_ + offset) == 8 ? 0 : 89;
    }
}
