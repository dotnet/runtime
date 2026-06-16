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
        long a = 0x00000000DEADBEEFL;
        return a == 0x00000000DEADBEEFL ? 0 : 25;
    }
}
