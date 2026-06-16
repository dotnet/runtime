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
        float a = 20.0f;
        float b = 4.0f;
        return (a / b) == 5.0f ? 0 : 62;
    }
}
