// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_58259;

using Xunit;
public unsafe class Runtime_58259
{
    [OuterLoop]
    [Fact]
    // This test uses pinvoke marshalling with calli which is not currently supported by the interpreter.
    [ActiveIssue("https://github.com/dotnet/runtime/issues/120904", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsCoreClrInterpreter))]
    public static void TestEntryPoint()
    {
        M(out _);
    }

    static delegate* unmanaged<out int, void> _f;

    internal static void M(out int index)
    {
        if (_f != null)
        {
            _f(out index);
            _f(out index);
        }
        else
        {
            index = 0;
        }
    }
}

