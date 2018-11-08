// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public static class TypeTests_GetInterface
    {
        [Fact]
        public static void Test_GetInterface_NoMatchReturnsNull()
        {
            Type t = typeof(NoInterfaces).Project();
            Type ifc = t.GetInterface("NotFound", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_CaseSensitiveMatch()
        {
            Type t = typeof(Foo).Project();
            Type ifc = t.GetInterface("IFoo2", ignoreCase: false);
            Assert.Equal(typeof(IFoo2).Project(), ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_IgnoreCaseNoMatchDueToCase()
        {
            Type t = typeof(Foo).Project();
            Type ifc = t.GetInterface("Ifoo2", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_IgnoreCase()
        {
            Type t = typeof(Foo).Project();
            Type ifc = t.GetInterface("Ifoo2", ignoreCase: true);
            Assert.Equal(typeof(IFoo2).Project(), ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_PrefixingNotSupported()
        {
            Type t = typeof(Foo1).Project();
            Type ifc = t.GetInterface("IFo*", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_WithNamespace()
        {
            Type t = typeof(Hoo1).Project();
            Type ifc = t.GetInterface("System.Reflection.Tests.Ns.Inner.IHoo1", ignoreCase: false);
            Assert.Equal(typeof(Ns.Inner.IHoo1).Project(), ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_NamespaceOptional()
        {
            Type t = typeof(Hoo1).Project();
            Type ifc = t.GetInterface("IHoo1", ignoreCase: false);
            Assert.Equal(typeof(Ns.Inner.IHoo1).Project(), ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_NamespaceOptionalityIsAllOrNone()
        {
            Type t = typeof(Hoo1).Project();
            Type ifc = t.GetInterface("Inner.IHoo1", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_NamespaceOptionalityTriggersAmbiguity()
        {
            Type t = typeof(TwoFoo1).Project();
            Assert.Throws<AmbiguousMatchException>(() => t.GetInterface("IFoo1", ignoreCase: false));
        }
    
        [Fact]
        public static void Test_GetInterface_IgnoreCaseDoesNotApplyToNamespacePortion()
        {
            Type t = typeof(Hoo1).Project();
            Type ifc = t.GetInterface("Ns.InNer.IHoo1", ignoreCase: true);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_IgnoreCaseTriggersAmbiguity()
        {
            Type t = typeof(FooMixedCase).Project();
            Type ifc = t.GetInterface("IFoO1", ignoreCase: false);
            Assert.Equal(typeof(IFoO1).Project(), ifc);
            Assert.Throws<AmbiguousMatchException>(() => t.GetInterface("ifoo1", ignoreCase: true));
        }
    
        [Fact]
        public static void Test_GetInterface_Null()
        {
            Type t = typeof(Hoo1).Project();
            Assert.Throws<ArgumentNullException>(() => t.GetInterface(null, ignoreCase: false));
        }
    
        [Fact]
        public static void Test_GetInterface_Empty()
        {
            Type t = typeof(Hoo1).Project();
            Type ifc = t.GetInterface("", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_TrailingDot()
        {
            Type t = typeof(Hoo1).Project();
            Type ifc = t.GetInterface("Ns.Inner.", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_StartingtDot()
        {
            Type t = typeof(Foo).Project();
            Type ifc = t.GetInterface(".IFoo", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        [Fact]
        public static void Test_GetInterface_JustDot()
        {
            Type t = typeof(Hoo1).Project();
            Type ifc = t.GetInterface(".", ignoreCase: false);
            Assert.Null(ifc);
        }
    
        private class NoInterfaces { }
        private class Foo : IFoo1, IFoo2, IFoo3 { }
        private class Foo1 : IFoo1 { }
        private class Hoo1 : Ns.Inner.IHoo1 { }
        private class TwoFoo1 : IFoo1, Ns.Inner.IFoo1 { }
        private class FooMixedCase : IFoo1, IFoO1 { }
    
    }
    
    internal interface IFoo1 { }
    internal interface IFoo2 { }
    internal interface IFoo3 { }
    internal interface IFoO1 { }
    
    namespace Ns.Inner
    {
        internal interface IHoo1 { }
        internal interface IHoo2 { }
        internal interface IHoo3 { }
    
        internal interface IFoo1 { }
    }
}
