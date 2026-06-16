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
        double a    = 3.14159;
        float  r    = (float)a;
        float  diff = System.Math.Abs(r - 3.14159f);
        return diff <= 0.001f ? 0 : 75;
    }
}
