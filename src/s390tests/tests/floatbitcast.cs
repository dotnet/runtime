using System;

public class S390xBitcastTest
{
    public static int s390xHw()
    {
        float value = 1.5f;

        unsafe
        {
            return *(int*)&value;
        }
    }
}

public class Program
{
    public static int Main()
    {
        int result = S390xBitcastTest.s390xHw();
        Console.WriteLine(result);

        return result == 0x3FC00000 ? 0 : 1;
    }
}
