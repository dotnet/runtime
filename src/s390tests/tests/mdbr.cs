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
        double a = 4.0;
        double b = 2.5;
        return (a * b) == 10.0 ? 0 : 61;
    }
}
