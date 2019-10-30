// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security;

using Xunit;

namespace System.Tests
{
    public static class SecurityExceptionTests
    {
        private const int COR_E_SECURITY = unchecked((int)0x8013150A);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new SecurityException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_SECURITY, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "security problem";
            var exception = new SecurityException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_SECURITY, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "security problem";
            var innerException = new Exception("Inner exception");
            var exception = new SecurityException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_SECURITY, innerException: innerException, message: message);
        }

        [Fact]
        public static void Ctor_String_Type()
        {
            string message = "security problem";
            var exception = new SecurityException(message, typeof(SecurityExceptionTests));
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_SECURITY, message: message);
            Assert.Equal(typeof(SecurityExceptionTests), exception.PermissionType);
        }

        [Fact]
        public static void Ctor_String_Type_String()
        {
            string message = "security problem";
            var exception = new SecurityException(message, typeof(SecurityExceptionTests), "permission state");
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_SECURITY, message: message);
            Assert.Equal(typeof(SecurityExceptionTests), exception.PermissionType);
            Assert.Equal("permission state", exception.PermissionState);
        }

        [Fact]
        public static void Properties()
        {
            var exception = new SecurityException();

            Assert.Null(exception.Demanded);
            exception.Demanded = "Demanded";
            Assert.Equal("Demanded", exception.Demanded);
            exception.Demanded = null;
            Assert.Null(exception.Demanded);

            Assert.Null(exception.DenySetInstance);
            exception.DenySetInstance = "DenySetInstance";
            Assert.Equal("DenySetInstance", exception.DenySetInstance);
            exception.DenySetInstance = null;
            Assert.Null(exception.DenySetInstance);

            var assemblyName = new AssemblyName("MyAssembly");
            Assert.Null(exception.FailedAssemblyInfo);
            exception.FailedAssemblyInfo = assemblyName;
            Assert.Equal(assemblyName, exception.FailedAssemblyInfo);
            exception.FailedAssemblyInfo = null;
            Assert.Null(exception.FailedAssemblyInfo);

            Assert.Null(exception.GrantedSet);
            exception.GrantedSet = "GrantedSet";
            Assert.Equal("GrantedSet", exception.GrantedSet);
            exception.GrantedSet = null;
            Assert.Null(exception.GrantedSet);

            MethodInfo methodInfo = typeof(SecurityExceptionTests).GetMethod("Properties");
            Assert.Null(exception.Method);
            exception.Method = methodInfo;
            Assert.Equal(methodInfo, exception.Method);
            exception.Method = null;
            Assert.Null(exception.Method);

            Assert.Null(exception.PermissionState);
            exception.PermissionState = "PermissionState";
            Assert.Equal("PermissionState", exception.PermissionState);
            exception.PermissionState = null;
            Assert.Null(exception.PermissionState);

            Type type = typeof(SecurityExceptionTests);
            Assert.Null(exception.PermissionType);
            exception.PermissionType = type;
            Assert.Equal(type, exception.PermissionType);
            exception.PermissionType = null;
            Assert.Null(exception.PermissionType);

            Assert.Null(exception.PermitOnlySetInstance);
            exception.PermitOnlySetInstance = "PermitOnlySetInstance";
            Assert.Equal("PermitOnlySetInstance", exception.PermitOnlySetInstance);
            exception.PermitOnlySetInstance = null;
            Assert.Null(exception.PermitOnlySetInstance);

            Assert.Null(exception.RefusedSet);
            exception.RefusedSet = "RefusedSet";
            Assert.Equal("RefusedSet", exception.RefusedSet);
            exception.RefusedSet = null;
            Assert.Null(exception.RefusedSet);

            Assert.Null(exception.Url);
            exception.Url = "Url";
            Assert.Equal("Url", exception.Url);
            exception.Url = null;
            Assert.Null(exception.Url);
        }
    }
}
