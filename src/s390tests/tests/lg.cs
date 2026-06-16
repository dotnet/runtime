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
        long a = unchecked((long)0x123456789ABCDEF0L);
        return a == unchecked((long)0x123456789ABCDEF0L) ? 0 : 31;
    }
}
