// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Resources.Extensions.Tests.Common.TestTypes;

internal sealed class NonSerializablePair<T1, T2>
{
    public T1? Value1;
    public T2? Value2;
}
