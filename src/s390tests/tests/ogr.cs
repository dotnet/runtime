using System;

public class TestOgr
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        long a = 0xA0L;
        long b = 0x0BL;
        return (a | b) == 0xABL ? 0 : 1;
    }
}
