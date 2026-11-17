// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Arrays implement the generic collection interfaces through SZArrayHelper. These calls drive
// discovery of the SZArrayHelper instantiations that the runtime dispatches to.

readonly struct EmptyArrayElement
{
    public readonly object Value;
    public EmptyArrayElement(object value) => Value = value;
}

readonly struct IndexedArrayElement
{
    public readonly int Value;
    public IndexedArrayElement(int value) => Value = value;
}

static class ArrayInterfaceMethodsTests
{
    // The array type is only created inside Array.Empty<T>, and the interface call is made on the result.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static IReadOnlyList<EmptyArrayElement> GetEmpty() => Array.Empty<EmptyArrayElement>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IList<IndexedArrayElement> GetIndexed() => new IndexedArrayElement[3];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IReadOnlyCollection<string> GetStrings() => new string[3];

    static int Run()
    {
        return GetEmpty().Count + GetIndexed()[1].Value + GetStrings().Count;
    }
}
