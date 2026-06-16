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
        double a = 20.0;
        double b = 4.0;
        return (a / b) == 5.0 ? 0 : 63;
    }
}
