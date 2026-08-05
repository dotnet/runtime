using System;

public class TestAgr
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    public static int s390xHw()
    {
        long a = 0x10L;
        long b = 0x20L;
        return (a + b) == 0x30L ? 0 : 1;
    }
}
