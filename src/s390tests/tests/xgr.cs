using System;

public class TestXgr
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        long a = 0xFFL;
        long b = 0xAAL;
        return (a ^ b) == 0x55L ? 0 : 1;
    }
}
