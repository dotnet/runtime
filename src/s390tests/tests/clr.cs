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
        uint a = 10U;
        uint b = 5U;
        return (a > b) ? 0 : 45;
    }
}
