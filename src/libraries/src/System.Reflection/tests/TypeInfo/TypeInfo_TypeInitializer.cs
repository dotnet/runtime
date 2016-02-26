// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class TypeInfo_TypeInitializer
    {
        [Fact]
        public static void TypeInitializer()
        {
            ConstructorInfo cInfo = typeof(ClassWithStaticCtor).GetTypeInfo().TypeInitializer;
            Assert.NotNull(cInfo);
            Assert.Equal(cInfo.Name, ".cctor");

            cInfo = typeof(ClassWithoutStaticCtor).GetTypeInfo().TypeInitializer;
            Assert.Null(cInfo);
        }

        public class ClassWithStaticCtor
        {
            static ClassWithStaticCtor() { }
        }

        public class ClassWithoutStaticCtor
        {
        }
    }
}