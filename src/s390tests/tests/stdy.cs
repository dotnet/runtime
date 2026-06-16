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
        double a = 2.71828;
        return a == 2.71828 ? 0 : 40;
    }
}
