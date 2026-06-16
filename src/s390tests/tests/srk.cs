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
        int a = 20;
        int b = 8;
        return (a - b) == 12 ? 0 : 3;
    }
}
