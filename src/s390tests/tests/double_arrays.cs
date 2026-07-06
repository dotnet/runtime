using System;

public class S390xInstructionTest
{
    public static int Main()
    {
        double result = s390xHw();
        return result == 1.1 ? 0 : 1;
    }

    public static double s390xHw()
    {
        double[] arr = {1.1, 2.2, 3.3};
        return arr[0];
    }
}

