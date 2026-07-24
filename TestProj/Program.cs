using System;

struct ClrDataAddress
{
    public ulong Value;
    public ClrDataAddress(ulong value) => Value = value;
    public override string ToString() => $"0x{Value:x}";
}

class Program
{
    static void Main()
    {
        ClrDataAddress addr = new ClrDataAddress(255);
        Console.WriteLine($"cDAC: {addr:x}");
    }
}
