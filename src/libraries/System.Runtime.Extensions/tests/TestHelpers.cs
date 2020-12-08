// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System
{
    public static class TestHelpers
    {
        public static TheoryData<T> Concat<T>(this IEnumerable<T> first, TheoryData<T> second)
        {
            TheoryData<T> data = new TheoryData<T>();
            foreach (var item in first)
                data.Add(item);
            foreach (var item in second)
                data.Add((T)item[0]);
            return data;
        }

        public static TheoryData<T> Concat<T>(this TheoryData<T> first, TheoryData<T> second)
        {
            TheoryData<T> data = new TheoryData<T>();
            foreach (var item in first)
                data.Add((T)item[0]);
            foreach (var item in second)
                data.Add((T)item[0]);
            return data;
        }
    }
}
