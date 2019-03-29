// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class AppDomainTests
    {
        [Fact]
        public void GetSetupInformation()
        {
            RemoteExecutor.Invoke(() => {
                Assert.Equal(AppContext.BaseDirectory, AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                Assert.Equal(AppContext.TargetFrameworkName, AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName);
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void GetPermissionSet()
        {
            RemoteExecutor.Invoke(() => {
                Assert.Equal(new PermissionSet(PermissionState.Unrestricted), AppDomain.CurrentDomain.PermissionSet);
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }    
 
        [Theory]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleData))]
        public static void TestingCreateInstanceFromObjectHandle(string physicalFileName, string assemblyFile, string type, string returnedFullNameType, Type exceptionType)
        {
            ObjectHandle oh = null;
            object obj = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type);
                CheckValidity(obj, returnedFullNameType);
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type, null));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type, null);
                CheckValidity(obj, returnedFullNameType);
            }
            Assert.True(File.Exists(physicalFileName));
        }

        public static TheoryData<string, string, string, string, Type> TestingCreateInstanceFromObjectHandleData => new TheoryData<string, string, string, string, Type>
        {
            // string physicalFileName, string assemblyFile, string typeName, returnedFullNameType, expectedException
            { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassSample", "AssemblyResolveTestApp.PublicClassSample", null },
            { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclasssample", "AssemblyResolveTestApp.PublicClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PrivateClassSample", "AssemblyResolveTestApp.PrivateClassSample", null },
            { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.privateclasssample", "AssemblyResolveTestApp.PrivateClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(MissingMethodException) },
            { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclassnodefaultconstructorsample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(TypeLoadException) }
        };

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleData))]
        public static void TestingCreateInstanceObjectHandle(string assemblyName, string type, string returnedFullNameType, Type exceptionType)
        {
            ObjectHandle oh = null;
            object obj = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type);
                CheckValidity(obj, returnedFullNameType);
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type, null));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type, null));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type, null);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type, null);
                CheckValidity(obj, returnedFullNameType);
            }
        }

        public static TheoryData<string, string, string, Type> TestingCreateInstanceObjectHandleData => new TheoryData<string, string, string, Type>()
        {
            // string assemblyName, string typeName, returnedFullNameType, expectedException
            { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassSample", "AssemblyResolveTestApp.PublicClassSample", null },
            { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclasssample", "AssemblyResolveTestApp.PublicClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PrivateClassSample", "AssemblyResolveTestApp.PrivateClassSample", null },
            { "assemblyresolvetestapp", "assemblyresolvetestapp.privateclasssample", "AssemblyResolveTestApp.PrivateClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(MissingMethodException) },
            { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclassnodefaultconstructorsample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(TypeLoadException) }
        };

        [Theory]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleFullSignatureData))]
        public static void TestingCreateInstanceFromObjectHandleFullSignature(string physicalFileName, string assemblyFile, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType)
        {
            ObjectHandle oh = AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(oh, returnedFullNameType);

            object obj = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(obj, returnedFullNameType);

            Assert.True(File.Exists(physicalFileName));
        }

        public static IEnumerable<object[]> TestingCreateInstanceFromObjectHandleFullSignatureData()
        {
            // string physicalFileName, string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };

            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
        }

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleFullSignatureData))]
        public static void TestingCreateInstanceObjectHandleFullSignature(string assemblyName, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType)
        {
            ObjectHandle oh = AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(oh, returnedFullNameType);

            object obj = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(obj, returnedFullNameType);
        }

        private static void CheckValidity(object instance, string expected)
        {
            Assert.NotNull(instance);
            Assert.Equal(expected, instance.GetType().FullName);
        }

        private static void CheckValidity(ObjectHandle instance, string expected)
        {
            Assert.NotNull(instance);
            Assert.Equal(expected, instance.Unwrap().GetType().FullName);
        }

        public static IEnumerable<object[]> TestingCreateInstanceObjectHandleFullSignatureData()
        {
            // string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };

            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
        }
    }
}
