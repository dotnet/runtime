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
    static FourDoubles passStructs(
        FourDoubles a,
        FourDoubles b,
        FourDoubles c,
        FourDoubles d) => a;

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

        FourDoubles third = new FourDoubles();
        third.A = 9.0;
        third.B = 10.0;
        third.C = 11.0;
        third.D = 12.0;

        FourDoubles fourth = new FourDoubles();
        fourth.A = 13.0;
        fourth.B = 14.0;
        fourth.C = 15.0;
        fourth.D = 16.0;

        FourDoubles result = passStructs(first, second, third, fourth);

        return result.A + result.B + result.C + result.D;
    }

    static int Main() => s390xHw() == 10.0 ? 0 : 1;
}

