// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class ActivatorTests
    {
        [Fact]
        public void CreateInstance_NonPublicValueTypeWithPrivateDefaultConstructor_Success()
        {
            AssemblyName assemblyName = new AssemblyName("Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Public, typeof(ValueType));
            
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_field", typeof(int), FieldAttributes.Public);
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, new Type[0]);

            ILGenerator generator = constructorBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4, -1);
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            FieldInfo field = type.GetField("_field");

            // Activator holds a cache of constructors and the types to which they belong.
            // Test caching behaviour by activating multiple times.
            object v1 = Activator.CreateInstance(type, nonPublic: true);
            Assert.Equal(-1, field.GetValue(v1));

            object v2 = Activator.CreateInstance(type, nonPublic: true);
            Assert.Equal(-1, field.GetValue(v2));
        }

        [Fact]
        public void CreateInstance_PublicOnlyValueTypeWithPrivateDefaultConstructor_ThrowsMissingMethodException()
        {
            AssemblyName assemblyName = new AssemblyName("Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Public, typeof(ValueType));
            
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_field", typeof(int), FieldAttributes.Public);
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, new Type[0]);

            ILGenerator generator = constructorBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4, -1);
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type, nonPublic: false));

            // Put the private default constructor into the cache and make sure we still throw if public only.
            Assert.NotNull(Activator.CreateInstance(type, nonPublic: true));

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type, nonPublic: false));
        }

        [Theory]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleData))]
        public static void TestingCreateInstanceFromObjectHandle(string physicalFileName, string assemblyFile, string type, string returnedFullNameType, Type exceptionType)
        {
            ObjectHandle oh = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type));
            }
            else
            {
                oh = Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type);
                CheckValidity(oh, returnedFullNameType);
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null));
            }
            else
            {
                oh = Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null);
                CheckValidity(oh, returnedFullNameType);
            }
            Assert.True(File.Exists(physicalFileName));
        }

        public static TheoryData<string, string, string, string, Type> TestingCreateInstanceFromObjectHandleData => new TheoryData<string, string, string, string, Type>()
        {
            // string physicalFileName, string assemblyFile, string typeName, returnedFullNameType, expectedException
            { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassSample", "PublicClassSample", null },
            { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclasssample", "PublicClassSample", typeof(TypeLoadException) },

            { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PrivateClassSample", "PrivateClassSample", null },
            { "TestLoadAssembly.dll", "testloadassembly.dll", "privateclasssample", "PrivateClassSample", typeof(TypeLoadException) },

            { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassNoDefaultConstructorSample", "PublicClassNoDefaultConstructorSample", typeof(MissingMethodException) },
            { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclassnodefaultconstructorsample", "PublicClassNoDefaultConstructorSample", typeof(TypeLoadException) }
        };

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleData))]
        public static void TestingCreateInstanceObjectHandle(string assemblyName, string type, string returnedFullNameType, Type exceptionType, bool returnNull)
        {
            ObjectHandle oh = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstance(assemblyName: assemblyName, typeName: type));
            }
            else
            {
                oh = Activator.CreateInstance(assemblyName: assemblyName, typeName: type);
                if (returnNull)
                {
                    Assert.Null(oh);
                }
                else
                {
                    CheckValidity(oh, returnedFullNameType);
                }
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstance(assemblyName: assemblyName, typeName: type, null));
            }
            else
            {
                oh = Activator.CreateInstance(assemblyName: assemblyName, typeName: type, null);
                if (returnNull)
                {
                    Assert.Null(oh);
                }
                else
                {
                    CheckValidity(oh, returnedFullNameType);
                }
            }
        }

        public static TheoryData<string, string, string, Type, bool> TestingCreateInstanceObjectHandleData => new TheoryData<string, string, string, Type, bool>()
        {
            // string assemblyName, string typeName, returnedFullNameType, expectedException
            { "TestLoadAssembly", "PublicClassSample", "PublicClassSample", null, false },
            { "testloadassembly", "publicclasssample", "PublicClassSample", typeof(TypeLoadException), false },

            { "TestLoadAssembly", "PrivateClassSample", "PrivateClassSample", null, false },
            { "testloadassembly", "privateclasssample", "PrivateClassSample", typeof(TypeLoadException), false },

            { "TestLoadAssembly", "PublicClassNoDefaultConstructorSample", "PublicClassNoDefaultConstructorSample", typeof(MissingMethodException), false },
            { "testloadassembly", "publicclassnodefaultconstructorsample", "PublicClassNoDefaultConstructorSample", typeof(TypeLoadException), false },

            { "mscorlib", "System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", "", null, true }
        };

        [Theory]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleFullSignatureData))]
        public static void TestingCreateInstanceFromObjectHandleFullSignature(string physicalFileName, string assemblyFile, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType)
        {
            ObjectHandle oh = Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(oh, returnedFullNameType);
            Assert.True(File.Exists(physicalFileName));
        }

        public static IEnumerable<object[]> TestingCreateInstanceFromObjectHandleFullSignatureData()
        {
            // string physicalFileName, string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" };

            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample" };            
        }

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleFullSignatureData))]
        public static void TestingCreateInstanceObjectHandleFullSignature(string assemblyName, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType, bool returnNull)
        {
            ObjectHandle oh = Activator.CreateInstance(assemblyName: assemblyName, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            if (returnNull)
            {
                Assert.Null(oh);
            }
            else
            {
                CheckValidity(oh, returnedFullNameType);
            }
        }

        public static IEnumerable<object[]> TestingCreateInstanceObjectHandleFullSignatureData()
        {
            // string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "TestLoadAssembly", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" , false };
            yield return new object[] { "testloadassembly", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" , false };
            yield return new object[] { "TestLoadAssembly", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" , false };
            yield return new object[] { "testloadassembly", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" , false };            

            yield return new object[] { "TestLoadAssembly", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample", false };
            yield return new object[] { "testloadassembly", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample", false };
            yield return new object[] { "TestLoadAssembly", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample", false };
            yield return new object[] { "testloadassembly", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample", false };

            yield return new object[] { null, typeof(PublicType).FullName, false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, typeof(PublicType).FullName, false };
            yield return new object[] { null, typeof(PrivateType).FullName, false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, typeof(PrivateType).FullName, false };

            yield return new object[] { "mscorlib", "System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "", true };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWinRTSupported), nameof(PlatformDetection.IsNotWindows8x), nameof(PlatformDetection.IsNotWindowsServerCore), nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotWindowsIoTCore))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(TestingCreateInstanceObjectHandleFullSignatureWinRTData))]
        public static void TestingCreateInstanceObjectHandleFullSignatureWinRT(string assemblyName, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType)
        {
            ObjectHandle oh = Activator.CreateInstance(assemblyName: assemblyName, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(oh, returnedFullNameType);
        }

        public static IEnumerable<object[]> TestingCreateInstanceObjectHandleFullSignatureWinRTData()
        {
            // string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime", "Windows.Foundation.Collections.StringMap", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "Windows.Foundation.Collections.StringMap" };
        }

        private static void CheckValidity(ObjectHandle instance, string expected)
        {
            Assert.NotNull(instance);
            Assert.Equal(expected, instance.Unwrap().GetType().FullName);
        }

        public class PublicType
        {
            public PublicType() { }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.LoadFile is not supported in AppX.")]
        public static void CreateInstanceAssemblyResolve()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) => Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "TestLoadAssembly.dll"));
                Assert.Throws<FileLoadException>(() => Activator.CreateInstance(",,,,", "PublicClassSample"));
            }).Dispose();
        }

        [Fact]
        public void CreateInstance_TypeBuilder_ThrowsNotSupportedException()
        {
            AssemblyName assemblyName = new AssemblyName("Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Public);

            Assert.Throws<ArgumentException>("type", () => Activator.CreateInstance(typeBuilder));
            Assert.Throws<NotSupportedException>(() => Activator.CreateInstance(typeBuilder, new object[0]));
        }
    }
}
