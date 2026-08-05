using System;

public class TestMsgr
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        long a = 6L;
        long b = 7L;
        return (a * b) == 42L ? 0 : 1;
    }
}
