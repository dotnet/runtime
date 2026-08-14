using System.Runtime.CompilerServices;

struct FourDoubles
{
    public double A;
    public double B;
    public double C;
    public double D;
}

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static FourDoubles passStructs(FourDoubles x, FourDoubles y) => x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double s390xHw()
    {
        FourDoubles first = new FourDoubles();
        first.A = 1.0;
        first.B = 2.0;
        first.C = 3.0;
        first.D = 4.0;

        FourDoubles second = new FourDoubles();
        second.A = 5.0;
        second.B = 6.0;
        second.C = 7.0;
        second.D = 8.0;

        FourDoubles result = passStructs(first, second);

        return result.A + result.B + result.C + result.D;
    }

    static int Main() => s390xHw() == 10.0 ? 0 : 1;
}

