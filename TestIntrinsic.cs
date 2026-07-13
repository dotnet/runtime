using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
public class Program {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsAesSupported() => Aes.IsSupported;
}
