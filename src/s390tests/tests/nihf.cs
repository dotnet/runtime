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
        int a = 0x12345678;
        return (a & unchecked((int)0xFF000000)) == 0x12000000 ? 0 : 20;
    }
}
