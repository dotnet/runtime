using System;

public class S390xInstructionTest
{
    public static int Main()
    {
        long result = s390xHw();
        return result == 1 ? 0 : 1;
    }

    public static long s390xHw()
    {
        long[] arr = {1, 2, 3};
        return arr[0];
    }
}

