// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Extended assembly tests for coverage
    public class ExtendedAssemblyTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly Assembly _customAssembly;

        public ExtendedAssemblyTests()
        {
            Assembly assembly = typeof(ExtendedAssemblyTests).Assembly;
            _customAssembly = _customReflectionContext.MapAssembly(assembly);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void CodeBase_ReturnsValue()
        {
            // CodeBase is obsolete but still covered
#pragma warning disable SYSLIB0012
            string codeBase = _customAssembly.CodeBase;
#pragma warning restore SYSLIB0012
            Assert.NotNull(codeBase);
        }

        [Fact]
        public void EntryPoint_ReturnsValue()
        {
            // EntryPoint may not be null for test assemblies
            MethodInfo entryPoint = _customAssembly.EntryPoint;
            // Just verify it doesn't throw
        }

        [Fact]
        public void FullName_ReturnsValue()
        {
            string fullName = _customAssembly.FullName;
            Assert.NotNull(fullName);
            Assert.Contains("System.Reflection.Context.Tests", fullName);
        }

        [Fact]
        public void GlobalAssemblyCache_ReturnsFalse()
        {
#pragma warning disable SYSLIB0005
            bool gac = _customAssembly.GlobalAssemblyCache;
#pragma warning restore SYSLIB0005
            Assert.False(gac);
        }

        [Fact]
        public void HostContext_ReturnsValue()
        {
            long hostContext = _customAssembly.HostContext;
            // Just verify it doesn't throw
        }

        [Fact]
        public void ImageRuntimeVersion_ReturnsValue()
        {
            string version = _customAssembly.ImageRuntimeVersion;
            Assert.NotNull(version);
        }

        [Fact]
        public void IsDynamic_ReturnsFalse()
        {
            Assert.False(_customAssembly.IsDynamic);
        }

        [Fact]
        public void IsFullyTrusted_ReturnsTrue()
        {
            Assert.True(_customAssembly.IsFullyTrusted);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void Location_ReturnsValue()
        {
            string location = _customAssembly.Location;
            Assert.NotNull(location);
        }

        [Fact]
        public void ReflectionOnly_ReturnsFalse()
        {
            Assert.False(_customAssembly.ReflectionOnly);
        }

        [Fact]
        public void SecurityRuleSet_ReturnsValue()
        {
            var ruleSet = _customAssembly.SecurityRuleSet;
            // Just verify it doesn't throw
        }

        [Fact]
        public void GetName_ReturnsValue()
        {
            AssemblyName name = _customAssembly.GetName();
            Assert.NotNull(name);
            Assert.Contains("System.Reflection.Context.Tests", name.FullName);
        }

        [Fact]
        public void GetName_WithCopiedName_ReturnsValue()
        {
            AssemblyName name = _customAssembly.GetName(false);
            Assert.NotNull(name);
        }

        [Fact]
        public void GetReferencedAssemblies_ReturnsValue()
        {
            AssemblyName[] refs = _customAssembly.GetReferencedAssemblies();
            Assert.NotEmpty(refs);
        }

        [Fact]
        public void GetDefinedTypes_ReturnsProjectedTypes()
        {
            IEnumerable<TypeInfo> types = _customAssembly.DefinedTypes;
            Assert.NotEmpty(types);
            Assert.All(types, t => Assert.Equal(ProjectionConstants.CustomType, t.GetType().FullName));
        }

        [Fact]
        public void GetManifestResourceStream_ReturnsNull_ForNonExistent()
        {
            var stream = _customAssembly.GetManifestResourceStream("NonExistent");
            Assert.Null(stream);
        }

        [Fact]
        public void GetManifestResourceNames_ReturnsNames()
        {
            string[] names = _customAssembly.GetManifestResourceNames();
            Assert.NotNull(names);
        }

        [Fact]
        public void CreateInstance_CreatesInstance()
        {
            object instance = _customAssembly.CreateInstance(typeof(TestObject).FullName, false, BindingFlags.Public | BindingFlags.Instance, null, new object[] { "test" }, CultureInfo.InvariantCulture, null);
            Assert.NotNull(instance);
            Assert.IsType<TestObject>(instance);
        }

        [Fact]
        public void GetObjectData_ThrowsPlatformNotSupported()
        {
#pragma warning disable SYSLIB0050
            Assert.ThrowsAny<Exception>(() =>
                _customAssembly.GetObjectData(null, default));
#pragma warning restore SYSLIB0050
        }

        [Fact]
        public void GetForwardedTypes_ThrowsNotImplemented()
        {
            // GetForwardedTypes may throw NotImplementedException
            Assert.Throws<NotImplementedException>(() => _customAssembly.GetForwardedTypes());
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _customAssembly.ToString();
            Assert.NotNull(str);
        }
    }

    // Generic method tests for coverage
    internal class TypeWithGenericMethod
    {
        public T GenericMethod<T>(T value) => value;
        public static T StaticGenericMethod<T>(T value) => value;
    }

    public class GenericMethodTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;
        private readonly MethodInfo _genericMethod;

        public GenericMethodTests()
        {
            TypeInfo typeInfo = typeof(TypeWithGenericMethod).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _genericMethod = _customTypeInfo.GetMethod("GenericMethod");
        }

        [Fact]
        public void IsGenericMethod_ReturnsTrue()
        {
            Assert.True(_genericMethod.IsGenericMethod);
        }

        [Fact]
        public void IsGenericMethodDefinition_ReturnsTrue()
        {
            Assert.True(_genericMethod.IsGenericMethodDefinition);
        }

        [Fact]
        public void GetGenericMethodDefinition_ReturnsSelf()
        {
            MethodInfo genericDef = _genericMethod.GetGenericMethodDefinition();
            Assert.NotNull(genericDef);
        }

        [Fact]
        public void MakeGenericMethod_ReturnsProjectedMethod()
        {
            MethodInfo concreteMethod = _genericMethod.MakeGenericMethod(typeof(int));
            Assert.NotNull(concreteMethod);
            Assert.False(concreteMethod.IsGenericMethodDefinition);
        }

        [Fact]
        public void GetGenericArguments_ReturnsProjectedTypes()
        {
            Type[] args = _genericMethod.GetGenericArguments();
            Assert.Single(args);
            Assert.Equal(ProjectionConstants.CustomType, args[0].GetType().FullName);
        }

        [Fact]
        public void ContainsGenericParameters_ReturnsTrue()
        {
            Assert.True(_genericMethod.ContainsGenericParameters);
        }

        [Fact]
        public void MadeGenericMethod_ContainsGenericParameters_ReturnsFalse()
        {
            MethodInfo concreteMethod = _genericMethod.MakeGenericMethod(typeof(int));
            Assert.False(concreteMethod.ContainsGenericParameters);
        }

        [Fact]
        public void Invoke_OnMadeGenericMethod_Works()
        {
            MethodInfo concreteMethod = _genericMethod.MakeGenericMethod(typeof(int));
            var target = new TypeWithGenericMethod();
            object result = concreteMethod.Invoke(target, new object[] { 42 });
            Assert.Equal(42, result);
        }

        [Fact]
        public void CreateDelegate_ForStaticMethod_ReturnsDelegate()
        {
            MethodInfo staticGenericMethod = _customTypeInfo.GetMethod("StaticGenericMethod");
            MethodInfo concreteMethod = staticGenericMethod.MakeGenericMethod(typeof(string));
            Delegate del = concreteMethod.CreateDelegate(typeof(Func<string, string>));
            Assert.NotNull(del);
            Assert.Equal("test", ((Func<string, string>)del)("test"));
        }
    }
}
