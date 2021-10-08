
#pragma warning disable 649

interface IFirstType
{
    static void Method() { }
}

interface IProgram
{
    static int Field;

    static int FirstMethod() => 300;
    static int Main()
    {
        return 42;
    }
    static void LastMethod(int someParameter) { }
}

interface IAnotherType
{
    static void Method() { }
}
