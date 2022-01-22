// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal interface IInternalConverterFromReadOnlySpan<TEnumerable, TElement>
    {
        TEnumerable ConvertFromSpan(ReadOnlySpan<TElement> span);
        ReadOnlySpan<TElement> ConvertToSpan(TEnumerable obj);
    }
}
