// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Internal;
using System.Text;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class OrderByTests
    {
        [Fact]
        public void ElementsAllSameKey()
        {
            List<int> source = new List<int>(new int[] { 9, 9, 9, 9, 9, 9 });
            int[] expected = { 9, 9, 9, 9, 9, 9 };

            Assert.Equal(expected, source.OrderBy((int x, int y) => { return Comparer<int>.Default.Compare(x, y); }));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            List<string> source = new List<string>(new string[] { "prakash", "Alpha", "dan", "DAN", "Prakash" });
            string[] expected = { "Alpha", "dan", "DAN", "prakash", "Prakash" };

            Assert.Equal(expected, source.OrderBy((string left, string right) => { return StringComparer.OrdinalIgnoreCase.Compare(left, right); }));
        }
    }
}