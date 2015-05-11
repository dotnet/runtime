using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Tests.Helpers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToArrayTests
    {
        private class TestLargeSequence : IEnumerable<byte>
        {
            public long MaxSize = 2 * (long)int.MaxValue;
            public IEnumerator<byte> GetEnumerator()
            {
                for (long i = 0; i < MaxSize; i++) yield return (byte)1;
            }
            IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
        }

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

        // =====================


        [Fact]
        public void ToArray_AlwaysCreateACopy()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5 };
            int[] resultArray = sourceArray.ToArray();

            Assert.NotSame(sourceArray, resultArray);
            Assert.Equal(sourceArray, resultArray);
        }


        private void RunToArrayOnAllCollectionTypes<T>(T[] items, Action<T[]> validation)
        {
            validation(Enumerable.ToArray(items));
            validation(Enumerable.ToArray(new List<T>(items)));
            validation(new TestEnumerable<T>(items).ToArray());
            validation(new TestReadOnlyCollection<T>(items).ToArray());
            validation(new TestCollection<T>(items).ToArray());
        }


        [Fact]
        public void ToArray_WorkWithEmptyCollection()
        {
            RunToArrayOnAllCollectionTypes(new int[0],
                resultArray =>
                {
                    Assert.NotNull(resultArray);
                    Assert.Equal(0, resultArray.Length);
                });
        }

        [Fact]
        public void ToArray_ProduceCorrectArray()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            RunToArrayOnAllCollectionTypes(sourceArray,
                resultArray =>
                {
                    Assert.Equal(sourceArray.Length, resultArray.Length);
                    Assert.Equal(sourceArray, resultArray);
                });

            string[] sourceStringArray = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
            RunToArrayOnAllCollectionTypes(sourceStringArray,
                resultStringArray =>
                {
                    Assert.Equal(sourceStringArray.Length, resultStringArray.Length);
                    for (int i = 0; i < sourceStringArray.Length; i++)
                        Assert.Same(sourceStringArray[i], resultStringArray[i]);
                });
        }


        [Fact]
        public void ToArray_TouchCountWithICollection()
        {
            TestCollection<int> source = new TestCollection<int>(new int[] { 1, 2, 3, 4 });
            var resultArray = source.ToArray();

            Assert.Equal(source, resultArray);
            Assert.Equal(1, source.CountTouched);
        }


        [Fact]
        public void ToArray_ThrowArgumentNullExceptionWhenSourceIsNull()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>(() => source.ToArray());
        }


        // Later this behaviour can be changed
        [Fact]
        [ActiveIssue(1561)]
        public void ToArray_UseCopyToWithICollection()
        {
            TestCollection<int> source = new TestCollection<int>(new int[] { 1, 2, 3, 4 });
            var resultArray = source.ToArray();

            Assert.Equal(source, resultArray);
            Assert.Equal(1, source.CopyToTouched);
        }


        [Fact]
        [ActiveIssue(1561)]
        public void ToArray_WorkWhenCountChangedAsynchronously()
        {
            GrowingAfterCountReadCollection source = new GrowingAfterCountReadCollection(new int[] { 1, 2, 3, 4 });
            var resultArray = source.ToArray();

            Assert.True(resultArray.Length >= 4);
            Assert.Equal(1, resultArray[0]);
            Assert.Equal(2, resultArray[0]);
            Assert.Equal(3, resultArray[0]);
            Assert.Equal(4, resultArray[0]);
        }


        [Fact]
        [OuterLoop]
        public void ToArray_FailOnExtremelyLargeCollection()
        {
            TestLargeSequence largeSeq = new TestLargeSequence();
            var thrownException = Assert.ThrowsAny<Exception>(() => { largeSeq.ToArray(); });
            Assert.True(thrownException.GetType() == typeof(OverflowException) || thrownException.GetType() == typeof(OutOfMemoryException));
        }
    }
}
