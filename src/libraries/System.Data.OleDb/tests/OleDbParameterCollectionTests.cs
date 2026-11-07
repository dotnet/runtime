// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data.Common;
using Xunit;

namespace System.Data.OleDb.Tests
{
    /// <summary>
    /// Tests for <see cref="OleDbParameterCollection"/> that need no OLE DB provider: a command and its parameters can be
    /// created without opening a connection.
    /// </summary>
    public class OleDbParameterCollectionTests
    {
        private static OleDbParameterCollection CreateCollection(int count)
        {
            OleDbParameterCollection collection = new OleDbCommand().Parameters;
            for (int i = 0; i < count; i++)
            {
                collection.Add(new OleDbParameter("p" + i, i));
            }

            return collection;
        }

        [Theory]
        [InlineData(0, new[] { "p1", "p2" })]
        [InlineData(1, new[] { "p0", "p2" })]
        [InlineData(2, new[] { "p0", "p1" })]
        public void RemoveAt_ValidIndex_RemovesOnlyThatParameter(int index, string[] expectedNames)
        {
            OleDbParameterCollection collection = CreateCollection(3);

            collection.RemoveAt(index);

            Assert.Equal(expectedNames.Length, collection.Count);
            for (int i = 0; i < expectedNames.Length; i++)
            {
                Assert.Equal(expectedNames[i], collection[i].ParameterName);
            }
        }

        [Fact]
        public void RemoveAt_ValidIndex_DetachesParameterFromCollection()
        {
            OleDbParameterCollection collection = CreateCollection(2);
            OleDbParameter removed = collection[0];

            collection.RemoveAt(0);

            // A parameter still owned by a collection cannot be added to another one.
            OleDbParameterCollection other = new OleDbCommand().Parameters;
            other.Add(removed);
            Assert.Same(removed, other[0]);
            Assert.False(collection.Contains(removed));
        }

        [Fact]
        public void RemoveAt_LastRemainingParameter_LeavesEmptyCollection()
        {
            OleDbParameterCollection collection = CreateCollection(1);

            collection.RemoveAt(0);

            Assert.Equal(0, collection.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        [InlineData(0, 0)]
        [InlineData(0, -1)]
        [InlineData(0, int.MinValue)]
        [InlineData(0, int.MaxValue)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        [InlineData(2, -1)]
        [InlineData(2, int.MaxValue)]
        public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException(int count, int index)
        {
            OleDbParameterCollection collection = CreateCollection(count);

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(index));

            Assert.Equal("index", exception.ParamName);
            Assert.Equal(index, exception.ActualValue);
            Assert.Equal(count, collection.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void RemoveAt_InvalidIndex_MessageNamesTheCollectionAndItsCount()
        {
            OleDbParameterCollection collection = CreateCollection(2);

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(5));

            Assert.Contains(nameof(OleDbParameterCollection), exception.Message);
            Assert.Contains("5", exception.Message);
            Assert.Contains("Count=2", exception.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void RemoveAt_InvalidIndex_ThroughIListAndDbParameterCollection_ThrowsArgumentOutOfRangeException()
        {
            OleDbParameterCollection collection = CreateCollection(1);

            // IList.RemoveAt documents ArgumentOutOfRangeException for an index that is not valid.
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection).RemoveAt(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((DbParameterCollection)collection).RemoveAt(-1));
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void RemoveAt_NameNotInCollection_StillThrowsIndexOutOfRangeException()
        {
            // Unchanged: only the Int32 overload is documented through IList.RemoveAt.
            OleDbParameterCollection collection = CreateCollection(1);

            Assert.Throws<IndexOutOfRangeException>(() => collection.RemoveAt("missing"));
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void Indexer_InvalidIndex_StillThrowsIndexOutOfRangeException()
        {
            // Unchanged: the indexer is out of the scope of the RemoveAt fix.
            OleDbParameterCollection collection = CreateCollection(1);

            Assert.Throws<IndexOutOfRangeException>(() => collection[1]);
            Assert.Throws<IndexOutOfRangeException>(() => collection[-1]);
        }
    }
}
