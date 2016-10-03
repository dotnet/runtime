// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Tests
{
    public static partial class EnumTests
    {
        [Theory]
        [MemberData(nameof(Parse_TestData))]
        public static void Parse_NetCoreApp11<T>(string value, bool ignoreCase, T expected) where T : struct
        {
            object result;
            if (!ignoreCase)
            {
                Assert.True(Enum.TryParse(expected.GetType(), value, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, Enum.Parse<T>(value));
            }

            Assert.True(Enum.TryParse(expected.GetType(), value, ignoreCase, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, Enum.Parse<T>(value, ignoreCase));
        }
        
        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid_NetCoreApp11(Type enumType, string value, bool ignoreCase, Type exceptionType)
        {
            Type typeArgument = enumType == null || !enumType.GetTypeInfo().IsEnum ? typeof(SimpleEnum) : enumType;
            MethodInfo parseMethod = typeof(EnumTests).GetTypeInfo().GetMethod(nameof(Parse_Generic_Invalid_NetCoreApp11)).MakeGenericMethod(typeArgument);
            parseMethod.Invoke(null, new object[] { enumType, value, ignoreCase, exceptionType });
        }

        public static void Parse_Generic_Invalid_NetCoreApp11<T>(Type enumType, string value, bool ignoreCase, Type exceptionType) where T : struct
        {
            object result = null;
            if (!ignoreCase)
            {
                if (enumType != null && enumType.IsEnum)
                {
                    Assert.False(Enum.TryParse(enumType, value, out result));
                    Assert.Equal(default(object), result);

                    Assert.Throws(exceptionType, () => Enum.Parse<T>(value));
                }
                else
                {
                    Assert.Throws(exceptionType, () => Enum.TryParse(enumType, value, out result));
                    Assert.Equal(default(object), result);
                }
            }

            if (enumType != null && enumType.IsEnum)
            {
                Assert.False(Enum.TryParse(enumType, value, ignoreCase, out result));
                Assert.Equal(default(object), result);

                Assert.Throws(exceptionType, () => Enum.Parse<T>(value, ignoreCase));
            }
            else
            {
                Assert.Throws(exceptionType, () => Enum.TryParse(enumType, value, ignoreCase, out result));
                Assert.Equal(default(object), result);
            }
        }
    }
}
