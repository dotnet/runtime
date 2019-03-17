// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Tests
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
            new object[] { typeof(Test<>).GetGenericArguments()[0].GetTypeInfo(), 0x2A }
        };

        [ConditionalTheory(nameof(GetMetadataTokenSupported))]
        [MemberData(nameof(MembersWithExpectedTableIndex))]
        public void SuccessImpliesNonNilWithCorrectTable(MemberInfo member, int expectedTableIndex)
        {
            Assert.True(member.HasMetadataToken());
            int token = member.GetMetadataToken();
            Assert.Equal(expectedTableIndex, TableIndex(token));
            Assert.NotEqual(0, TableIndex(token));
        }

        [ConditionalFact(nameof(GetMetadataTokenSupported), nameof(IsReflectionEmitSupported))]
        public static void UnbakedReflectionEmitType_HasNoMetadataToken()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("dynamic"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("dynamic.dll");
            TypeBuilder type = module.DefineType("T");
            MethodInfo method = type.DefineMethod("M", MethodAttributes.Public);
            Assert.False(method.HasMetadataToken());
            Assert.Throws<InvalidOperationException>(() => method.GetMetadataToken());
        }

        public static bool GetMetadataTokenSupported
        {
            get
            {
                if (!PlatformDetection.IsNetNative)
                    return true;

                // Expected false but in case .NET Native ever changes its mind...
                return typeof(MetadataTokenTests).HasMetadataToken();
            }
        }

        public static bool IsReflectionEmitSupported => PlatformDetection.IsReflectionEmitSupported;

        private static int TableIndex(int token) => token >> 24;
        private static int RowIndex(int token) => token & 0x00FFFFFF;
    }
}
