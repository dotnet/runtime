using System;

public class S390xInstructionTest
{
    public static int Main()
    {
        int result = s390xHw();
        return result == 0 ? 0 : 1;
    }

    static double id(double x) { return x; }

    public static int s390xHw()
    {
        double a = id(1.0);
        double b = id(2.0);
        double c = id(3.0);
        double d = id(4.0);
        double e = id(5.0);
        double f = id(6.0);
        double g = id(7.0);
        double h = id(8.0);
        double i = id(8.0);
        return (h == i && a < b && c < d && e < f) ? 0 : 1;
    }
}
