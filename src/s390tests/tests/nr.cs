using System;

public class TestNr
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        int a = 0xFF;
        int b = 0xAA;
        return (a & b) == 0xAA ? 0 : 1;
    }
}
