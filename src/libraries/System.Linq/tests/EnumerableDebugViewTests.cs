// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Tests
{
    // Enumerable contains several *EnumerableDebugView* types that aren't referenced
    // within the assembly but that are accessed via reflection by other tools.
    // As such, we test those types via reflection as well.

    public class EnumerableDebugViewTests
    {
        [Fact]
        public void NonGenericEnumerableDebugView_ThrowsForNullSource()
        {
            Exception exc = Assert.Throws<TargetInvocationException>(() => CreateSystemCore_EnumerableDebugView(null));
            ArgumentNullException ane = Assert.IsType<ArgumentNullException>(exc.InnerException);
            Assert.Equal("enumerable", ane.ParamName);
        }

        [Fact]
        public void NonGenericEnumerableDebugView_ThrowsForEmptySource()
        {
            IEnumerable source = Enumerable.Range(10, 0);
            object debugView = CreateSystemCore_EnumerableDebugView(source);
            Exception exc = Assert.Throws<TargetInvocationException>(() => GetItems<object>(debugView));
            Assert.NotNull(exc.InnerException);
            Assert.Equal("System.Linq.SystemCore_EnumerableDebugViewEmptyException", exc.InnerException.GetType().FullName);
            Assert.False(string.IsNullOrEmpty(GetEmptyProperty(exc.InnerException)));
        }

        [Fact]
        public void NonGenericEnumerableDebugView_NonEmptySource()
        {
            IEnumerable source = Enumerable.Range(10, 5).Select(i => (object)i);
            object debugView = CreateSystemCore_EnumerableDebugView(source);
            Assert.Equal<object>(source.Cast<object>().ToArray(), GetItems<object>(debugView));
        }

        [Fact]
        public void GenericEnumerableDebugView_ThrowsForNullSource()
        {
            Exception exc = Assert.Throws<TargetInvocationException>(() => CreateSystemCore_EnumerableDebugView<int>(null));
            ArgumentNullException ane = Assert.IsType<ArgumentNullException>(exc.InnerException);
            Assert.Equal("enumerable", ane.ParamName);
        }

        [Fact]
        public void GenericEnumerableDebugView_ThrowsForEmptySource()
        {
            IEnumerable<int> source = Enumerable.Range(10, 0);
            object debugView = CreateSystemCore_EnumerableDebugView(source);
            Exception exc = Assert.Throws<TargetInvocationException>(() => GetItems<int>(debugView));
            Assert.NotNull(exc.InnerException);
            Assert.Equal("System.Linq.SystemCore_EnumerableDebugViewEmptyException", exc.InnerException.GetType().FullName);
            Assert.False(string.IsNullOrEmpty(GetEmptyProperty(exc.InnerException)));
        }

        [Fact]
        public void GenericEnumerableDebugView_NonEmptySource()
        {
            IEnumerable<int> source = Enumerable.Range(10, 5);
            object debugView = CreateSystemCore_EnumerableDebugView(source);
            Assert.Equal(source, GetItems<int>(debugView));
        }

        private static object CreateSystemCore_EnumerableDebugView(IEnumerable source)
        {
            Type edvType = typeof(Enumerable).GetTypeInfo().Assembly.GetType("System.Linq.SystemCore_EnumerableDebugView");
            ConstructorInfo ctor = edvType.GetTypeInfo().DeclaredConstructors.First();
            return ctor.Invoke(new object[] { source });
        }

        private static object CreateSystemCore_EnumerableDebugView<T>(IEnumerable<T> source)
        {
            Type edvOpenGenericType = typeof(Enumerable).GetTypeInfo().Assembly.GetType("System.Linq.SystemCore_EnumerableDebugView`1");
            Type edvClosedGenericType = edvOpenGenericType.MakeGenericType(typeof(T));
            ConstructorInfo ctor = edvClosedGenericType.GetTypeInfo().DeclaredConstructors.First();
            return ctor.Invoke(new object[] { source });
        }

        private static T[] GetItems<T>(object debugView)
        {
            PropertyInfo items = debugView.GetType().GetTypeInfo().GetDeclaredProperty("Items");
            return (T[])items.GetValue(debugView);
        }

        private static string GetEmptyProperty(Exception exc)
        {
            return (string)exc.GetType().GetTypeInfo().GetDeclaredProperty("Empty").GetValue(exc);
        }
    }
}
