namespace ReproContracts;

public static class ContractBridge
{
    public static T FromPointer<T>(nint pointer)
        where T : class
        => default!;
}

public sealed class MissingReference
{
}

public struct MissingInitObjValue
{
    public int Value;
}

public sealed class MissingFieldOwner
{
    public static int Counter;

    public int InstanceCounter;
}
