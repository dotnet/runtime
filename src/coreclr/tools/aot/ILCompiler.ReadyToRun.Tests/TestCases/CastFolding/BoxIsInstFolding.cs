// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public interface INotImplemented
{
}

public struct SourceStruct
{
    public int Value;
}

public struct TargetStruct
{
    public int Value;
}

public struct ControlStruct
{
    public int Value;
}

public static class BoxIsInstFolding
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Classify<T>(T value)
    {
        if (value is byte or sbyte)
            return 1;
        if (value is short or ushort or char)
            return 2;
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ClassifyChar(char c) => Classify(c);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool StructIsOtherStruct(SourceStruct s) => (object)s is TargetStruct;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool StructIsInterface(ControlStruct s) => (object)s is INotImplemented;
}
