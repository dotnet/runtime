// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Linq.Tests
{
    public class WhereTests
    {
        #region Null arguments

        [Fact]
        public void Where_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = null;
            Func<int, bool> simplePredicate = (value) => true;
            Func<int, int, bool> complexPredicate = (value, index) => true;

            Assert.Throws<ArgumentNullException>(() => source.Where(simplePredicate));
            Assert.Throws<ArgumentNullException>(() => source.Where(complexPredicate));
        }

        [Fact]
        public void Where_PredicateIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = Enumerable.Range(1, 10);
            Func<int, bool> simplePredicate = null;
            Func<int, int, bool> complexPredicate = null;

            Assert.Throws<ArgumentNullException>(() => source.Where(simplePredicate));
            Assert.Throws<ArgumentNullException>(() => source.Where(complexPredicate));
        }

        #endregion

        #region Deferred execution

        [Fact]
        public void Where_Array_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            Func<bool>[] source = { () => { funcCalled = true; return true; } };

            IEnumerable<Func<bool>> query = source.Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Where_List_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            List<Func<bool>> source = new List<Func<bool>>() { () => { funcCalled = true; return true; } };

            IEnumerable<Func<bool>> query = source.Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Where_IReadOnlyCollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            IReadOnlyCollection<Func<bool>> source = new ReadOnlyCollection<Func<bool>>(new List<Func<bool>>() { () => { funcCalled = true; return true; } });

            IEnumerable<Func<bool>> query = source.Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Where_ICollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            ICollection<Func<bool>> source = new LinkedList<Func<bool>>(new List<Func<bool>>() { () => { funcCalled = true; return true; } });

            IEnumerable<Func<bool>> query = source.Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Where_IEnumerable_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            IEnumerable<Func<bool>> source = Enumerable.Repeat((Func<bool>)(() => { funcCalled = true; return true; }), 1);

            IEnumerable<Func<bool>> query = source.Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void WhereWhere_Array_ExecutionIsDefered()
        {
            bool funcCalled = false;
            Func<bool>[] source = new Func<bool>[] { () => { funcCalled = true; return true; } };

            IEnumerable<Func<bool>> query = source.Where(value => value()).Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void WhereWhere_List_ExecutionIsDefered()
        {
            bool funcCalled = false;
            List<Func<bool>> source = new List<Func<bool>>() { () => { funcCalled = true; return true; } };

            IEnumerable<Func<bool>> query = source.Where(value => value()).Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void WhereWhere_IReadOnlyCollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            IReadOnlyCollection<Func<bool>> source = new ReadOnlyCollection<Func<bool>>(new List<Func<bool>>() { () => { funcCalled = true; return true; } });

            IEnumerable<Func<bool>> query = source.Where(value => value()).Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void WhereWhere_ICollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            ICollection<Func<bool>> source = new LinkedList<Func<bool>>(new List<Func<bool>>() { () => { funcCalled = true; return true; } });

            IEnumerable<Func<bool>> query = source.Where(value => value()).Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void WhereWhere_IEnumerable_ExecutionIsDefered()
        {
            bool funcCalled = false;
            IEnumerable<Func<bool>> source = Enumerable.Repeat((Func<bool>)(() => { funcCalled = true; return true; }), 1);

            IEnumerable<Func<bool>> query = source.Where(value => value()).Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        #endregion

        #region Expected return value

        [Fact]
        public void Where_Array_ReturnsExpectedValues_True()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, bool> truePredicate = (value) => true;

            IEnumerable<int> result = source.Where(truePredicate);

            Assert.Equal(source.Length, result.Count());
            for (int i = 0; i < source.Length; i++)
            {
                Assert.Equal(source.ElementAt(i), result.ElementAt(i));
            }
        }

        [Fact]
        public void Where_Array_ReturnsExpectedValues_False()
        {            
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, bool> falsePredicate = (value) => false;

            IEnumerable<int> result = source.Where(falsePredicate);

            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void Where_Array_ReturnsExpectedValues_Complex()
        {
            int[] source = new[] { 2, 1, 3, 5, 4 };
            Func<int, int, bool> complexPredicate = (value, index) => { return (value == index); };

            IEnumerable<int> result = source.Where(complexPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(1, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void Where_List_ReturnsExpectedValues_True()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, bool> truePredicate = (value) => true;

            IEnumerable<int> result = source.Where(truePredicate);
            
            Assert.Equal(source.Count, result.Count());
            for (int i = 0; i < source.Count; i++)
            {
                Assert.Equal(source.ElementAt(i), result.ElementAt(i));
            }
        }

        [Fact]
        public void Where_List_ReturnsExpectedValues_False()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, bool> falsePredicate = (value) => false;

            IEnumerable<int> result = source.Where(falsePredicate);

            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void Where_List_ReturnsExpectedValues_Complex()
        {
            List<int> source = new List<int> { 2, 1, 3, 5, 4 };
            Func<int, int, bool> complexPredicate = (value, index) => { return (value == index); };

            IEnumerable<int> result = source.Where(complexPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(1, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void Where_IReadOnlyCollection_ReturnsExpectedValues_True()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> truePredicate = (value) => true;

            IEnumerable<int> result = source.Where(truePredicate);

            Assert.Equal(source.Count, result.Count());
            for (int i = 0; i < source.Count; i++)
            {
                Assert.Equal(source.ElementAt(i), result.ElementAt(i));
            }
        }

        [Fact]
        public void Where_IReadOnlyCollection_ReturnsExpectedValues_False()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> falsePredicate = (value) => false;

            IEnumerable<int> result = source.Where(falsePredicate);

            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void Where_IReadOnlyCollection_ReturnsExpectedValues_Complex()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 2, 1, 3, 5, 4 });
            Func<int, int, bool> complexPredicate = (value, index) => { return (value == index); };

            IEnumerable<int> result = source.Where(complexPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(1, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void Where_ICollection_ReturnsExpectedValues_True()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> truePredicate = (value) => true;

            IEnumerable<int> result = source.Where(truePredicate);

            Assert.Equal(source.Count, result.Count());
            for (int i = 0; i < source.Count; i++)
            {
                Assert.Equal(source.ElementAt(i), result.ElementAt(i));
            }
        }

        [Fact]
        public void Where_ICollection_ReturnsExpectedValues_False()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> falsePredicate = (value) => false;

            IEnumerable<int> result = source.Where(falsePredicate);

            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void Where_ICollection_ReturnsExpectedValues_Complex()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 2, 1, 3, 5, 4 });
            Func<int, int, bool> complexPredicate = (value, index) => { return (value == index); };

            IEnumerable<int> result = source.Where(complexPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(1, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void Where_IEnumerable_ReturnsExpectedValues_True()
        {
            IEnumerable<int> source = Enumerable.Range(1, 5);
            Func<int, bool> truePredicate = (value) => true;

            IEnumerable<int> result = source.Where(truePredicate);

            Assert.Equal(source.Count(), result.Count());
            for (int i = 0; i < source.Count(); i++)
            {
                Assert.Equal(source.ElementAt(i), result.ElementAt(i));
            }
        }

        [Fact]
        public void Where_IEnumerable_ReturnsExpectedValues_False()
        {
            IEnumerable<int> source = Enumerable.Range(1, 5);
            Func<int, bool> falsePredicate = (value) => false;

            IEnumerable<int> result = source.Where(falsePredicate);

            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void Where_IEnumerable_ReturnsExpectedValues_Complex()
        {
            IEnumerable<int> source = new LinkedList<int>(new List<int> { 2, 1, 3, 5, 4 });
            Func<int, int, bool> complexPredicate = (value, index) => { return (value == index); };

            IEnumerable<int> result = source.Where(complexPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(1, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void Where_EmptyEnumerable_ReturnsNoElements()
        {
            IEnumerable<int> source = Enumerable.Empty<int>();
            bool wasSelectorCalled = false;

            IEnumerable<int> result = source.Where(value => { wasSelectorCalled = true; return true; });
            
            Assert.Equal(0, result.Count());
            Assert.False(wasSelectorCalled);
        }
        
        [Fact]
        public void Where_Array_CurrentIsDefaultOfTAfterEnumeration()
        {
            int[] source = new[] { 1 };
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }
        
        [Fact]
        public void Where_List_CurrentIsDefaultOfTAfterEnumeration()
        {
            List<int> source = new List<int>() { 1 };
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void Where_IReadOnlyCollection_CurrentIsDefaultOfTAfterEnumeration()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int>() { 1 });
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void Where_ICollection_CurrentIsDefaultOfTAfterEnumeration()
        {
            ICollection<int> source = new LinkedList<int>(new List<int>() { 1 });
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void Where_IEnumerable_CurrentIsDefaultOfTAfterEnumeration()
        {
            IEnumerable<int> source = Enumerable.Repeat(1, 1);
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void WhereWhere_Array_ReturnsExpectedValues()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;

            IEnumerable<int> result = source.Where(evenPredicate).Where(evenPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void WhereWhere_List_ReturnsExpectedValues()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;

            IEnumerable<int> result = source.Where(evenPredicate).Where(evenPredicate);
            
            Assert.Equal(2, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void WhereWhere_IReadOnlyCollection_ReturnsExpectedValues()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;

            IEnumerable<int> result = source.Where(evenPredicate).Where(evenPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void WhereWhere_ICollection_ReturnsExpectedValues()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;

            IEnumerable<int> result = source.Where(evenPredicate).Where(evenPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void WhereWhere_IEnumerable_ReturnsExpectedValues()
        {
            IEnumerable<int> source = Enumerable.Range(1, 5);
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;

            IEnumerable<int> result = source.Where(evenPredicate).Where(evenPredicate);

            Assert.Equal(2, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
        }

        [Fact]
        public void WhereSelect_Array_ReturnsExpectedValues()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(addSelector);

            Assert.Equal(2, result.Count());
            Assert.Equal(3, result.ElementAt(0));
            Assert.Equal(5, result.ElementAt(1));
        }

        [Fact]
        public void WhereSelect_List_ReturnsExpectedValues()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(addSelector);

            Assert.Equal(2, result.Count());
            Assert.Equal(3, result.ElementAt(0));
            Assert.Equal(5, result.ElementAt(1));
        }

        [Fact]
        public void WhereSelect_IReadOnlyCollection_ReturnsExpectedValues()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(addSelector);

            Assert.Equal(2, result.Count());
            Assert.Equal(3, result.ElementAt(0));
            Assert.Equal(5, result.ElementAt(1));
        }

        [Fact]
        public void WhereSelect_ICollection_ReturnsExpectedValues()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(addSelector);

            Assert.Equal(2, result.Count());
            Assert.Equal(3, result.ElementAt(0));
            Assert.Equal(5, result.ElementAt(1));
        }

        [Fact]
        public void WhereSelect_IEnumerable_ReturnsExpectedValues()
        {
            IEnumerable<int> source = Enumerable.Range(1, 5);
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(addSelector);

            Assert.Equal(2, result.Count());
            Assert.Equal(3, result.ElementAt(0));
            Assert.Equal(5, result.ElementAt(1));
        }

        [Fact]
        public void SelectWhere_Array_ReturnsExpectedValues()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Select(addSelector).Where(evenPredicate);

            Assert.Equal(3, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
            Assert.Equal(6, result.ElementAt(2));
        }

        [Fact]
        public void SelectWhere_List_ReturnsExpectedValues()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Select(addSelector).Where(evenPredicate);

            Assert.Equal(3, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
            Assert.Equal(6, result.ElementAt(2));
        }

        [Fact]
        public void SelectWhere_IReadOnlyCollection_ReturnsExpectedValues()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Select(addSelector).Where(evenPredicate);

            Assert.Equal(3, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
            Assert.Equal(6, result.ElementAt(2));
        }

        [Fact]
        public void SelectWhere_ICollection_ReturnsExpectedValues()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Select(addSelector).Where(evenPredicate);

            Assert.Equal(3, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
            Assert.Equal(6, result.ElementAt(2));
        }

        [Fact]
        public void SelectWhere_IEnumerable_ReturnsExpectedValues()
        {
            IEnumerable<int> source = Enumerable.Range(1, 5);
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Select(addSelector).Where(evenPredicate);

            Assert.Equal(3, result.Count());
            Assert.Equal(2, result.ElementAt(0));
            Assert.Equal(4, result.ElementAt(1));
            Assert.Equal(6, result.ElementAt(2));
        }

        #endregion

        #region Exceptions
        
        [Fact]
        public void Where_PredicateThrowsException()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, bool> predicate = value =>
            {
                if (value == 1)
                {
                    throw new InvalidOperationException();
                }
                return true;
            };

            var enumerator = source.Where(predicate).GetEnumerator();

            // Ensure the first MoveNext call throws an exception
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

            // Ensure Current is set to the default value of type T
            int currentValue = enumerator.Current;
            Assert.Equal(default(int), currentValue);

            // Ensure subsequent MoveNext calls succeed
            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);
        }

        /// <summary>
        /// Test enumerator - returns int values from 1 to 5 inclusive.
        /// </summary>
        private class TestEnumerator : IEnumerable<int>, IEnumerator<int>
        {
            private int _current = 0;

            public virtual int Current { get { return _current; } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() { }

            public virtual IEnumerator<int> GetEnumerator()
            {
                return this;
            }

            public virtual bool MoveNext()
            {
                return _current++ < 5;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// A test enumerator that throws an InvalidOperationException when invoking Current after MoveNext has been called exactly once.
        /// </summary>
        private class ThrowsOnCurrentEnumerator : TestEnumerator
        {
            public override int Current
            {
                get
                {
                    var current = base.Current;
                    if (current == 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return current;
                }
            }
        }

        [Fact]
        public void Where_SourceThrowsOnCurrent()
        {
            IEnumerable<int> source = new ThrowsOnCurrentEnumerator();
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();

            // Ensure the first MoveNext call throws an exception
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            
            // Ensure subsequent MoveNext calls succeed
            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);
        }

        /// <summary>
        /// A test enumerator that throws an InvalidOperationException when invoking MoveNext after MoveNext has been called exactly once.
        /// </summary>
        private class ThrowsOnMoveNext : TestEnumerator
        {
            public override bool MoveNext()
            {
                bool baseReturn = base.MoveNext();
                if (base.Current == 1)
                {
                    throw new InvalidOperationException();
                }

                return baseReturn;
            }
        }

        [Fact]
        public void Where_SourceThrowsOnMoveNext()
        {
            IEnumerable<int> source = new ThrowsOnMoveNext();
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();

            // Ensure the first MoveNext call throws an exception
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

            // Ensure Current is set to the default value of type T
            int currentValue = enumerator.Current;
            Assert.Equal(default(int), currentValue);

            // Ensure subsequent MoveNext calls succeed
            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);
        }

        /// <summary>
        /// A test enumerator that throws an InvalidOperationException when GetEnumerator is called for the first time.
        /// </summary>
        private class ThrowsOnGetEnumerator : TestEnumerator
        {
            private int getEnumeratorCallCount;

            public override IEnumerator<int> GetEnumerator()
            {
                if (getEnumeratorCallCount++ == 0)
                {
                    throw new InvalidOperationException();
                }

                return base.GetEnumerator();
            }
        }

        [Fact]
        public void Where_SourceThrowsOnGetEnumerator()
        {
            IEnumerable<int> source = new ThrowsOnGetEnumerator();
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();

            // Ensure the first MoveNext call throws an exception
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

            // Ensure Current is set to the default value of type T
            int currentValue = enumerator.Current;
            Assert.Equal(default(int), currentValue);
            
            // Ensure subsequent MoveNext calls succeed
            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);
        }

        [Fact]
        public void Select_SourceThrowsOnReset()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };

            var enumerator = source.Where(value => true).GetEnumerator();

            Assert.Throws<NotImplementedException>(() => enumerator.Reset());
        }

        [Fact]
        public void Where_SourceThrowsOnConcurrentModification()
        {
            List<int> source = new List<int>() { 1, 2, 3, 4, 5 };
            Func<int, bool> truePredicate = (value) => true;

            var enumerator = source.Where(truePredicate).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);

            source.Add(6);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        #endregion

        [Fact]
        public void Where_GetEnumeratorReturnsUniqueInstances()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };

            var result = source.Where(value => true);

            using (var enumerator1 = result.GetEnumerator())
            using (var enumerator2 = result.GetEnumerator())
            {
                Assert.Same(result, enumerator1);
                Assert.NotSame(enumerator1, enumerator2);
            }
        }
    }
}
