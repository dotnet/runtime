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
        long a = unchecked((long)0xCAFEBABE00000000L);
        return a == unchecked((long)0xCAFEBABE00000000L) ? 0 : 24;
    }
}
