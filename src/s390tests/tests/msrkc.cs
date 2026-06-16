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
        int a = 6;
        int b = 7;
        return (a * b) == 42 ? 0 : 5;
    }
}
