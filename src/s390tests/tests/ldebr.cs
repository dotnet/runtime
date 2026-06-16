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
        float  a    = 3.14f;
        double r    = (double)a;
        double diff = System.Math.Abs(r - 3.14);
        return diff <= 0.001 ? 0 : 74;
    }
}
