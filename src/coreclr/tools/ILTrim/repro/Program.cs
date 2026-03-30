
#pragma warning disable 649

interface IFirstType
{
    static void Method() { }
}

interface IPropertyType
{
    static int FirstProperty { get; }
}

interface IProgram
{
    static int Field;

    static int FirstMethod() => 300;
    static int Main()
    {
#pragma warning disable 0219
        IAnotherType mylocal = default;
#pragma warning restore 0219
        return 42;
    }
    static void LastMethod(int someParameter) { }
}

interface IAnotherType
{
    static void Method() { }
}
