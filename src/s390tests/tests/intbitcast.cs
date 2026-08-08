using System;

public class S390xBitcastTest
{
    public static float s390xHw()
    {
        int bits = 0x3F800000;

        unsafe
        {
            return *(float*)&bits;
        }
    }
}

public class Program
{
    public static int Main()
    {
        float result = S390xBitcastTest.s390xHw();
        Console.WriteLine(result);

        return result == 1.0f ? 0 : 1;
    }
}
