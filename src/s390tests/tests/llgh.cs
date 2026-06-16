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
        ushort a = 0xFFFF;
        long   r = a;
        return r == 0xFFFFL ? 0 : 29;
    }
}
