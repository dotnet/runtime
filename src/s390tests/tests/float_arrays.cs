using System;

public class S390xInstructionTest
{
    public static int Main()
    {
        float result = s390xHw();
        return result == 1.1f ? 0 : 1;
    }

    public static float s390xHw()
    {
        float[] arr = {1.1f, 2.2f, 3.3f};
        return arr[0];
    }
}

