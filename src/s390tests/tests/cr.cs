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
        int a = 10;
        int b = 10;
        int c = 5;
        return (a == b && a > c) ? 0 : 43;
    }
}
