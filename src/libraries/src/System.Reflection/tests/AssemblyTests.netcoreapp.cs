// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

[assembly:TypeForwardedTo(typeof(string))]
[assembly: TypeForwardedTo(typeof(TypeInForwardedAssembly))]

namespace System.Reflection.Tests
{
    public static class AssemblyNetCoreAppTests
    {
        [Fact]
        public static void AssemblyGetForwardedTypes()
        {
            Assembly a = typeof(AssemblyNetCoreAppTests).Assembly;
            Type[] forwardedTypes = a.GetForwardedTypes();

            forwardedTypes = forwardedTypes.OrderBy(t => t.FullName).ToArray();

            Type[] expected = { typeof(string), typeof(TypeInForwardedAssembly), typeof(TypeInForwardedAssembly.PublicInner), typeof(TypeInForwardedAssembly.PublicInner.PublicInnerInner) };
            expected = expected.OrderBy(t => t.FullName).ToArray();

            Assert.Equal<Type>(expected, forwardedTypes);
        }

        [Fact]
        public static void AssemblyGetForwardedTypesLoadFailure()
        {
            Assembly a = typeof(TypeInForwardedAssembly).Assembly;
            ReflectionTypeLoadException rle = Assert.Throws<ReflectionTypeLoadException>(() => a.GetForwardedTypes());
            Assert.Equal(2, rle.Types.Length);
            Assert.Equal(2, rle.LoaderExceptions.Length);

            bool foundSystemObject = false;
            bool foundBifException = false;
            for (int i = 0; i < rle.Types.Length; i++)
            {
                Type type = rle.Types[i];
                Exception exception = rle.LoaderExceptions[i];

                if (type == typeof(object) && exception == null)
                    foundSystemObject = true;

                if (type == null && exception is BadImageFormatException)
                    foundBifException = true;
            }

            Assert.True(foundSystemObject);
            Assert.True(foundBifException);
        }
    }
}
