// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class AttributesTests
    {
        [Fact]
        public static void ModuleInitializerAttributeTests()
        {
            new ModuleInitializerAttribute();
        }

        [Fact]
        public static void SkipLocalsInitAttributeTests()
        {
            new SkipLocalsInitAttribute();
        }

        [Fact]
        public static void AccessedThroughPropertyAttributeTests()
        {
            var attr1 = new AccessedThroughPropertyAttribute(null);
            Assert.Null(attr1.PropertyName);

            var attr2 = new AccessedThroughPropertyAttribute("MyProperty");
            Assert.Equal("MyProperty", attr2.PropertyName);
        }

        [Fact]
        public static void CallerFilePathAttributeTests()
        {
            new CallerFilePathAttribute();
        }

        [Fact]
        public static void CallerLineNumberAttributeTests()
        {
            new CallerLineNumberAttribute();
        }

        [Fact]
        public static void CallerMemberNameAttributeTests()
        {
            new CallerMemberNameAttribute();
        }

        [Fact]
        public static void CompilationRelaxationsAttributeTests()
        {
            var attr1 = new CompilationRelaxationsAttribute(42);
            Assert.Equal(42, attr1.CompilationRelaxations);

            var attr2 = new CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning);
            Assert.Equal((int)CompilationRelaxations.NoStringInterning, attr2.CompilationRelaxations);
        }

        [Fact]
        public static void CompilerGeneratedAttributeTests()
        {
            new CompilerGeneratedAttribute();
        }

        [Fact]
        public static void CompilerGlobalScopeAttributeTests()
        {
            new CompilerGlobalScopeAttribute();
        }

        [Fact]
        public static void DateTimeConstantAttributeTests()
        {
            long ticks = DateTime.Now.Ticks;
            var attr = new DateTimeConstantAttribute(ticks);
            Assert.Equal(new DateTime(ticks), attr.Value);

            Assert.Throws<ArgumentOutOfRangeException>(() => new DateTimeConstantAttribute(-1));
        }

        [Fact]
        public static void DecimalConstantAttributeTests()
        {
            var attrSigned = new DecimalConstantAttribute(scale: 10, sign: 20, hi: 30, mid: 40, low: 50);
            Assert.Equal(-55340232238.3085240370m, attrSigned.Value);

            var attrUnsigned = new DecimalConstantAttribute(scale: 0, sign: 0, hi: 12u, mid: 13u, low: 14u);
            Assert.Equal(221360928940349194254m, attrUnsigned.Value);

            Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalConstantAttribute(scale: 50, sign: 55, hi: 60, mid: 65, low: 70));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalConstantAttribute(scale: 100, sign: 101, hi: 102u, mid: 103u, low: 104u));
        }

        [Fact]
        public static void DefaultDependencyAttributeTests()
        {
            var attr1 = new DefaultDependencyAttribute(LoadHint.Sometimes);
            Assert.Equal(LoadHint.Sometimes, attr1.LoadHint);

            var attr2 = new DefaultDependencyAttribute((LoadHint)(-1));
            Assert.Equal((LoadHint)(-1), attr2.LoadHint);
        }

        [Fact]
        public static void DependencyAttributeTests()
        {
            var attr1 = new DependencyAttribute("DependentAssembly", LoadHint.Always);
            Assert.Equal("DependentAssembly", attr1.DependentAssembly);
            Assert.Equal(LoadHint.Always, attr1.LoadHint);

            var attr2 = new DependencyAttribute(null, (LoadHint)(-2));
            Assert.Null(attr2.DependentAssembly);
            Assert.Equal((LoadHint)(-2), attr2.LoadHint);
        }

#pragma warning disable SYSLIB0015 // Obsolete: DisablePrivateReflectionAttribute
        [Fact]
        public static void DisablePrivateReflectionAttributeTests()
        {
            new DisablePrivateReflectionAttribute();
        }
#pragma warning restore SYSLIB0015 // Obsolete: DisablePrivateReflectionAttribute

        [Fact]
        public static void DiscardableAttributeTests()
        {
            new DiscardableAttribute();
        }

        [Fact]
        public static void ExtensionAttributeTests()
        {
            new ExtensionAttribute();
        }

        [Fact]
        public static void FixedAddressValueTypeAttributeTests()
        {
            new FixedAddressValueTypeAttribute();
        }

        [Fact]
        public static void FixedBufferAttributeTests()
        {
            var attr1 = new FixedBufferAttribute(typeof(AttributesTests), 5);
            Assert.Equal(typeof(AttributesTests), attr1.ElementType);
            Assert.Equal(5, attr1.Length);

            var attr2 = new FixedBufferAttribute(null, -1);
            Assert.Null(attr2.ElementType);
            Assert.Equal(-1, attr2.Length);
        }

        [Fact]
        public static void IndexerNameAttributeTests()
        {
            new IndexerNameAttribute("Indexer");
            new IndexerNameAttribute(null);
        }

        [Fact]
        public static void InternalsVisibleToAttributeTests()
        {
            var attr1 = new InternalsVisibleToAttribute("MyAssembly");
            Assert.Equal("MyAssembly", attr1.AssemblyName);
            Assert.True(attr1.AllInternalsVisible);
            attr1.AllInternalsVisible = false;
            Assert.False(attr1.AllInternalsVisible);

            var attr2 = new InternalsVisibleToAttribute(null);
            Assert.Null(attr2.AssemblyName);
        }

        [Fact]
        public static void IteratorStateMachineAttributeTests()
        {
            new IteratorStateMachineAttribute(typeof(AttributesTests));
            new IteratorStateMachineAttribute(null);
        }

        [Fact]
        public static void MethodImplAttributeTests()
        {
            var attr1 = new MethodImplAttribute();
            Assert.Equal(default(MethodImplOptions), attr1.Value);

            var attr2 = new MethodImplAttribute(-1);
            Assert.Equal((MethodImplOptions)(-1), attr2.Value);

            var attr3 = new MethodImplAttribute(MethodImplOptions.Unmanaged);
            Assert.Equal(MethodImplOptions.Unmanaged, attr3.Value);
        }

        [Fact]
        public static void ReferenceAssemblyAttributeTests()
        {
            var attr1 = new ReferenceAssemblyAttribute(null);
            Assert.Null(attr1.Description);

            var attr2 = new ReferenceAssemblyAttribute("description");
            Assert.Equal("description", attr2.Description);
        }

        [Fact]
        public static void RuntimeCompatibilityAttributeTests()
        {
            var attr = new RuntimeCompatibilityAttribute();
            Assert.False(attr.WrapNonExceptionThrows);
            attr.WrapNonExceptionThrows = true;
            Assert.True(attr.WrapNonExceptionThrows);
        }

        [Fact]
        public static void SpecialNameAttributeTests()
        {
            new SpecialNameAttribute();
        }

        [Fact]
        public static void StateMachineAttributeTests()
        {
            var attr1 = new StateMachineAttribute(null);
            Assert.Null(attr1.StateMachineType);

            var attr2 = new StateMachineAttribute(typeof(AttributesTests));
            Assert.Equal(typeof(AttributesTests), attr2.StateMachineType);
        }

        [Fact]
        public static void StringFreezingAttributeTests()
        {
            new StringFreezingAttribute();
        }

#pragma warning disable SYSLIB0025 // Obsolete: SuppressIldasmAttribute
        [Fact]
        public static void SuppressIldasmAttributeTests()
        {
            new SuppressIldasmAttribute();
        }
#pragma warning restore SYSLIB0025 // Obsolete: SuppressIldasmAttribute

        [Fact]
        public static void TypeForwardedFromAttributeTests()
        {
            string assemblyFullName = "MyAssembly";
            var attr = new TypeForwardedFromAttribute(assemblyFullName);
            Assert.Equal(assemblyFullName, attr.AssemblyFullName);

            AssertExtensions.Throws<ArgumentNullException>("assemblyFullName", () => new TypeForwardedFromAttribute(null));
            AssertExtensions.Throws<ArgumentNullException>("assemblyFullName", () => new TypeForwardedFromAttribute(""));
        }

        [Fact]
        public static void TypeForwardedToAttributeTests()
        {
            var attr1 = new TypeForwardedToAttribute(null);
            Assert.Null(attr1.Destination);

            var attr2 = new TypeForwardedToAttribute(typeof(AttributesTests));
            Assert.Equal(typeof(AttributesTests), attr2.Destination);
        }

        [Fact]
        public static void UnsafeValueTypeAttributeTests()
        {
            new UnsafeValueTypeAttribute();
        }

        [Fact]
        public static void AsyncMethodBuilderAttributeTests()
        {
            var attr1 = new AsyncMethodBuilderAttribute(null);
            Assert.Null(attr1.BuilderType);

            var attr2 = new AsyncMethodBuilderAttribute(typeof(AttributesTests));
            Assert.Equal(typeof(AttributesTests), attr2.BuilderType);
        }

        [Fact]
        public static void IsByRefLikeAttributeTests()
        {
            new IsByRefLikeAttribute();
        }

        [Fact]
        public static void IsReadOnlyAttributeTests()
        {
            new IsReadOnlyAttribute();
        }

        [Fact]
        public static void EnumeratorCancellationAttributeTests()
        {
            new EnumeratorCancellationAttribute();
        }

        [Fact]
        public static void InterpolatedStringHandlerAttributeTests()
        {
            new InterpolatedStringHandlerAttribute();
        }

        [Theory]
        [InlineData("")]
        [InlineData("param1")]
        public static void InterpolatedStringHandlerArgumentAttributeTests(string firstParameterName)
        {
            var attr1 = new InterpolatedStringHandlerArgumentAttribute(firstParameterName);
            Assert.NotNull(attr1.Arguments);
            Assert.Same(attr1.Arguments, attr1.Arguments);
            Assert.Equal(1, attr1.Arguments.Length);
            Assert.Equal(firstParameterName, attr1.Arguments[0]);

            string[] arguments = new[] { firstParameterName, "param2" };
            var attr2 = new InterpolatedStringHandlerArgumentAttribute(arguments);
            Assert.NotNull(attr2.Arguments);
            Assert.Same(arguments, attr2.Arguments);
            Assert.Equal(firstParameterName, attr2.Arguments[0]);
            Assert.Equal("param2", attr2.Arguments[1]);
        }
    }
}
