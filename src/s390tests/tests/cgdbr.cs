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
        double a = 5.7;
        return (long)a == 5L ? 0 : 69;
    }
}
