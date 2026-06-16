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
        float a = 3.5f;
        float b = 2.5f;
        return (a + b) == 6.0f ? 0 : 56;
    }
}
