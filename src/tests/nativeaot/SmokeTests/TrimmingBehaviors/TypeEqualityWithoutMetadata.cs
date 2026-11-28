// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class TypeEqualityWithoutMetadata
{
    class Retained;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Type GetRetainedType() => typeof(Retained);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Matches(Type value)
    {
        if (value == typeof(Retained))
            return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Matches<T>() where T : class
    {
        if (typeof(T) == typeof(Retained))
            return true;
        return false;
    }

    static int Main()
    {
        // A runtime type identity remains observable even without reflection metadata.
        if (!Matches(GetRetainedType()) || Matches(typeof(string)) || !Matches<Retained>() || Matches<string>())
            throw new Exception("An independently retained runtime type lost its identity.");
        return 100;
    }
}
