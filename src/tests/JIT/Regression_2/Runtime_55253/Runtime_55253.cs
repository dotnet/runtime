// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Runtime_55253
{
    [Fact]
    public static int TestEntryPoint()
    {
        int errors = 0;
        if (AsInt32() != -1)
            errors |= 1;
        if (AsUInt32() != 255)
            errors |= 2;

        return 100 + errors;
    }

    static uint AsUInt32() => AsUInt16();
    static uint AsUInt16() => AsUInt8();
    static uint AsUInt8() => 255;

    static int AsInt32() => AsInt16();
    static short AsInt16() => AsInt8();
    static sbyte AsInt8() => -1;
}