// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using Xunit;

namespace System.Reflection.TypeExtensions.Tests
{
    public class MetadataTokenTests
    {
        private class Test<T> { }

        public int Field;
        public int Property { get; set; }
        public event EventHandler Event { add { } remove { } }

        public static readonly object[][] MembersWithExpectedTableIndex = new object[][]
        {
            new object[] { typeof(MetadataTokenTests).GetTypeInfo(), 0x02 },
            new object[] { typeof(MetadataTokenTests).GetMethods()[0], 0x06 },
            new object[] { typeof(MetadataTokenTests).GetProperties()[0], 0x17 },
            new object[] { typeof(MetadataTokenTests).GetEvents()[0], 0x14 },
            new object[] { typeof(MetadataTokenTests).GetFields()[0], 0x04 },
        };

        [Fact]
        public void SuccessImpliesNonNilWithCorrectTable_GenericArgument()
        {
            // This should just be another entry in MembersWithExpectedTableIndex above
            // but that's blocked by https://github.com/xunit/xunit/issues/634
            SuccessImpliesNonNilWithCorrectTable(typeof(Test<>).GetGenericArguments()[0].GetTypeInfo(), 0x2A);
        }

        [Theory]
        [MemberData(nameof(MembersWithExpectedTableIndex))]
        public void SuccessImpliesNonNilWithCorrectTable(MemberInfo member, int expectedTableIndex)
        {

            Assert.True(member.HasMetadataToken());
            int token = member.GetMetadataToken();
            Assert.Equal(expectedTableIndex, TableIndex(token));
            Assert.NotEqual(0, TableIndex(token));
        }

        [Fact]
        public static void NoTokenForUnbakedRefEmit()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("dynamic"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("dynamic.dll");
            TypeBuilder type = module.DefineType("T");
            MethodInfo method = type.DefineMethod("M", MethodAttributes.Public);
            Assert.False(method.HasMetadataToken());
            Assert.Throws<InvalidOperationException>(() => method.GetMetadataToken());
        }

        private static int TableIndex(int token)
        {
            return token >> 24;
        }

        private static int RowIndex(int token)
        {
            return token & 0x00FFFFFF;
        }
    }
}
