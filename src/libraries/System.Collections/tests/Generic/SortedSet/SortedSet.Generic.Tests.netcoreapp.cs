// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the SortedSet class.
    /// </summary>
    public abstract partial class SortedSet_Generic_Tests<T> : ISet_Generic_Tests<T>
    {
        #region TryGetValue

        [Fact]
        public void SortedSet_Generic_TryGetValue_Contains()
        {
            T value = CreateT(1);
            SortedSet<T> set = new SortedSet<T> { value };
            T equalValue = CreateT(1);
            T actualValue;
            Assert.True(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(value, actualValue);
            if (!typeof(T).IsValueType)
            {
                Assert.Same(value, actualValue);
            }
        }

        [Fact]
        public void SortedSet_Generic_TryGetValue_Contains_OverwriteOutputParam()
        {
            T value = CreateT(1);
            SortedSet<T> set = new SortedSet<T> { value };
            T equalValue = CreateT(1);
            T actualValue = CreateT(2);
            Assert.True(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(value, actualValue);
            if (!typeof(T).IsValueType)
            {
                Assert.Same(value, actualValue);
            }
        }

        [Fact]
        public void SortedSet_Generic_TryGetValue_NotContains()
        {
            T value = CreateT(1);
            SortedSet<T> set = new SortedSet<T> { value };
            T equalValue = CreateT(2);
            T actualValue;
            Assert.False(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(default(T), actualValue);
        }

        [Fact]
        public void SortedSet_Generic_TryGetValue_NotContains_OverwriteOutputParam()
        {
            T value = CreateT(1);
            SortedSet<T> set = new SortedSet<T> { value };
            T equalValue = CreateT(2);
            T actualValue = equalValue;
            Assert.False(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(default(T), actualValue);
        }

        #endregion
    }
}
