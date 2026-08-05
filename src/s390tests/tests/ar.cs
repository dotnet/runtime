using System;

public class TestAr
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        int a = 0x10;
        int b = 0x20;
        return (a + b) == 0x30 ? 0 : 1;
    }
}
