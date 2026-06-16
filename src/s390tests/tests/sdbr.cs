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
        double a = 10.0;
        double b = 3.0;
        return (a - b) == 7.0 ? 0 : 59;
    }
}
