using System;
using System.Runtime.CompilerServices;

public static class Interop
{
    public enum Numbers { A, B, C, D }

    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    internal static extern void Square(Numbers x);

    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    internal static extern void Square(Numbers x, Numbers y);

    public static void Main()
    {
        // Noop
    }
}
