// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Xunit;

internal static class TheoryDataExtensions
{
    /// <summary>
    ///  Converts an IEnumerable<typeparamref name="T"/> into an Xunit theory compatible enumerable.
    /// </summary>
    public static TheoryData<T> ToTheoryData<T>(this IEnumerable<T> data)
    {
        TheoryData<T> theoryData = [];
        foreach (var item in data)
        {
            theoryData.Add(item);
        }

        return theoryData;
    }

    /// <summary>
    ///  Converts an IEnumerable into an Xunit theory compatible enumerable.
    /// </summary>
    public static TheoryData<T1, T2> ToTheoryData<T1, T2>(this IEnumerable<(T1, T2)> data)
    {
        TheoryData<T1, T2> theoryData = [];
        foreach (var item in data)
        {
            theoryData.Add(item.Item1, item.Item2);
        }

        return theoryData;
    }

    /// <summary>
    ///  Converts an IEnumerable into an Xunit theory compatible enumerable.
    /// </summary>
    public static TheoryData<T1, T2, T3> ToTheoryData<T1, T2, T3>(this IEnumerable<(T1, T2, T3)> data)
    {
        TheoryData<T1, T2, T3> theoryData = [];
        foreach (var item in data)
        {
            theoryData.Add(item.Item1, item.Item2, item.Item3);
        }

        return theoryData;
    }
}
