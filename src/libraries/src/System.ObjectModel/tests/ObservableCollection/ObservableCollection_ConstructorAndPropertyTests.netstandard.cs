// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    /// <summary>
    /// Tests the public properties and constructor in ObservableCollection<T>.
    /// </summary>
    public partial class ConstructorAndPropertyTests
    {
        /// <summary>
        /// Tests that ArgumentNullException is thrown when given a null IEnumerable.
        /// </summary>
        [Fact]
        public static void ListConstructorTest_Negative()
        {
            Assert.Throws<ArgumentNullException>("list", () => new ObservableCollection<string>((List<string>)null));
        }

        [Fact]
        public static void ListConstructorTest()
        {
            List<string> collection = new List<string> { "one", "two", "three" };
            var actual = new ObservableCollection<string>(collection);
            Assert.Equal(collection, actual);
        }

        [Fact]
        public static void ListConstructorTest_MakesCopy()
        {
            List<string> collection = new List<string> { "one", "two", "three" };
            var oc = new ObservableCollectionSubclass<string>(collection);
            Assert.NotNull(oc.InnerList);
            Assert.NotSame(collection, oc.InnerList);
        }

        private partial class ObservableCollectionSubclass<T> : ObservableCollection<T>
        {
            public ObservableCollectionSubclass(List<T> list) : base(list) { }
        }
    }
}
