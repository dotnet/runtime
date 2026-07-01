using System;

public class S390xInstructionTest
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 1 ? 0 : 1;
    }

    public static int s390xHw()
    {
        int[] arr = {1, 2, 3};
        return arr[0];
    }
}
