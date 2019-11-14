// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class CustomAssemblyTests
    {
        private readonly Assembly _customAssembly;

        public CustomAssemblyTests()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            Assembly assembly = typeof(CustomAssemblyTests).Assembly;

            _customAssembly = customReflectionContext.MapAssembly(assembly);
        }

        [Fact]
        public void ProjectAssemblyTest()
        {
            Assert.Equal(ProjectionConstants.CustomAssembly, _customAssembly.GetType().FullName);
        }

        [Fact]
        public void ManifestModuleTest()
        {
            Module customModule = _customAssembly.ManifestModule;
            Assert.Equal(ProjectionConstants.CustomModule, customModule.GetType().FullName);
        }

        [Fact]
        public void GetCustomAttributesTest()
        {
            object[] attributes = _customAssembly.GetCustomAttributes(typeof(TestAssemblyAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestAssemblyAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributesDataTest()
        {
            IList<CustomAttributeData> customAttributesData = _customAssembly.GetCustomAttributesData();
            Assert.NotEmpty(customAttributesData);
            Assert.All(customAttributesData,
                cad => Assert.Equal(ProjectionConstants.ProjectingCustomAttributeData, cad.GetType().FullName));
        }

        [Fact]
        public void IsDefinedTest()
        {
            Assert.True(_customAssembly.IsDefined(typeof(TestAssemblyAttribute), true));
            Assert.True(_customAssembly.IsDefined(typeof(TestAssemblyAttribute), false));
            Assert.False(_customAssembly.IsDefined(typeof(DataContractAttribute), true));
            Assert.False(_customAssembly.IsDefined(typeof(DataContractAttribute), false));
        }

        [Fact]
        public void GetExportedTypesTest()
        {
            Type[] exportedTypes = _customAssembly.GetExportedTypes();
            Assert.NotEmpty(exportedTypes);
            Assert.All(exportedTypes,
                (et) => Assert.Equal(ProjectionConstants.CustomType, et.GetType().FullName));
        }

        [Fact]
        public void GetLoadedModulesTest()
        {
            Module[] loadedModules = _customAssembly.GetLoadedModules(true);
            Assert.Single(loadedModules);
            Assert.Equal(typeof(CustomAssemblyTests).Assembly.GetName().Name + ".dll", loadedModules[0].Name);
            Assert.All(loadedModules,
                (mod) => Assert.Equal(ProjectionConstants.CustomModule, mod.GetType().FullName));
        }

        [Fact]
        public void GetManifestResourceInfoTest()
        {
            IEnumerable<ManifestResourceInfo> manifestResourceInfos = _customAssembly.GetManifestResourceNames()
                .Select(mrn => _customAssembly.GetManifestResourceInfo(mrn));
            Assert.NotEmpty(manifestResourceInfos);
            Assert.All(manifestResourceInfos,
                (mri) => Assert.Equal(ProjectionConstants.ProjectingManifestResourceInfo, mri.GetType().FullName));
        }

        [Fact]
        public void GetModulesTest()
        {
            Module[] modules = _customAssembly.GetModules(true);
            Assert.Single(modules);
            Assert.Equal(typeof(CustomAssemblyTests).Assembly.GetName().Name + ".dll", modules[0].Name);
            Assert.All(modules,
                (module) => Assert.Equal(ProjectionConstants.CustomModule, module.GetType().FullName));
        }

        [Fact]
        public void GetModuleTest()
        {
            Module[] modules = _customAssembly.GetModules(true);
            string moduleName = modules[0].Name;

            Module module = _customAssembly.GetModule(moduleName);
            Assert.NotNull(module);
            Assert.Equal(ProjectionConstants.CustomModule, module.GetType().FullName);
        }

        [Fact]
        public void GetSatelliteAssemblyTest()
        {
            Assert.Throws<FileNotFoundException>(() => _customAssembly.GetSatelliteAssembly(CultureInfo.InvariantCulture));
            Assert.Throws<FileNotFoundException>(() => _customAssembly.GetSatelliteAssembly(CultureInfo.InvariantCulture, Version.Parse("1.0.0.0")));
        }

        [Fact]
        public void GetTypeTest()
        {
            Type type = _customAssembly.GetType(typeof(TestObject).FullName, throwOnError: false, ignoreCase: false);
            Assert.NotNull(type);
            Assert.Equal(ProjectionConstants.CustomType, type.GetType().FullName);
        }

        [Fact]
        public void GetTypesTest()
        {
            Type[] types = _customAssembly.GetTypes();
            Assert.NotEmpty(types);
            Assert.All(types,
                (type) => Assert.Equal(ProjectionConstants.CustomType, type.GetType().FullName));
        }

        [Fact]
        public void EqualsTest()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            Assembly assembly = typeof(CustomAssemblyTests).Assembly;
            Assembly customAssembly = customReflectionContext.MapAssembly(assembly);

            // Projector is not the same.
            Assert.NotEqual(customAssembly, _customAssembly);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            Assembly assembly = typeof(CustomAssemblyTests).Assembly;

            Assert.NotEqual(assembly.GetHashCode(), _customAssembly.GetHashCode());
        }
    }
}
