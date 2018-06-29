// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Linq.Tests
{
    public class WhereTests : EnumerableTests
    {
        #region Null arguments

        [Fact]
        public void Where_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = null;
            Func<int, bool> simplePredicate = (value) => true;
            Func<int, int, bool> complexPredicate = (value, index) => true;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Where(simplePredicate));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Where(complexPredicate));
        }

        [Fact]
        public void Where_PredicateIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = Enumerable.Range(1, 10);
            Func<int, bool> simplePredicate = null;
            Func<int, int, bool> complexPredicate = null;

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.Where(simplePredicate));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.Where(complexPredicate));
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
        public void WhereWhere_Array_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            Func<bool>[] source = new Func<bool>[] { () => { funcCalled = true; return true; } };

            IEnumerable<Func<bool>> query = source.Where(value => value()).Where(value => value());
            Assert.False(funcCalled);

            query = source.Where((value, index) => value());
            Assert.False(funcCalled);
        }

        [Fact]
        public void WhereWhere_List_ExecutionIsDeferred()
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
        public void WhereWhere_IEnumerable_ExecutionIsDeferred()
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
        public void Where_EmptyEnumerable_ReturnsNoElementsWithIndex()
        {
            Assert.Empty(Enumerable.Empty<int>().Where((e, i) => true));
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
        public void WhereSelectSelect_Array_ReturnsExpectedValues()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(i => i).Select(addSelector);

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
        public void WhereSelectSelect_List_ReturnsExpectedValues()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(i => i).Select(addSelector);

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
        public void WhereSelectSelect_IReadOnlyCollection_ReturnsExpectedValues()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(i => i).Select(addSelector);

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
        public void WhereSelectSelect_ICollection_ReturnsExpectedValues()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(i => i).Select(addSelector);

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
        public void WhereSelectSelect_IEnumerable_ReturnsExpectedValues()
        {
            IEnumerable<int> source = Enumerable.Range(1, 5);
            Func<int, bool> evenPredicate = (value) => value % 2 == 0;
            Func<int, int> addSelector = (value) => value + 1;

            IEnumerable<int> result = source.Where(evenPredicate).Select(i => i).Select(addSelector);

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
        public void Select_ResetEnumerator_ThrowsException()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            IEnumerator<int> enumerator = source.Where(value => true).GetEnumerator();

            // The full .NET Framework throws a NotImplementedException.
            // See https://github.com/dotnet/corefx/pull/2959.
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Throws<NotImplementedException>(() => enumerator.Reset());
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
            }
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

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Where(IsEven), q.Where(IsEven));

        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", null, "SoS", string.Empty }
                    select x;

            Assert.Equal(q.Where(string.IsNullOrEmpty), q.Where(string.IsNullOrEmpty));

        }

        [Fact]
        public void SingleElementPredicateFalse()
        {
            int[] source = { 3 };
            Assert.Empty(source.Where(IsEven));
        }

        [Fact]
        public void PredicateFalseForAll()
        {
            int[] source = { 9, 7, 15, 3, 27 };
            Assert.Empty(source.Where(IsEven));
        }

        [Fact]
        public void PredicateTrueFirstOnly()
        {
            int[] source = { 10, 9, 7, 15, 3, 27 };
            Assert.Equal(source.Take(1), source.Where(IsEven));
        }

        [Fact]
        public void PredicateTrueLastOnly()
        {
            int[] source = { 9, 7, 15, 3, 27, 20 };
            Assert.Equal(source.Skip(source.Length - 1), source.Where(IsEven));
        }

        [Fact]
        public void PredicateTrueFirstThirdSixth()
        {
            int[] source = { 20, 7, 18, 9, 7, 10, 21 };
            int[] expected = { 20, 18, 10 };
            Assert.Equal(expected, source.Where(IsEven));
        }

        [Fact]
        public void RunOnce()
        {
            int[] source = { 20, 7, 18, 9, 7, 10, 21 };
            int[] expected = { 20, 18, 10 };
            Assert.Equal(expected, source.RunOnce().Where(IsEven));
        }

        [Fact]
        public void SourceAllNullsPredicateTrue()
        {
            int?[] source = { null, null, null, null };
            Assert.Equal(source, source.Where(num => true));
        }

        [Fact]
        public void SourceEmptyIndexedPredicate()
        {
            Assert.Empty(Enumerable.Empty<int>().Where((e, i) => i % 2 == 0));
        }

        [Fact]
        public void SingleElementIndexedPredicateTrue()
        {
            int[] source = { 2 };
            Assert.Equal(source, source.Where((e, i) => e % 2 == 0));
        }

        [Fact]
        public void SingleElementIndexedPredicateFalse()
        {
            int[] source = { 3 };
            Assert.Empty(source.Where((e, i) => e % 2 == 0));
        }

        [Fact]
        public void IndexedPredicateFalseForAll()
        {
            int[] source = { 9, 7, 15, 3, 27 };
            Assert.Empty(source.Where((e, i) => e % 2 == 0));
        }

        [Fact]
        public void IndexedPredicateTrueFirstOnly()
        {
            int[] source = { 10, 9, 7, 15, 3, 27 };
            Assert.Equal(source.Take(1), source.Where((e, i) => e % 2 == 0));
        }

        [Fact]
        public void IndexedPredicateTrueLastOnly()
        {
            int[] source = { 9, 7, 15, 3, 27, 20 };
            Assert.Equal(source.Skip(source.Length - 1), source.Where((e, i) => e % 2 == 0));
        }

        [Fact]
        public void IndexedPredicateTrueFirstThirdSixth()
        {
            int[] source = { 20, 7, 18, 9, 7, 10, 21 };
            int[] expected = { 20, 18, 10 };
            Assert.Equal(expected, source.Where((e, i) => e % 2 == 0));
        }

        [Fact]
        public void SourceAllNullsIndexedPredicateTrue()
        {
            int?[] source = { null, null, null, null };
            Assert.Equal(source, source.Where((num, index) => true));
        }

        [Fact]
        public void PredicateSelectsFirst()
        {
            int[] source = { -40, 20, 100, 5, 4, 9 };
            Assert.Equal(source.Take(1), source.Where((e, i) => i == 0));
        }

        [Fact]
        public void PredicateSelectsLast()
        {
            int[] source = { -40, 20, 100, 5, 4, 9 };
            Assert.Equal(source.Skip(source.Length - 1), source.Where((e, i) => i == source.Length - 1));
        }

        [Fact(Skip = "Valid test but too intensive to enable even in OuterLoop")]
        public void IndexOverflows()
        {
            var infiniteWhere = new FastInfiniteEnumerator<int>().Where((e, i) => true);
            using (var en = infiniteWhere.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while (en.MoveNext())
                    {
                    }
                });
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Where(i => true);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateArray()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).ToArray().Where(i => true);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateList()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Where(i => true);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIndexed()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Where((e, i) => true);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateWhereSelect()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Where(i => true).Select(i => i);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateWhereSelectArray()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).ToArray().Where(i => true).Select(i => i);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateWhereSelectList()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Where(i => true).Select(i => i);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Theory]
        [MemberData(nameof(ToCollectionData))]
        public void ToCollection(IEnumerable<int> source)
        {
            foreach (IEnumerable<int> equivalent in new[] { source.Where(s => true), source.Where(s => true).Select(s => s) })
            {
                Assert.Equal(source, equivalent);
                Assert.Equal(source, equivalent.ToArray());
                Assert.Equal(source, equivalent.ToList());
                Assert.Equal(source.Count(), equivalent.Count()); // Count may be optimized. The above asserts do not imply this will pass.

                using (IEnumerator<int> en = equivalent.GetEnumerator())
                {
                    for (int i = 0; i < equivalent.Count(); i++)
                    {
                        Assert.True(en.MoveNext());
                    }

                    Assert.False(en.MoveNext()); // No more items, this should dispose.
                    Assert.Equal(0, en.Current); // Reset to default value

                    Assert.False(en.MoveNext()); // Want to be sure MoveNext after disposing still works.
                    Assert.Equal(0, en.Current);
                }
            }
        }

        public static IEnumerable<object[]> ToCollectionData()
        {
            IEnumerable<int> seq = GenerateRandomSequnce(seed: 0xdeadbeef, count: 10);

            foreach (IEnumerable<int> seq2 in IdentityTransforms<int>().Select(t => t(seq)))
            {
                yield return new object[] { seq2 };
            }
        }

        private static IEnumerable<int> GenerateRandomSequnce(uint seed, int count)
        {
            var random = new Random(unchecked((int)seed));

            for (int i = 0; i < count; i++)
            {
                yield return random.Next(int.MinValue, int.MaxValue);
            }
        }
    }
}
