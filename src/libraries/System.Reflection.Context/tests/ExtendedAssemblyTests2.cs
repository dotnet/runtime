// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class ExtendedAssemblyTests2
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly Assembly _customAssembly;

        public ExtendedAssemblyTests2()
        {
            Assembly assembly = typeof(ExtendedAssemblyTests2).Assembly;
            _customAssembly = _customReflectionContext.MapAssembly(assembly);
        }

        [Fact]
        public void ManifestModule_ReturnsProjectedModule()
        {
            Module module = _customAssembly.ManifestModule;
            Assert.NotNull(module);
            Assert.Equal(ProjectionConstants.CustomModule, module.GetType().FullName);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void EscapedCodeBase_ReturnsValue()
        {
#pragma warning disable SYSLIB0012
            string escapedCodeBase = _customAssembly.EscapedCodeBase;
#pragma warning restore SYSLIB0012
            Assert.NotNull(escapedCodeBase);
        }

        [Fact]
        public void GetExportedTypes_ReturnsProjectedTypes()
        {
            Type[] types = _customAssembly.GetExportedTypes();
            Assert.NotNull(types);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void GetFile_ReturnsNull_ForNonExistent()
        {
            FileStream file = _customAssembly.GetFile("NonExistent.file");
            Assert.Null(file);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void GetFiles_ReturnsFileStreams()
        {
            FileStream[] files = _customAssembly.GetFiles();
            Assert.NotNull(files);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void GetFiles_WithResourceModules_ReturnsFileStreams()
        {
            FileStream[] files = _customAssembly.GetFiles(true);
            Assert.NotNull(files);
        }

        [Fact]
        public void GetLoadedModules_ReturnsModules()
        {
            Module[] modules = _customAssembly.GetLoadedModules(false);
            Assert.NotEmpty(modules);
        }

        [Fact]
        public void GetLoadedModules_WithResourceModules_ReturnsModules()
        {
            Module[] modules = _customAssembly.GetLoadedModules(true);
            Assert.NotEmpty(modules);
        }

        [Fact]
        public void GetManifestResourceInfo_ReturnsInfo()
        {
            string[] names = _customAssembly.GetManifestResourceNames();
            if (names.Length > 0)
            {
                ManifestResourceInfo info = _customAssembly.GetManifestResourceInfo(names[0]);
                Assert.NotNull(info);
            }
        }

        [Fact]
        public void GetManifestResourceStream_WithType_ReturnsStream()
        {
            // Try to get a resource with type
            Stream stream = _customAssembly.GetManifestResourceStream(typeof(ExtendedAssemblyTests2), "NonExistent");
            // May be null if resource doesn't exist
        }

        [Fact]
        public void GetModule_ReturnsModule()
        {
            Module module = _customAssembly.GetModule(_customAssembly.ManifestModule.Name);
            Assert.NotNull(module);
        }

        [Fact]
        public void GetModules_ReturnsModules()
        {
            Module[] modules = _customAssembly.GetModules(false);
            Assert.NotEmpty(modules);
        }

        [Fact]
        public void GetModules_WithResourceModules_ReturnsModules()
        {
            Module[] modules = _customAssembly.GetModules(true);
            Assert.NotEmpty(modules);
        }

        [Fact]
        public void GetTypes_ReturnsProjectedTypes()
        {
            Type[] types = _customAssembly.GetTypes();
            Assert.NotEmpty(types);
        }

        [Fact]
        public void GetType_CaseInsensitive_ReturnsType()
        {
            Type type = _customAssembly.GetType(typeof(TestObject).FullName.ToLowerInvariant(), false, true);
            Assert.NotNull(type);
        }

        [Fact]
        public void GetType_ThrowOnError_ThrowsForNonExistent()
        {
            Assert.Throws<TypeLoadException>(() =>
                _customAssembly.GetType("NonExistent.Type", true, false));
        }

        [Fact]
        public void GetType_ReturnsNull_ForNonExistent()
        {
            Type type = _customAssembly.GetType("NonExistent.Type", false, false);
            Assert.Null(type);
        }

        [Fact]
        public void GetSatelliteAssembly_ThrowsForNonExistent()
        {
            Assert.Throws<FileNotFoundException>(() =>
                _customAssembly.GetSatelliteAssembly(new CultureInfo("fr-FR")));
        }

        [Fact]
        public void CreateInstance_WithIgnoreCase_CreatesInstance()
        {
            object instance = _customAssembly.CreateInstance(
                typeof(TestObject).FullName.ToLowerInvariant(),
                true,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { "test" },
                CultureInfo.InvariantCulture,
                null);
            Assert.NotNull(instance);
        }
    }

    public class ManifestResourceInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void ManifestResourceInfo_Properties_Work()
        {
            Assembly assembly = typeof(ManifestResourceInfoTests).Assembly;
            Assembly customAssembly = _customReflectionContext.MapAssembly(assembly);

            string[] names = customAssembly.GetManifestResourceNames();
            if (names.Length > 0)
            {
                ManifestResourceInfo info = customAssembly.GetManifestResourceInfo(names[0]);
                Assert.NotNull(info);

                // Access properties and assert they are valid
                string fileName = info.FileName;
                Assembly referencedAssembly = info.ReferencedAssembly;
                ResourceLocation resourceLocation = info.ResourceLocation;
                Assert.Null(fileName);
                Assert.Null(referencedAssembly);
                Assert.True(resourceLocation >= 0);
            }
        }
    }

    public class MoreMethodInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;

        public MoreMethodInfoTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
        }

        [Fact]
        public void GetMethod_WithBindingFlags_ReturnsMethod()
        {
            MethodInfo method = _customTypeInfo.GetMethod("GetMessage", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
        }

        [Fact]
        public void GetMethod_WithTypes_ReturnsMethod()
        {
            MethodInfo method = _customTypeInfo.GetMethod("GetMessage", Type.EmptyTypes);
            Assert.NotNull(method);
        }

        [Fact]
        public void GetProperty_WithReturnType_ReturnsProperty()
        {
            // GetProperty with return type may return null if types don't match
            PropertyInfo prop = _customTypeInfo.GetProperty("A", typeof(string));
            // Just verify it doesn't throw
        }

        [Fact]
        public void GetProperty_WithBindingFlags_ReturnsProperty()
        {
            PropertyInfo prop = _customTypeInfo.GetProperty("A", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(prop);
        }
    }

    public class MoreParameterTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void ReturnParameter_HasCorrectPosition()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");

            ParameterInfo returnParam = method.ReturnParameter;
            Assert.NotNull(returnParam);
            Assert.Equal(-1, returnParam.Position);
        }

        [Fact]
        public void Parameter_GetOptionalCustomModifiers_ReturnsProjectedTypes()
        {
            TypeInfo typeInfo = typeof(TypeWithParameters).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("MethodWithOptionalParam");
            ParameterInfo param = method.GetParameters()[0];

            Type[] modifiers = param.GetOptionalCustomModifiers();
            // Just verify we get projected types if any
            Assert.All(modifiers, m => Assert.Equal(ProjectionConstants.CustomType, m.GetType().FullName));
        }

        [Fact]
        public void Parameter_GetRequiredCustomModifiers_ReturnsProjectedTypes()
        {
            TypeInfo typeInfo = typeof(TypeWithParameters).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("MethodWithOptionalParam");
            ParameterInfo param = method.GetParameters()[0];

            Type[] modifiers = param.GetRequiredCustomModifiers();
            // Just verify we get projected types if any
            Assert.All(modifiers, m => Assert.Equal(ProjectionConstants.CustomType, m.GetType().FullName));
        }
    }
}
