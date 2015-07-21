// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Tests.Helpers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToListTests
    {
        /// <summary>
        /// Emulation of async collection change.
        /// It adds a new element to the sequence each time the Count property touched,
        /// so the further call of CopyTo method will fail.
        /// </summary>
        private class GrowingAfterCountReadCollection : TestCollection<int>
        {
            public GrowingAfterCountReadCollection(int[] items) : base(items) { }

            public override int Count
            {
                get
                {
                    var result = base.Count;
                    Array.Resize(ref Items, Items.Length + 1);
                    return result;
                }
            }
        }

        // ============================


        [Fact]
        public void ToList_AlwaysCreateACopy()
        {
            List<int> sourceList = new List<int>() { 1, 2, 3, 4, 5 };
            List<int> resultList = sourceList.ToList();

            Assert.NotSame(sourceList, resultList);
            Assert.Equal(sourceList, resultList);
        }


        private void RunToListOnAllCollectionTypes<T>(T[] items, Action<List<T>> validation)
        {
            validation(Enumerable.ToList(items));
            validation(Enumerable.ToList(new List<T>(items)));
            validation(new TestEnumerable<T>(items).ToList());
            validation(new TestReadOnlyCollection<T>(items).ToList());
            validation(new TestCollection<T>(items).ToList());
        }


        [Fact]
        public void ToList_WorkWithEmptyCollection()
        {
            RunToListOnAllCollectionTypes(new int[0],
                resultList =>
                {
                    Assert.NotNull(resultList);
                    Assert.Equal(0, resultList.Count);
                });
        }

        [Fact]
        public void ToList_ProduceCorrectList()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            RunToListOnAllCollectionTypes(sourceArray,
                resultList =>
                {
                    Assert.Equal(sourceArray.Length, resultList.Count);
                    Assert.Equal(sourceArray, resultList);
                });

            string[] sourceStringArray = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
            RunToListOnAllCollectionTypes(sourceStringArray,
                resultStringList =>
                {
                    Assert.Equal(sourceStringArray.Length, resultStringList.Count);
                    for (int i = 0; i < sourceStringArray.Length; i++)
                        Assert.Same(sourceStringArray[i], resultStringList[i]);
                });
        }


        [Fact]
        public void ToList_TouchCountWithICollection()
        {
            TestCollection<int> source = new TestCollection<int>(new int[] { 1, 2, 3, 4 });
            var resultList = source.ToList();

            Assert.Equal(source, resultList);
            Assert.Equal(1, source.CountTouched);
        }


        [Fact]
        public void ToList_ThrowArgumentNullExceptionWhenSourceIsNull()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>(() => source.ToList());
        }


        // Later this behaviour can be changed
        [Fact]
        [ActiveIssue(1561)]
        public void ToList_UseCopyToWithICollection()
        {
            TestCollection<int> source = new TestCollection<int>(new int[] { 1, 2, 3, 4 });
            var resultList = source.ToList();

            Assert.Equal(source, resultList);
            Assert.Equal(1, source.CopyToTouched);
        }


        [Fact]
        [ActiveIssue(1561)]
        public void ToList_WorkWhenCountChangedAsynchronously()
        {
            GrowingAfterCountReadCollection source = new GrowingAfterCountReadCollection(new int[] { 1, 2, 3, 4 });
            var resultList = source.ToList();

            Assert.True(resultList.Count >= 4);
            Assert.Equal(1, resultList[0]);
            Assert.Equal(2, resultList[0]);
            Assert.Equal(3, resultList[0]);
            Assert.Equal(4, resultList[0]);
        }
    }
}
