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
        int   a = -1;           // 0xFFFFFFFF
        ulong r = (ulong)(uint)a;
        return r == 0xFFFFFFFFUL ? 0 : 87;
    }
}
