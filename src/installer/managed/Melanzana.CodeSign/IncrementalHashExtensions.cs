using System.Security.Cryptography;

namespace Melanzana.CodeSign;

public static class IncrementalHashExtensions
{
    public static void AppendData(this IncrementalHash hash, Span<byte> buffer)
    {
        hash.AppendData(buffer.ToArray());
    }
}
