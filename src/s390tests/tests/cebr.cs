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
        float a = 5.0f;
        float b = 3.0f;
        return (a > b) ? 0 : 64;
    }
}
