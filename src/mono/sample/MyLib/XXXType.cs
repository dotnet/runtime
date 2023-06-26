namespace MyLib;
public class XXXType
{
    public int Foo(int a, int b)
    {
        return Calculate(a, b, 10);
    }

    private int Calculate(int a, int b, int c)
    {
        var res1 = Add(a, c);
        var res2 = Diff(b, c);
        var res3 = Mul(a, b);
        var res4 = Div(res1, res2);
        return Div(res3, res4); 
    }

    private int Add(int a, int b)
    {
        return a + b;
    }

    private int Diff(int a, int b)
    {
        if (a > b)
            return a - b;
        else
            return b - a;
    }

    private int Mul(int a, int b)
    {
        return a * b;
    }

    private int Div(int a, int b)
    {
        if (b == 0)
            throw new Exception("Division by zero");
        else
            return a / b;
    }
}