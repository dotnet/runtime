class Program
{
    static int Main(string[] args)
    {
        System.Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        CallC();
        CallB();
        CallC2();
        CallB2();

        return 100;
    }

    static void CallB() => new B();
    static void CallC() => new C();
    static void CallB2() => new B2();
    static void CallC2() => new C2();
}

abstract class A<T>
{
    public abstract A<T> M();
}

abstract class A2<T>
{
    public abstract A2<T> M<U>();
}

class B : A<string>
{
    public override B M() => new B();
}

class B2 : A2<string>
{
    public override B2 M<U>() => new B2();
}

class C : A<int>
{
    public override C M() => new C();
}

class C2 : A2<int>
{
    public override C2 M<U>() => new C2();
}
