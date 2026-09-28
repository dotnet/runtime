#:package System.IO.Hashing@11.0.0-rc.1.26425.128
// Finding: Crc32ParameterSet/Crc64ParameterSet.Create(polynomial, initialValue, finalXorValue, reflectValues: true) load
// initialValue into the reflected (LSB-first) register as-is. The Rocksoft/reveng CRC parameter model that the property names
// follow (and that crcmod, reveng and CRC specifications use) defines the initial value unreflected. The two agree only for
// bit-palindromic initial values such as 0 and all ones, so every catalogue check value passes, but a reflected CRC with any other
// seed gives different results from other CRC tools. Either reflect the value or document the convention.
// Run: dotnet run 25-Crc32ParameterSet-ReflectedInitialValue.cs
using System.IO.Hashing;

byte[] check = "123456789"u8.ToArray();
const uint Polynomial = 0x04C11DB7, Init = 0x12345678;

var dotnet = new Crc32(Crc32ParameterSet.Create(Polynomial, Init, 0, reflectValues: true));
dotnet.Append(check);
var dotnetWithReversedInit = new Crc32(Crc32ParameterSet.Create(Polynomial, ReverseBits(Init), 0, reflectValues: true));
dotnetWithReversedInit.Append(check);

uint rocksoft = Rocksoft(Polynomial, Init, 0, reflect: true, check);
Console.WriteLine($"Rocksoft model (width=32 poly=0x{Polynomial:X8} init=0x{Init:X8} refin=refout=true xorout=0): 0x{rocksoft:X8}");
Console.WriteLine($"Crc32ParameterSet.Create(poly, 0x{Init:X8}, 0, true):                          0x{dotnet.GetCurrentHashAsUInt32():X8}");
Console.WriteLine($"Crc32ParameterSet.Create(poly, ReverseBits(init) = 0x{ReverseBits(Init):X8}, 0, true):      0x{dotnetWithReversedInit.GetCurrentHashAsUInt32():X8}");
Console.WriteLine(dotnet.GetCurrentHashAsUInt32() != rocksoft ? "REPRODUCED: the initial value is taken in the reflected domain." : "NOT REPRODUCED");

static uint Rocksoft(uint poly, uint init, uint xorOut, bool reflect, ReadOnlySpan<byte> data)
{
    uint reg = init;
    foreach (byte b in data)
    {
        uint input = reflect ? ReverseBits(b) >> 24 : b;
        reg ^= input << 24;
        for (int k = 0; k < 8; k++)
        {
            reg = (reg & 0x80000000) != 0 ? (reg << 1) ^ poly : reg << 1;
        }
    }

    return (reflect ? ReverseBits(reg) : reg) ^ xorOut;
}

static uint ReverseBits(uint value)
{
    uint result = 0;
    for (int i = 0; i < 32; i++)
    {
        result = (result << 1) | ((value >> i) & 1);
    }

    return result;
}
