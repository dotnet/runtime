// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Xunit;

/// <summary>
///  Theory data for tuple enumeration.
/// </summary>
public class EnumerableTupleTheoryData<T1, T2> : IReadOnlyCollection<object[]>
    where T1 : notnull
    where T2 : notnull
{
    private readonly IEnumerable<(T1, T2)> _data;

    public int Count => _data.Count();

    public EnumerableTupleTheoryData(IEnumerable<(T1, T2)> data) => _data = data;

    public IEnumerator<object[]> GetEnumerator() =>
        _data.Select(i => new object[] { i.Item1, i.Item2 }).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <inheritdoc cref="EnumerableTupleTheoryData{T1, T2}"/>
public class EnumerableTupleTheoryData<T1, T2, T3> : IReadOnlyCollection<object[]>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
{
    private readonly IEnumerable<(T1, T2, T3)> _data;

    public int Count => _data.Count();

    public EnumerableTupleTheoryData(IEnumerable<(T1, T2, T3)> data) => _data = data;

    public IEnumerator<object[]> GetEnumerator() =>
        _data.Select(i => new object[] { i.Item1, i.Item2, i.Item3 }).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
