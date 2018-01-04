// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: TypeForwardedTo(typeof(string))]
[assembly: TypeForwardedTo(typeof(TypeInForwardedAssembly))]

namespace System.Reflection.Tests
{
    public static class ReflectionTypeLoadExceptionTests
    {
        [Fact]
        public static void NullExceptionsNoNullPointerException()
        {
            bool foundRtleException = false;
            try
            {
                Type[] Typo = new Type[1];
                Exception[] Excepto = new Exception[1];
                throw new ReflectionTypeLoadException(Typo, Excepto, "Null elements in Exceptions array");

            }
            catch (ReflectionTypeLoadException)
            {
                foundRtleException = true;
            }
            Assert.True(foundRtleException);
        }

        [Fact]
        public static void NullArgumentsNoNullPointerException()
        {
            bool foundRtleException = false;
            try
            {
                throw new ReflectionTypeLoadException(null, null, "Null arguments");

            }
            catch (ReflectionTypeLoadException)
            {
                foundRtleException = true;
            }
            Assert.True(foundRtleException);
        }
    }
}
