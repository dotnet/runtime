// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using Xunit;

#pragma warning disable SYSLIB0005, SYSLIB0012

namespace System.Reflection.Tests
{
    public class AssemblyTests
    {
        [Fact]
        public void CodeBase_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.CodeBase);
        }

        [Fact]
        public void CustomAttributes_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.CustomAttributes);
        }

        public static IEnumerable<object[]> CustomAttributes_CustomGetCustomAttributesData_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CustomAttributeData[0] };
            yield return new object[] { new CustomAttributeData[] { null } };
            yield return new object[] { new CustomAttributeData[] { new SubCustomAttributeData() } };
        }

        [Theory]
        [MemberData(nameof(CustomAttributes_CustomGetCustomAttributesData_TestData))]
        public void CustomAttributes_GetCustomGetCustomAttributesData_Success(IList<CustomAttributeData> result)
        {
            var assembly = new CustomAssembly
            {
                GetCustomAttributesDataAction = () => result
            };
            Assert.Same(result, assembly.CustomAttributes);
        }

        [Fact]
        public void DefinedTypes_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.DefinedTypes);
        }

        public static IEnumerable<object[]> DefinedTypes_GetCustom_TestData()
        {
            yield return new object[] { new Type[0], new TypeInfo[0] };
            yield return new object[] { new Type[] { typeof(int) }, new TypeInfo[] { typeof(int).GetTypeInfo() } };
            var type = new IReflectableTypeType
            {
                GetTypeInfoAction = () => typeof(int).GetTypeInfo()
            };
            yield return new object[] { new Type[] { type }, new TypeInfo[] { typeof(int).GetTypeInfo() } };
            yield return new object[] { new Type[] { typeof(string), type }, new TypeInfo[] { typeof(string).GetTypeInfo(), typeof(int).GetTypeInfo() } };
        }

        [Fact]
        public void DefinedTypes_GetCustom_ReturnsExpected()
        {
            // Cannot be a [Theory] as xunit throws "Catastrophic failure"
            foreach (object[] testData in DefinedTypes_GetCustom_TestData())
            {
                Type[] types = (Type[])testData[0];
                TypeInfo[] expected = (TypeInfo[])testData[1];
                var assembly = new CustomAssembly
                {
                    GetTypesAction = () => types
                };
                Assert.Equal(expected, assembly.DefinedTypes);
            }
        }

        [Fact]
        public void DefinedTypes_GetNotTypeInfo_ThrowsNotSupportedException()
        {
            var type = new IReflectableTypeType
            {
                GetTypeInfoAction = () => null
            };
            var assembly = new CustomAssembly
            {
                GetTypesAction = () => new Type[] { type }
            };
            Assert.Throws<NotImplementedException>(() => assembly.DefinedTypes);
        }

        [Fact]
        public void DefinedTypes_GetNullGetTypes_ThrowsNullReferenceException()
        {
            var assembly = new CustomAssembly
            {
                GetTypesAction = () => null
            };
            Assert.Throws<NullReferenceException>(() => assembly.DefinedTypes);
        }

        [Fact]
        public void DefinedTypes_GetNullValueInGetTypes_ThrowsArgumentNullException()
        {
            var assembly = new CustomAssembly
            {
                GetTypesAction = () => new Type[] { null }
            };
            Assert.Throws<ArgumentNullException>("type", () => assembly.DefinedTypes);
        }

        [Fact]
        public void EscapedCodeBase_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.EscapedCodeBase);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("CodeBase", "CodeBase")]
        [InlineData("\0", "%00")]
        [InlineData("\uD800", "%EF%BF%BD%EF%BF%BD")]
        public void EscapedCodeBase_GetCustom_ReturnsExpected(string codeBase, string expected)
        {
            var assembly = new CustomAssembly
            {
                CodeBaseAction = () => codeBase
            };
            Assert.Equal(expected, assembly.EscapedCodeBase);
        }

        [Fact]
        public void EntryPoint_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.EntryPoint);
        }

        [Fact]
        public void ExportedTypes_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.ExportedTypes);
        }

        public static IEnumerable<object[]> ExportedTypes_Custom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Type[0] };
            yield return new object[] { new Type[] { null } };
            yield return new object[] { new Type[] { typeof(int) } };
        }

        [Theory]
        [MemberData(nameof(ExportedTypes_Custom_TestData))]
        public void ExportedTypes_GetCustom_ReturnsExpected(Type[] result)
        {
            var assembly = new CustomAssembly
            {
                GetExportedTypesAction = () => result
            };
            Assert.Same(result, assembly.ExportedTypes);
        }

        [Fact]
        public void FullName_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.FullName);
        }

        [Fact]
        public void GlobalAssemblyCache_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GlobalAssemblyCache);
        }

        [Fact]
        public void HostContext_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.HostContext);
        }

        [Fact]
        public void ImageRuntimeVersion_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.ImageRuntimeVersion);
        }

        [Fact]
        public void IsCollectible_Get_ReturnsExpected()
        {
            var assembly = new SubAssembly();
            Assert.True(assembly.IsCollectible);
        }

        [Fact]
        public void IsDynamic_Get_ReturnsExpected()
        {
            var assembly = new SubAssembly();
            Assert.False(assembly.IsDynamic);
        }

        [Fact]
        public void IsFullyTrusted_Get_ReturnsExpected()
        {
            var assembly = new SubAssembly();
            Assert.True(assembly.IsFullyTrusted);
        }

        [Fact]
        public void Location_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.Location);
        }

        [Fact]
        public void ManifestModule_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.ManifestModule);
        }

        [Fact]
        public void ModuleResolve_AddRemove_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            int callCount = 0;
            Module handler(object sender, ResolveEventArgs e)
            {
                callCount++;
                return null;
            }
            Assert.Throws<NotImplementedException>(() => assembly.ModuleResolve += handler);
            Assert.Throws<NotImplementedException>(() => assembly.ModuleResolve -= handler);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public void Modules_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.Modules);
        }

        public static IEnumerable<object[]> Modules_Custom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Module[0] };
            yield return new object[] { new Module[] { null } };
            yield return new object[] { new Module[] { typeof(int).Module } };
        }

        [Theory]
        [MemberData(nameof(Modules_Custom_TestData))]
        public void Modules_GetCustom_ReturnsExpected(Module[] result)
        {
            var assembly = new CustomAssembly
            {
                GetLoadedModulesAction = getResourceModulesParam =>
                {
                    Assert.True(getResourceModulesParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.Modules);
        }

        [Fact]
        public void ReflectionOnly_Get_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.ReflectionOnly);
        }

        [Fact]
        public void SecurityRuleSet_Get_ReturnsExpected()
        {
            var assembly = new SubAssembly();
            Assert.Equal(SecurityRuleSet.None, assembly.SecurityRuleSet);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void CreateInstance_InvokeString_ThrowsNotImplementedException(string name)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.CreateInstance(name));
        }

        public static IEnumerable<object[]> CreateInstance_StringCustom_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, new object() };
            yield return new object[] { "Type", new object() };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_StringCustom_TestData))]
        public void CreateInstance_InvokeStringCustom_ReturnsExpected(string name, object result)
        {
            var assembly = new CustomAssembly
            {
                CreateInstanceAction = (nameParam, ignoreCaseParam, bindingAttrParam, binderParam, argsParam, cultureParam, activationAttributesParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.False(ignoreCaseParam);
                    Assert.Equal(BindingFlags.Public | BindingFlags.Instance, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Null(argsParam);
                    Assert.Null(cultureParam);
                    Assert.Null(activationAttributesParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.CreateInstance(name));
        }

        public static IEnumerable<object[]> CreateInstance_StringCustomGetType_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { string.Empty, typeof(List<int>), new List<int>() };
            yield return new object[] { "Type", typeof(List<int>), new List<int>() };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_StringCustomGetType_TestData))]
        public void CreateInstance_InvokeStringCustomGetType_ReturnsExpected(string name, Type result, object expected)
        {
            var assembly = new CustomAssembly
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.False(throwOnErrorParam);
                    Assert.False(ignoreCaseParam);
                    return result;
                }
            };
            Assert.Equal(expected, assembly.CreateInstance(name));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("", false)]
        [InlineData("name", true)]
        [InlineData("name", false)]
        public void CreateInstance_InvokeStringBool_ThrowsNotImplementedException(string name, bool ignoreCase)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.CreateInstance(name, ignoreCase));
        }

        public static IEnumerable<object[]> CreateInstance_String_BoolCustom_TestData()
        {
            yield return new object[] { null, true, null };
            yield return new object[] { null, false, null };
            yield return new object[] { string.Empty, true, new object() };
            yield return new object[] { string.Empty, false, new object() };
            yield return new object[] { "Type", true, new object() };
            yield return new object[] { "Type", false, new object() };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_String_BoolCustom_TestData))]
        public void CreateInstance_InvokeStringBoolCustom_ReturnsExpected(string name, bool ignoreCase, object result)
        {
            var assembly = new CustomAssembly
            {
                CreateInstanceAction = (nameParam, ignoreCaseParam, bindingAttrParam, binderParam, argsParam, cultureParam, activationAttributesParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(ignoreCase, ignoreCaseParam);
                    Assert.Equal(BindingFlags.Public | BindingFlags.Instance, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Null(argsParam);
                    Assert.Null(cultureParam);
                    Assert.Null(activationAttributesParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.CreateInstance(name, ignoreCase));
        }

        public static IEnumerable<object[]> CreateInstance_String_BoolCustomGetType_TestData()
        {
            yield return new object[] { null, true, null, null };
            yield return new object[] { null, false, null,null };
            yield return new object[] { string.Empty, true, typeof(List<int>), new List<int>() };
            yield return new object[] { string.Empty, false, typeof(List<int>), new List<int>() };
            yield return new object[] { "Type", true, typeof(List<int>), new List<int>() };
            yield return new object[] { "Type", false, typeof(List<int>), new List<int>() };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_String_BoolCustomGetType_TestData))]
        public void CreateInstance_InvokeStringBoolCustomGetType_ReturnsExpected(string name, bool ignoreCase, Type result, object expected)
        {
            var assembly = new CustomAssembly
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.False(throwOnErrorParam);
                    Assert.Equal(ignoreCase, ignoreCaseParam);
                    return result;
                }
            };
            Assert.Equal(expected, assembly.CreateInstance(name, ignoreCase));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var assembly = new SubAssembly();
            yield return new object[] { assembly, assembly, true };
            yield return new object[] { assembly, new SubAssembly(), false };
            yield return new object[] { assembly, new object(), false };
            yield return new object[] { assembly, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(Assembly assembly, object other, bool expected)
        {
            Assert.Equal(expected, assembly.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var assembly = new SubAssembly();
            yield return new object[] { null, null, true };
            yield return new object[] { null, assembly, false };
            yield return new object[] { assembly, assembly, true };
            yield return new object[] { assembly, new SubAssembly(), false };
            yield return new object[] { assembly, null, false };

            yield return new object[] { new AlwaysEqualsAssembly(), null, false };
            yield return new object[] { null, new AlwaysEqualsAssembly(), false };
            yield return new object[] { new AlwaysEqualsAssembly(), new SubAssembly(), true };
            yield return new object[] { new SubAssembly(), new AlwaysEqualsAssembly(), false };
            yield return new object[] { new AlwaysEqualsAssembly(), new AlwaysEqualsAssembly(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(Assembly assembly1, Assembly assembly2, bool expected)
        {
            Assert.Equal(expected, assembly1 == assembly2);
            Assert.Equal(!expected, assembly1 != assembly2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCustomAttributes_InvokeBool_ThrowsNotImplementedException(bool inherit)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetCustomAttributes(inherit));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCustomAttributes_InvokeBoolCustom_ThrowsNotImplementedException(bool inherit)
        {
            var assembly = new CustomAssembly
            {
                GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
            };
            Assert.Throws<NotImplementedException>(() => assembly.GetCustomAttributes(inherit));
        }

        [Theory]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(int), false)]
        [InlineData(null, true)]
        [InlineData(null, false)]
        public void GetCustomAttributes_InvokeTypeBool_ThrowsNotImplementedException(Type attributeType, bool inherit)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetCustomAttributes(attributeType, inherit));
        }

        [Fact]
        public void GetCustomAttributesData_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetCustomAttributesData());
        }

        [Fact]
        public void GetExportedTypes_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetExportedTypes());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void GetFile_Invoke_ThrowsNotImplementedException(string name)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetFile(name));
        }

        [Fact]
        public void GetFiles_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetFiles());
        }

        public static IEnumerable<object[]> GetFiles_Custom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Module[0] };
            yield return new object[] { new FileStream[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetFiles_Custom_TestData))]
        public void GetFiles_InvokeCustom_ReturnsExpected(FileStream[] result)
        {
            var assembly = new CustomAssembly
            {
                GetFilesAction = getResourceModulesParam =>
                {
                    Assert.False(getResourceModulesParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.GetFiles());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetFiles_InvokeBool_ThrowsNotImplementedException(bool getResourceModules)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetFiles(getResourceModules));
        }

        [Fact]
        public void GetForwardedTypes_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetForwardedTypes());
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var assembly = new SubAssembly();
            Assert.NotEqual(0, assembly.GetHashCode());
            Assert.Equal(assembly.GetHashCode(), assembly.GetHashCode());
        }

        [Fact]
        public void GetLoadedModules_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetLoadedModules());
        }

        public static IEnumerable<object[]> GetLoadedModules_Custom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Module[0] };
            yield return new object[] { new Module[] { null } };
            yield return new object[] { new Module[] { typeof(int).Module } };
        }

        [Theory]
        [MemberData(nameof(GetLoadedModules_Custom_TestData))]
        public void GetLoadedModules_InvokeCustom_ReturnsExpected(Module[] result)
        {
            var assembly = new CustomAssembly
            {
                GetLoadedModulesAction = getResourceModulesParam =>
                {
                    Assert.False(getResourceModulesParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.GetLoadedModules());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetLoadedModules_InvokeBool_ThrowsNotImplementedException(bool getResourceModules)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetLoadedModules(getResourceModules));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("resourceName")]
        public void GetManifestResourceInfo_Invoke_ThrowsNotImplementedException(string resourceName)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetManifestResourceInfo(resourceName));
        }

        [Fact]
        public void GetManifestResourceNames_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetManifestResourceNames());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void GetManifestResourceStream_InvokeString_ThrowsNotImplementedException(string name)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetManifestResourceStream(name));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void GetManifestResourceStream_InvokeStringCustom_ThrowsNotImplementedException(string name)
        {
            using var stream = new MemoryStream();
            var assembly = new CustomAssembly
            {
                GetManifestResourceStreamAction = (type, name) => stream
            };
            Assert.Throws<NotImplementedException>(() => assembly.GetManifestResourceStream(name));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(typeof(int), null)]
        [InlineData(null, "")]
        [InlineData(typeof(int), "")]
        [InlineData(null, "name")]
        [InlineData(typeof(int), "name")]
        public void GetManifestResourceStream_InvokeStringType_ThrowsNotImplementedException(Type type, string name)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetManifestResourceStream(type, name));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void GetModule_Invoke_ThrowsNotImplementedException(string name)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetModule(name));
        }

        [Fact]
        public void GetModules_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetModules());
        }

        public static IEnumerable<object[]> GetModules_Custom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Module[0] };
            yield return new object[] { new Module[] { null } };
            yield return new object[] { new Module[] { typeof(int).Module } };
        }

        [Theory]
        [MemberData(nameof(GetModules_Custom_TestData))]
        public void GetModules_InvokeCustom_ReturnsExpected(Module[] result)
        {
            var assembly = new CustomAssembly
            {
                GetModulesAction = getResourceModulesParam =>
                {
                    Assert.False(getResourceModulesParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.GetModules());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetModules_InvokeBool_ThrowsNotImplementedException(bool getResourceModules)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetModules(getResourceModules));
        }

        [Fact]
        public void GetName_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetName());
        }

        public static IEnumerable<object[]> GetName_Custom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new AssemblyName() };
        }

        [Theory]
        [MemberData(nameof(GetName_Custom_TestData))]
        public void GetName_InvokeCustom_ReturnsExpected(AssemblyName result)
        {
            var assembly = new CustomAssembly
            {
                GetNameAction = copiedNameParam =>
                {
                    Assert.False(copiedNameParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.GetName());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetName_InvokeBool_ThrowsNotImplementedException(bool copiedName)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetName(copiedName));
        }

        public static IEnumerable<object[]> GetObjectData_TestData()
        {
            yield return new object[] { null, default(StreamingContext) };
            yield return new object[] { new SerializationInfo(typeof(int), new FormatterConverter()), default(StreamingContext) };
        }

        [Theory]
        [MemberData(nameof(GetObjectData_TestData))]
        public void GetObjectData_Invoke_ThrowsNotImplementedException(SerializationInfo info, StreamingContext context)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetObjectData(info, context));
        }

        [Fact]
        public void GetReferencedAssemblies_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetReferencedAssemblies());
        }

        public static IEnumerable<object[]> GetSatelliteAssembly_CultureInfo_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { CultureInfo.InvariantCulture };
        }

        [Theory]
        [MemberData(nameof(GetSatelliteAssembly_CultureInfo_TestData))]
        public void GetSatelliteAssembly_InvokeCultureInfo_ThrowsNotImplementedException(CultureInfo culture)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetSatelliteAssembly(culture));
        }

        public static IEnumerable<object[]> GetSatelliteAssembly_CultureInfoCustom_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { CultureInfo.InvariantCulture, typeof(int).Assembly };
        }

        [Theory]
        [MemberData(nameof(GetSatelliteAssembly_CultureInfoCustom_TestData))]
        public void GetSatelliteAssembly_InvokeCultureInfoCustom_ThrowsNotImplementedException(CultureInfo culture, Assembly result)
        {
            var assembly = new CustomAssembly();
            assembly.GetSatelliteAssemblyAction = (cultureParam, versionParam) => result;
            Assert.Throws<NotImplementedException>(() => assembly.GetSatelliteAssembly(culture));
        }

        public static IEnumerable<object[]> GetSatelliteAssembly_CultureInfoVersion_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { CultureInfo.InvariantCulture, new Version(1, 2) };
        }

        [Theory]
        [MemberData(nameof(GetSatelliteAssembly_CultureInfoVersion_TestData))]
        public void GetSatelliteAssembly_InvokeCultureInfoVersion_ThrowsNotImplementedException(CultureInfo culture, Version version)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetSatelliteAssembly(culture, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void GetType_InvokeString_ThrowsNotImplementedException(string name)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetType(name));
        }

        public static IEnumerable<object[]> GetType_StringCustom_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, typeof(List<int>) };
            yield return new object[] { "Type", typeof(List<int>) };
        }

        [Theory]
        [MemberData(nameof(GetType_StringCustom_TestData))]
        public void GetType_InvokeStringCustom_ReturnsExpected(string name, Type result)
        {
            var assembly = new CustomAssembly
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.False(throwOnErrorParam);
                    Assert.False(ignoreCaseParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.GetType(name));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("", false)]
        [InlineData("name", true)]
        [InlineData("name", false)]
        public void GetType_InvokeStringBool_ThrowsNotImplementedException(string name, bool throwOnError)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetType(name, throwOnError));
        }

        public static IEnumerable<object[]> GetType_String_BoolCustom_TestData()
        {
            yield return new object[] { null, true, null };
            yield return new object[] { null, false, null };
            yield return new object[] { string.Empty, true, typeof(List<int>) };
            yield return new object[] { string.Empty, false, typeof(List<int>) };
            yield return new object[] { "Type", true, typeof(List<int>) };
            yield return new object[] { "Type", false, typeof(List<int>) };
        }

        [Theory]
        [MemberData(nameof(GetType_String_BoolCustom_TestData))]
        public void GetType_InvokeStringBoolCustom_ReturnsExpected(string name, bool throwOnError, Type result)
        {
            var assembly = new CustomAssembly
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(throwOnError, throwOnErrorParam);
                    Assert.False(ignoreCaseParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.GetType(name, throwOnError));
        }

        [Theory]
        [InlineData(null, true, true)]
        [InlineData(null, true, false)]
        [InlineData(null, false, true)]
        [InlineData(null, false, false)]
        [InlineData("", true, true)]
        [InlineData("", true, false)]
        [InlineData("", false, true)]
        [InlineData("", false, false)]
        [InlineData("name", true, true)]
        [InlineData("name", true, false)]
        [InlineData("name", false, true)]
        [InlineData("name", false, false)]
        public void GetType_InvokeStringBoolBool_ThrowsNotImplementedException(string name, bool throwOnError, bool ignoreCase)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetType(name, throwOnError, ignoreCase));
        }

        public static IEnumerable<object[]> GetType_String_Bool_BoolCustom_TestData()
        {
            yield return new object[] { null, true, true, null };
            yield return new object[] { null, true, false, null };
            yield return new object[] { null, false, true, null };
            yield return new object[] { null, false, false, null };
            yield return new object[] { string.Empty, true, true, typeof(List<int>) };
            yield return new object[] { string.Empty, true, false, typeof(List<int>) };
            yield return new object[] { string.Empty, false, true, typeof(List<int>) };
            yield return new object[] { string.Empty, false, false, typeof(List<int>) };
            yield return new object[] { "Type", true, true, typeof(List<int>) };
            yield return new object[] { "Type", true, false, typeof(List<int>) };
            yield return new object[] { "Type", false, true, typeof(List<int>) };
            yield return new object[] { "Type", false, false, typeof(List<int>) };
        }

        [Theory]
        [MemberData(nameof(GetType_String_Bool_BoolCustom_TestData))]
        public void GetType_InvokeStringBoolBoolCustom_ReturnsExpected(string name, bool throwOnError, bool ignoreCase, Type result)
        {
            var assembly = new CustomAssembly
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(throwOnError, throwOnErrorParam);
                    Assert.Equal(ignoreCase, ignoreCaseParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.GetType(name, throwOnError, ignoreCase));
        }

        [Fact]
        public void GetTypes_Invoke_ThrowsNotImplementedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.GetTypes());
        }

        public static IEnumerable<object[]> GetTypes_Custom_TestData()
        {
            yield return new object[] { new Module[] { typeof(int).Module }, typeof(int).Module.GetTypes() };

            var module1 = new CustomModule
            {
                GetTypesAction = () => new Type[0]
            };
            var module2 = new CustomModule
            {
                GetTypesAction = () => new Type[] { typeof(int), typeof(string) }
            };
            var module3 = new CustomModule
            {
                GetTypesAction = () => new Type[] { null, typeof(bool) }
            };
            yield return new object[] { new Module[] { module2 }, new Type[] { typeof(int), typeof(string) } };
            yield return new object[] { new Module[] { new CustomModule { GetTypesAction = () => null } }, null };
            yield return new object[] { new Module[] { module1, module2, module3 }, new Type[] { typeof(int), typeof(string), null, typeof(bool) } };
        }

        [Theory]
        [MemberData(nameof(GetTypes_Custom_TestData))]
        public void GetTypes_InvokeCustom_ReturnsExpected(Module[] modules, Type[] expected)
        {
            var assembly = new CustomAssembly
            {
                GetModulesAction = getResourceModules => modules
            };
            Assert.Equal(expected, assembly.GetTypes());
        }

        [Fact]
        public void GetTypes_NullGetModules_ThrowsNullReferenceException()
        {
            var assembly = new CustomAssembly
            {
                GetModulesAction = (getResourceModules) => null
            };
            Assert.Throws<NullReferenceException>(() => assembly.GetTypes());
        }

        [Fact]
        public void GetTypes_NullModuleInGetModulesOneValue_ThrowsNullReferenceException()
        {
            var assembly = new CustomAssembly
            {
                GetModulesAction = (getResourceModules) => new Module[] { null }
            };
            Assert.Throws<NullReferenceException>(() => assembly.GetTypes());
        }

        [Fact]
        public void GetTypes_NullGetModulesGetTypesMoreThanOneValue_ThrowsNullReferenceException()
        {
            var module = new CustomModule
            {
                GetTypesAction = () => null
            };
            var assembly = new CustomAssembly
            {
                GetModulesAction = (getResourceModules) => new Module[] { typeof(int).Module, module }
            };
            Assert.Throws<NullReferenceException>(() => assembly.GetTypes());
        }

        [Fact]
        public void GetTypes_NullModuleInGetModulesMoreThanOneValue_ThrowsNullReferenceException()
        {
            var assembly = new CustomAssembly
            {
                GetModulesAction = (getResourceModules) => new Module[] { typeof(int).Module, null }
            };
            Assert.Throws<NullReferenceException>(() => assembly.GetTypes());
        }

        public static IEnumerable<object[]> LoadModule_String_ByteArray_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, Array.Empty<byte>() };
            yield return new object[] { "moduleName", new byte[] { 1, 2, 3} };
        }

        [Theory]
        [MemberData(nameof(LoadModule_String_ByteArray_TestData))]
        public void LoadModule_InvokeStringByteArrayThrowsNotImplementedException(string moduleName, byte[] rawModule)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.LoadModule(moduleName, rawModule));
        }

        public static IEnumerable<object[]> LoadModule_String_ByteArrayCustom_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { string.Empty, Array.Empty<byte>(), typeof(int).Module };
            yield return new object[] { "name", new byte[] { 1, 2, 3 }, typeof(int).Module };
        }

        [Theory]
        [MemberData(nameof(LoadModule_String_ByteArrayCustom_TestData))]
        public void LoadModule_InvokeStringByteArrayCustom_ReturnsExpected(string moduleName, byte[] rawModule, Module result)
        {
            var assembly = new CustomAssembly
            {
                LoadModuleAction = (moduleNameParam, rawModuleParam, rawSymbolStoreParam) =>
                {
                    Assert.Equal(moduleName, moduleNameParam);
                    Assert.Equal(rawModule, rawModuleParam);
                    Assert.Null(rawSymbolStoreParam);
                    return result;
                }
            };
            Assert.Same(result, assembly.LoadModule(moduleName, rawModule));
        }

        public static IEnumerable<object[]> LoadModule_String_ByteArray_ByteArray_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { string.Empty, Array.Empty<byte>(), Array.Empty<byte>() };
            yield return new object[] { "moduleName", new byte[] { 1, 2, 3}, new byte[] { 4, 5, 6 } };
        }

        [Theory]
        [MemberData(nameof(LoadModule_String_ByteArray_ByteArray_TestData))]
        public void LoadModule_InvokeStringByteArrayByteArray_ThrowsNotImplementedException(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.LoadModule(moduleName, rawModule, rawSymbolStore));
        }

        [Theory]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(int), false)]
        [InlineData(null, true)]
        [InlineData(null, false)]
        public void IsDefined_Invoke_ThrowsNotImplementedException(Type attributeType, bool inherit)
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.IsDefined(attributeType, inherit));
        }

        [Fact]
        public void ToString_Invoke_ThrowsNotSupportedException()
        {
            var assembly = new SubAssembly();
            Assert.Throws<NotImplementedException>(() => assembly.ToString());
        }

        [Theory]
        [InlineData(null, "System.Reflection.Tests.AssemblyTests+CustomAssembly")]
        [InlineData("", "")]
        [InlineData("FullName", "FullName")]
        public void ToString_InvokeCustom_ThrowsNotSupportedException(string fullName, string expected)
        {
            var assembly = new CustomAssembly
            {
                FullNameAction = () => fullName
            };
            Assert.Equal(expected, assembly.ToString());
        }

        public static IEnumerable<object[]> CreateQualifiedName_TestData()
        {
            yield return new object[] { null, null, ", " };
            yield return new object[] { string.Empty, string.Empty, ", " };
            yield return new object[] { "assemblyName", string.Empty, ", assemblyName" };
            yield return new object[] { string.Empty, "typeName", "typeName, " };
            yield return new object[] { "assemblyName", "typeName", "typeName, assemblyName" };
        }

        [Theory]
        [MemberData(nameof(CreateQualifiedName_TestData))]
        public void CreateQualifiedName_Invoke_ReturnsExpected(string assemblyName, string typeName, string expected)
        {
            Assert.Equal(expected, Assembly.CreateQualifiedName(assemblyName, typeName));
        }

        [Fact]
        public void GetAssembly_Invoke_ReturnsExpected()
        {
            Assert.Same(typeof(int).Assembly, Assembly.GetAssembly(typeof(int)));
        }

        public static IEnumerable<object[]> GetAssembly_Custom_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { typeof(int).Module, typeof(int).Assembly };
        }

        [Theory]
        [MemberData(nameof(GetAssembly_Custom_TestData))]
        public void GetAssembly_InvokeCustom_ReturnsExpected(Module module, Assembly expected)
        {
            var type = new CustomType
            {
                ModuleResult = module
            };
            Assert.Same(expected, Assembly.GetAssembly(type));
        }

        [Fact]
        public void GetAssembly_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("type", () => Assembly.GetAssembly(null));
        }

        [Fact]
        public void LoadFrom_NullAssemblyFile_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("assemblyFile", () => Assembly.LoadFrom(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        public void LoadFrom_InvalidAssemblyFile_ThrowsArgumentException(string assemblyFile)
        {
            Assert.Throws<ArgumentException>("path", () => Assembly.LoadFrom(assemblyFile));
        }

        [Fact]
        public void LoadFrom_NoSuchAssemblyFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => Assembly.LoadFrom("NoSuchAssembly"));
        }

        public static IEnumerable<object[]> LoadFrom_String_ByteArray_AssemblyHashAlgorithm_TestData()
        {
            yield return new object[] { null, null, System.Configuration.Assemblies.AssemblyHashAlgorithm.None - 1 };
            yield return new object[] { string.Empty, Array.Empty<byte>(), System.Configuration.Assemblies.AssemblyHashAlgorithm.None };
            yield return new object[] { "assemblyFile", new byte[] { 1, 2, 3 }, System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5 };
        }

        [Theory]
        [MemberData(nameof(LoadFrom_String_ByteArray_AssemblyHashAlgorithm_TestData))]
        public void LoadFrom_InvokeStringByteArrayAssemblyHashAlgorithm_ThrowsNotSupportedException(string assemblyFile, byte[] hashValue, System.Configuration.Assemblies.AssemblyHashAlgorithm hashAlgorithm)
        {
            Assert.Throws<NotSupportedException>(() => Assembly.LoadFrom(assemblyFile, hashValue, hashAlgorithm));
        }

        public static IEnumerable<object[]> ReflectionOnlyLoad_ByteArray_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { Array.Empty<byte>() };
            yield return new object[] { new byte[] { 1, 2, 3 } };
        }

        [Theory]
        [MemberData(nameof(ReflectionOnlyLoad_ByteArray_TestData))]
        public void ReflectionOnlyLoad_InvokeByteArray_ThrowsPlatformNotSupportedException(byte[] rawAssembly)
        {
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoad(rawAssembly));
        }

        public static IEnumerable<object[]> ReflectionOnlyLoad_String_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { string.Empty };
            yield return new object[] { "assemblyString" };
        }

        [Theory]
        [MemberData(nameof(ReflectionOnlyLoad_String_TestData))]
        public void ReflectionOnlyLoad_InvokeString_ThrowsPlatformNotSupportedException(string assemblyString)
        {
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoad(assemblyString));
        }

        public static IEnumerable<object[]> ReflectionOnlyLoadFrom_String_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { string.Empty };
            yield return new object[] { "assemblyFile" };
        }

        [Theory]
        [MemberData(nameof(ReflectionOnlyLoadFrom_String_TestData))]
        public void ReflectionOnlyLoadFrom_Invoke_ThrowsPlatformNotSupportedException(string assemblyFile)
        {
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoadFrom(assemblyFile));
        }

        [Fact]
        public void UnsafeLoadFrom_NullAssemblyFile_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("assemblyFile", () => Assembly.UnsafeLoadFrom(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        public void UnsafeLoadFrom_InvalidAssemblyFile_ThrowsArgumentException(string assemblyFile)
        {
            Assert.Throws<ArgumentException>("path", () => Assembly.UnsafeLoadFrom(assemblyFile));
        }

        [Fact]
        public void UnsafeLoadFrom_NoSuchAssemblyFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => Assembly.UnsafeLoadFrom("NoSuchAssembly"));
        }

        private class AlwaysEqualsAssembly : SubAssembly
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubCustomAttributeData : CustomAttributeData
        {
        }

        private class IReflectableTypeType : CustomType, IReflectableType
        {
            public Func<TypeInfo> GetTypeInfoAction { get; set; }

            public TypeInfo GetTypeInfo() => GetTypeInfoAction();
        }

        private class CustomType : Type
        {
            public override Assembly Assembly => throw new NotImplementedException();

            public override string AssemblyQualifiedName => throw new NotImplementedException();

            public override Type BaseType => throw new NotImplementedException();

            public override string FullName => throw new NotImplementedException();

            public override Guid GUID => throw new NotImplementedException();

            public Module ModuleResult { get; set; }

            public override Module Module => ModuleResult;

            public override string Namespace => throw new NotImplementedException();

            public override Type UnderlyingSystemType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override Type GetElementType() => throw new NotImplementedException();

            public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

            public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override FieldInfo GetField(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

            public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override Type GetInterface(string name, bool ignoreCase) => throw new NotImplementedException();

            public override Type[] GetInterfaces() => throw new NotImplementedException();

            public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override Type GetNestedType(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

            public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

            protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();

            protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();

            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();

            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();

            protected override bool HasElementTypeImpl() => throw new NotImplementedException();

            protected override bool IsArrayImpl() => throw new NotImplementedException();

            protected override bool IsByRefImpl() => throw new NotImplementedException();

            protected override bool IsCOMObjectImpl() => throw new NotImplementedException();

            protected override bool IsPointerImpl() => throw new NotImplementedException();

            protected override bool IsPrimitiveImpl() => throw new NotImplementedException();
        }

        private class CustomModule : Module
        {
            public Func<Type[]> GetTypesAction { get; set; }

            public override Type[] GetTypes()
            {
                if (GetTypesAction == null)
                {
                    return base.GetTypes();
                }

                return GetTypesAction();
            }
        }

        private class CustomAssembly : SubAssembly
        {
            public Func<string> CodeBaseAction { get; set; }

            [Obsolete]
            public override string CodeBase
            {
                get
                {
                    if (CodeBaseAction == null)
                    {
                        return base.CodeBase;
                    }

                    return CodeBaseAction();
                }
            }
    
            public Func<string> FullNameAction { get; set; }

            public override string FullName
            {
                get
                {
                    if (FullNameAction == null)
                    {
                        return base.FullName;
                    }

                    return FullNameAction();
                }
            }

            public Func<string, bool, BindingFlags, Binder, object[], CultureInfo, object[], object> CreateInstanceAction { get; set; }

            public override object CreateInstance(string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
            {
                if (CreateInstanceAction == null)
                {
                    return base.CreateInstance(typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
                }

                return CreateInstanceAction(typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
            }

            public Func<Type, bool, object[]> GetCustomAttributesAction { get; set; }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                if (GetCustomAttributesAction == null)
                {
                    return base.GetCustomAttributes(attributeType, inherit);
                }

                return GetCustomAttributesAction(attributeType, inherit);
            }

            public Func<IList<CustomAttributeData>> GetCustomAttributesDataAction { get; set; }

            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                if (GetCustomAttributesDataAction == null)
                {
                    return base.GetCustomAttributesData();
                }

                return GetCustomAttributesDataAction();
            }

            public Func<Type[]> GetExportedTypesAction { get; set; }

            public override Type[] GetExportedTypes()
            {
                if (GetExportedTypesAction == null)
                {
                    return base.GetExportedTypes();
                }

                return GetExportedTypesAction();
            }

            public Func<bool, FileStream[]> GetFilesAction { get; set; }

            public override FileStream[] GetFiles(bool getResourceModules)
            {
                if (GetFilesAction == null)
                {
                    return base.GetFiles(getResourceModules);
                }

                return GetFilesAction(getResourceModules);
            }

            public Func<bool, Module[]> GetLoadedModulesAction { get; set; }

            public override Module[] GetLoadedModules(bool getResourceModules)
            {
                if (GetLoadedModulesAction == null)
                {
                    return base.GetLoadedModules(getResourceModules);
                }

                return GetLoadedModulesAction(getResourceModules);
            }

            public Func<Type, string, Stream> GetManifestResourceStreamAction { get; set; }

            public override Stream GetManifestResourceStream(Type type, string name)
            {
                if (GetManifestResourceStreamAction == null)
                {
                    return base.GetManifestResourceStream(type, name);
                }

                return GetManifestResourceStreamAction(type, name);
            }

            public Func<bool, Module[]> GetModulesAction { get; set; }

            public override Module[] GetModules(bool getResourceModules)
            {
                if (GetModulesAction == null)
                {
                    return base.GetModules(getResourceModules);
                }

                return GetModulesAction(getResourceModules);
            }

            public Func<bool, AssemblyName> GetNameAction { get; set; }

            public override AssemblyName GetName(bool copiedName)
            {
                if (GetNameAction == null)
                {
                    return base.GetName(copiedName);
                }

                return GetNameAction(copiedName);
            }

            public Func<CultureInfo, Version, Assembly> GetSatelliteAssemblyAction { get; set; }

            public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
            {
                if (GetSatelliteAssemblyAction == null)
                {
                    return base.GetSatelliteAssembly(culture, version);
                }

                return GetSatelliteAssemblyAction(culture, version);
            }

            public Func<string, bool, bool, Type> GetTypeAction { get; set; }

            public override Type GetType(string name, bool throwOnError, bool ignoreCase)
            {
                if (GetTypeAction == null)
                {
                    return base.GetType(name, throwOnError, ignoreCase);
                }

                return GetTypeAction(name, throwOnError, ignoreCase);
            }

            public Func<Type[]> GetTypesAction { get; set; }

            public override Type[] GetTypes()
            {
                if (GetTypesAction == null)
                {
                    return base.GetTypes();
                }

                return GetTypesAction();
            }

            public Func<string, byte[], byte[], Module> LoadModuleAction { get; set; }

            public override Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
            {
                if (LoadModuleAction == null)
                {
                    return base.LoadModule(moduleName, rawModule, rawSymbolStore);
                }

                return LoadModuleAction(moduleName, rawModule, rawSymbolStore);
            }
        }

        private class SubAssembly : Assembly
        {
        }
    }
}

#pragma warning restore SYSLIB0005, SYSLIB0012
