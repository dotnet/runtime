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
    static double addStruct(FourDoubles x)
        => x.A + x.B + x.C + x.D;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double s390xHw()
    {
        FourDoubles values = new FourDoubles();
        values.A = 1.0;
        values.B = 2.0;
        values.C = 3.0;
        values.D = 4.0;

        return addStruct(values);
    }

    static int Main() => s390xHw() == 10.0 ? 0 : 1;
}

