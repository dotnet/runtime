using System;

public class TestOr
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        int a = 0xA0;
        int b = 0x0B;
        return (a | b) == 0xAB ? 0 : 1;
    }
}
