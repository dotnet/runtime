// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the generic
    /// IEnumerable interface.
    /// </summary>
    public abstract class IEnumerable_Generic_Tests<T> : TestBase<T>
    {
        #region IEnumerable<T> Helper Methods

        /// <summary>
        /// Creates an instance of an IEnumerable{T} that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned IEnumerable{T} contains.</param>
        /// <returns>An instance of an IEnumerable{T} that can be used for testing.</returns>
        protected abstract IEnumerable<T> GenericIEnumerableFactory(int count);

        /// <summary>
        /// Modifies the given IEnumerable using the given modificationCode as a 'key' to determine
        /// how to modify that IEnumerable. A range of values from 0..WaysToModify will be
        /// then passed to this function. The purpose of this is to enable easy testing
        /// of all modification scenarios of an IEnumerable.
        /// 
        /// Example: ICollection has three methods that can modify (and thus invalidate any currently existing
        /// enumerators): Add, Clear, and Remove. Therefore, when testing with an ICollection, WaysToModify should
        /// return 3.
        /// </summary>
        protected abstract void ModifyEnumerable(IEnumerable<T> enumerable, int modificationCode);

        /// <summary>
        /// To be implemented in the concrete collections test classes. Returns an integer representing how many
        /// different ways there are to modify the IEnumerable. A range of values from 0..WaysToModify will be
        /// then passed to the ModifyEnumerable helper function. The purpose of this is to enable easy testing
        /// of all modification scenarios of an IEnumerable.
        /// 
        /// Example: ICollection has three methods that can modify (and thus invalidate any currently existing
        /// enumerators): Add, Clear, and Remove. Therefore, when testing with an ICollection, WaysToModify should
        /// return 3.
        /// </summary>
        protected abstract int WaysToModify { get; }

        /// <summary>
        /// The Reset method is provided for COM interoperability. It does not necessarily need to be
        /// implemented; instead, the implementer can simply throw a NotSupportedException.
        /// 
        /// If Reset is not implemented, this property must return False. The default value is true.
        /// </summary>
        protected virtual bool ResetImplemented { get { return true; } }

        /// <summary>
        /// When calling Current of the enumerator before the first MoveNext, after the end of the collection,
        /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
        /// to cover two behavioral scenarios:
        ///   - Throwing an InvalidOperationException
        ///   - Returning an undefined value.
        /// 
        /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
        /// false.
        /// </summary>
        protected virtual bool Enumerator_Current_UndefinedOperation_Throws { get { return false; } }

        #endregion

        #region GetEnumerator()

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_GetEnumerator_NoExceptionsWhileGetting(int count)
        {
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            enumerable.GetEnumerator().Dispose();
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_GetEnumerator_ReturnsUniqueEnumerator(int count)
        {
            //Tests that the enumerators returned by GetEnumerator operate independently of one another
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            int iterations = 0;
            foreach (T item in enumerable)
                foreach (T item2 in enumerable)
                    foreach (T item3 in enumerable)
                        iterations++;
            Assert.Equal(count * count * count, iterations);
        }

        #endregion

        #region Enumerator.MoveNext

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_MoveNext_FromStartToFinish(int count)
        {
            int iterations = 0;
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                while (enumerator.MoveNext())
                    iterations++;
                Assert.Equal(count, iterations);
            }
        }

        /// <summary>
        /// For most collections, all calls to MoveNext after disposal of an enumerator will return false.
        /// Some collections (SortedList), however, treat a call to dispose as if it were a call to Reset. Since the docs
        /// specify neither of these as being strictly correct, we leave the method virtual.
        /// </summary>
        [Theory]
        [MemberData("ValidCollectionSizes")]
        public virtual void Enumerator_MoveNext_AfterDisposal(int count)
        {
            IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator();
            for (int i = 0; i < count; i++)
                enumerator.MoveNext();
            enumerator.Dispose();
            Assert.False(enumerator.MoveNext());
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_MoveNext_AfterEndOfCollection(int count)
        {
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                for (int i = 0; i < count; i++)
                    enumerator.MoveNext();
                Assert.False(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(Enumerable.Range(0, WaysToModify), modificationCode =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    ModifyEnumerable(enumerable, modificationCode);
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                }
            });
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(Enumerable.Range(0, WaysToModify), modificationCode =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    for (int i = 0; i < count / 2; i++)
                        enumerator.MoveNext();
                    ModifyEnumerable(enumerable, modificationCode);
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                }
            });
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(Enumerable.Range(0, WaysToModify), modificationCode =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    while (enumerator.MoveNext()) ;
                    ModifyEnumerable(enumerable, modificationCode);
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                }
            });
        }

        #endregion

        #region Enumerator.Current

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Current_FromStartToFinish(int count)
        {
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                T current;
                while (enumerator.MoveNext())
                    current = enumerator.Current;
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Current_ReturnsSameValueOnRepeatedCalls(int count)
        {
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    T current = enumerator.Current;
                    Assert.Equal(current, enumerator.Current);
                    Assert.Equal(current, enumerator.Current);
                    Assert.Equal(current, enumerator.Current);
                }
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Current_ReturnsSameObjectsOnDifferentEnumerators(int count)
        {
            // Ensures that the elements returned from enumeration are exactly the same collection of
            // elements returned from a previous enumeration
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            Dictionary<T, int> firstValues = new Dictionary<T, int>(count);
            Dictionary<T, int> secondValues = new Dictionary<T, int>(count);
            foreach (T item in enumerable)
                firstValues[item] = firstValues.ContainsKey(item) ? firstValues[item]++ : 1;
            foreach (T item in enumerable)
                secondValues[item] = secondValues.ContainsKey(item) ? secondValues[item]++ : 1;
            Assert.Equal(firstValues.Count, secondValues.Count);
            foreach (T key in firstValues.Keys)
                Assert.Equal(firstValues[key], secondValues[key]);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Current_BeforeFirstMoveNext_UndefinedBehavior(int count)
        {
            T current;
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
            {
                if (Enumerator_Current_UndefinedOperation_Throws)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else
                    current = enumerator.Current; // Undefined behavior. Some implementations throw.
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Current_AfterEndOfEnumerable_UndefinedBehavior(int count)
        {
            T current;
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext()) ;
                if (Enumerator_Current_UndefinedOperation_Throws)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else
                    current = enumerator.Current; // Undefined behavior. Some implementations throw.
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Current_ModifiedDuringEnumeration_UndefinedBehavior(int count)
        {
            Assert.All(Enumerable.Range(0, WaysToModify), modificationCode =>
            {
                T current;
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    ModifyEnumerable(enumerable, modificationCode);
                    if (Enumerator_Current_UndefinedOperation_Throws)
                        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                    else
                        current = enumerator.Current; // Undefined behavior. Some implementations throw.
                }
            });
        }

        #endregion

        #region Enumerator.Reset

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Reset_BeforeIteration_Support(int count)
        {
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                if (ResetImplemented)
                    enumerator.Reset();
                else
                    Assert.Throws<NotSupportedException>(() => enumerator.Reset());
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(Enumerable.Range(0, WaysToModify), modificationCode =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    ModifyEnumerable(enumerable, modificationCode);
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                }
            });
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(Enumerable.Range(0, WaysToModify), modificationCode =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    for (int i = 0; i < count / 2; i++)
                        enumerator.MoveNext();
                    ModifyEnumerable(enumerable, modificationCode);
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                }
            });
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(Enumerable.Range(0, WaysToModify), modificationCode =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    while (enumerator.MoveNext()) ;
                    ModifyEnumerable(enumerable, modificationCode);
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                }
            });
        }

        #endregion
    }
}
