using System;

public class S390xInstructionTest
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    static float id(float x) { return x; }

    public static int s390xHw()
    {
        float a = id(1.0f);
        float b = id(2.0f);
        float c = id(3.0f);
        float d = id(4.0f);
        float e = id(5.0f);
        float f = id(6.0f);
        float g = id(7.0f);
        float h = id(8.0f);
        float i = id(100.0f);
        float result = i / b;
        float keep = a + c + d + e + f + g + h;
        return (result == 50.0f && keep == 34.0f) ? 0 : 1;
    }
}
