using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class RangeRepeatTests
    {
        #region Range

        [Fact]
        public void Range_ProduceCorrectSequence()
        {
            var rangeSequence = Enumerable.Range(1, 100);
            int expected = 0;
            foreach (var val in rangeSequence)
            {
                expected++;
                Assert.Equal(expected, val);
            }

            Assert.Equal(100, expected);
        }

        [Fact]
        public void Range_ToArray_ProduceCorrectResult()
        {
            var array = Enumerable.Range(1, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Equal(i + 1, array[i]);
        }


        [Fact]
        public void Range_ZeroCountLeadToEmptySequence()
        {
            var array = Enumerable.Range(1, 0).ToArray();
            var array2 = Enumerable.Range(int.MinValue, 0).ToArray();
            var array3 = Enumerable.Range(int.MaxValue, 0).ToArray();
            Assert.Equal(array.Length, 0);
            Assert.Equal(array2.Length, 0);
            Assert.Equal(array3.Length, 0);
        }

        [Fact]
        public void Range_ThrowExceptionOnNegativeCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1, int.MinValue));
        }

        [Fact]
        public void Range_ThrowExceptionOnOverflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1000, int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(int.MaxValue, 1000));
        }

        [Fact]
        public void Range_NotEnumerateAfterEnd()
        {
            using (var rangeEnum = Enumerable.Range(1, 1).GetEnumerator())
            {
                Assert.True(rangeEnum.MoveNext());
                Assert.False(rangeEnum.MoveNext());
                Assert.False(rangeEnum.MoveNext());
            }
        }

        [Fact]
        public void Range_EnumerableAndEnumeratorAreSame()
        {
            var rangeEnumberable = Enumerable.Range(1, 1);
            using (var rangeEnumberator = rangeEnumberable.GetEnumerator())
            {
                Assert.Same(rangeEnumberable, rangeEnumberator);
            }
        }

        [Fact]
        public void Range_GetEnumeratorReturnUniqueInstances()
        {
            var rangeEnumberable = Enumerable.Range(1, 1);
            using (var enum1 = rangeEnumberable.GetEnumerator())
            using (var enum2 = rangeEnumberable.GetEnumerator())
            {
                Assert.NotSame(enum1, enum2);
            }
        }

        #endregion



        #region Repeat

        [Fact]
        public void Repeat_ProduceCorrectSequence()
        {
            var repeatSequence = Enumerable.Repeat(1, 100);
            int count = 0;
            foreach (var val in repeatSequence)
            {
                count++;
                Assert.Equal(1, val);
            }

            Assert.Equal(100, count);
        }

        [Fact]
        public void Repeat_ToArray_ProduceCorrectResult()
        {
            var array = Enumerable.Repeat(1, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Equal(1, array[i]);
        }

        [Fact]
        public void Repeat_ProduceSameObject()
        {
            object objectInstance = new object();
            var array = Enumerable.Repeat(objectInstance, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Same(objectInstance, array[i]);
        }

        [Fact]
        public void Repeat_WorkWithNullElement()
        {
            object objectInstance = null;
            var array = Enumerable.Repeat(objectInstance, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Null(array[i]);
        }


        [Fact]
        public void Repeat_ZeroCountLeadToEmptySequence()
        {
            var array = Enumerable.Repeat(1, 0).ToArray();
            Assert.Equal(array.Length, 0);
        }

        [Fact]
        public void Repeat_ThrowExceptionOnNegativeCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Repeat(1, -1));
        }


        [Fact]
        public void Repeat_NotEnumerateAfterEnd()
        {
            using (var repeatEnum = Enumerable.Repeat(1, 1).GetEnumerator())
            {
                Assert.True(repeatEnum.MoveNext());
                Assert.False(repeatEnum.MoveNext());
                Assert.False(repeatEnum.MoveNext());
            }
        }

        [Fact]
        public void Repeat_EnumerableAndEnumeratorAreSame()
        {
            var repeatEnumberable = Enumerable.Repeat(1, 1);
            using (var repeatEnumberator = repeatEnumberable.GetEnumerator())
            {
                Assert.Same(repeatEnumberable, repeatEnumberator);
            }
        }

        [Fact]
        public void Repeat_GetEnumeratorReturnUniqueInstances()
        {
            var repeatEnumberable = Enumerable.Repeat(1, 1);
            using (var enum1 = repeatEnumberable.GetEnumerator())
            using (var enum2 = repeatEnumberable.GetEnumerator())
            {
                Assert.NotSame(enum1, enum2);
            }
        }

        #endregion
    }
}
