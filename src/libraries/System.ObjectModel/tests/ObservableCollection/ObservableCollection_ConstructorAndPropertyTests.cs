// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    /// <summary>
    /// Tests the public properties and constructor in ObservableCollection<T>.
    /// </summary>
    public partial class ConstructorAndPropertyTests
    {
        /// <summary>
        /// Tests that the parameterless constructor works.
        /// </summary>
        [Fact]
        public static void ParameterlessConstructorTest()
        {
            var col = new ObservableCollection<string>();
            Assert.Equal(0, col.Count);
            Assert.Empty(col);
        }

        /// <summary>
        /// Tests that the IEnumerable constructor can various IEnumerables with items.
        /// </summary>
        [Theory]
        [MemberData(nameof(Collections))]
        public static void IEnumerableConstructorTest(IEnumerable<string> collection)
        {
            var actual = new ObservableCollection<string>(collection);
            Assert.Equal(collection, actual);
        }

        [Theory]
        [MemberData(nameof(Collections))]
        public static void IEnumerableConstructorTest_MakesCopy(IEnumerable<string> collection)
        {
            var oc = new ObservableCollectionSubclass<string>(collection);
            Assert.NotNull(oc.InnerList);
            Assert.NotSame(collection, oc.InnerList);
        }

        public static readonly object[][] Collections =
        {
            new object[] { new string[] { "one", "two", "three" } },
            new object[] { new List<string> { "one", "two", "three" } },
            new object[] { new Collection<string> { "one", "two", "three" } },
            new object[] { Enumerable.Range(1, 3).Select(i => i.ToString()) },
            new object[] { CreateIteratorCollection() }
        };

        private static IEnumerable<string> CreateIteratorCollection()
        {
            yield return "one";
            yield return "two";
            yield return "three";
        }

        /// <summary>
        /// Tests that the IEnumerable constructor can take an empty IEnumerable.
        /// </summary>
        [Fact]
        public static void IEnumerableConstructorTest_Empty()
        {
            var col = new ObservableCollection<string>(new string[] { });
            Assert.Equal(0, col.Count);
            Assert.Empty(col);
        }

        /// <summary>
        /// Tests that ArgumentNullException is thrown when given a null IEnumerable.
        /// </summary>
        [Fact]
        public static void IEnumerableConstructorTest_Negative()
        {
            AssertExtensions.Throws<ArgumentNullException>("collection", () => new ObservableCollection<string>((IEnumerable<string>)null));
        }

        /// <summary>
        /// Tests that an item can be set using the index.
        /// </summary>
        [Fact]
        public static void ItemTestSet()
        {
            var col = new ObservableCollection<Guid>(new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });
            for (int i = 0; i < col.Count; ++i)
            {
                Guid guid = Guid.NewGuid();
                col[i] = guid;
                Assert.Equal(guid, col[i]);
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, int.MinValue)]
        [InlineData(3, -1)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        [InlineData(3, int.MaxValue)]
        public static void ItemTestSet_Negative_InvalidIndex(int size, int index)
        {
            var col = new ObservableCollection<int>(new int[size]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => col[index]);
        }

        // ICollection<T>.IsReadOnly
        [Fact]
        public static void IsReadOnlyTest()
        {
            var col = new ObservableCollection<Guid>(new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });
            Assert.False(((ICollection<Guid>)col).IsReadOnly);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/57588", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/57588", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsAppleMobile))]
        public static void DebuggerAttributeTests()
        {
            ObservableCollection<int> col = new ObservableCollection<int>(new[] {1, 2, 3, 4});
            DebuggerAttributes.ValidateDebuggerDisplayReferences(col);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(col);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            int[] items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(col, items);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/57588", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/57588", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsAppleMobile))]
        public static void DebuggerAttribute_NullCollection_ThrowsArgumentNullException()
        {
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.CreateDebuggerTypeProxyWithNullArgument(typeof(ObservableCollection<int>)));
            ArgumentNullException argumentNullException = Assert.IsType<ArgumentNullException>(ex.InnerException);
        }

        private partial class ObservableCollectionSubclass<T> : ObservableCollection<T>
        {
            public ObservableCollectionSubclass(IEnumerable<T> collection) : base(collection) { }

            public List<T> InnerList => (List<T>)base.Items;
        }

        /// <summary>
        /// Tests that ArgumentNullException is thrown when given a null IEnumerable.
        /// </summary>
        [Fact]
        public static void ListConstructorTest_Negative()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => new ObservableCollection<string>((List<string>)null));
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
