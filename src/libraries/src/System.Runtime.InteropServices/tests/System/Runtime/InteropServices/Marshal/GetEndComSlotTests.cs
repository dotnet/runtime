// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetEndComSlotTests
    {
        [Theory]
        [InlineData(typeof(int), -1)]
        [InlineData(typeof(string), -1)]
        [InlineData(typeof(NonGenericClass), -1)]
        [InlineData(typeof(NonGenericStruct), -1)]
        [InlineData(typeof(NonGenericInterface), 6)]
        [InlineData(typeof(int*), -1)]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue(31068, ~TargetFrameworkMonikers.NetFramework)]
        public void GetEndComSlot_ValidType_ReturnsExpected(Type type, int expected)
        {
            Assert.Equal(expected, Marshal.GetEndComSlot(type));
        }
        
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetEndComSlot_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetEndComSlot(null));
        }
        
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetEndComSlot_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => Marshal.GetEndComSlot(null));
        }
        
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetEndComSlot_NotRuntimeType_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetEndComSlot(typeBuilder));
        }
        
        public static IEnumerable<object[]> GetStartComSlot_InvalidGenericType_TestData()
        {
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };
        }
        
        [Theory]
        [MemberData(nameof(GetStartComSlot_InvalidGenericType_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetEndComSlot_InvalidGenericType_ThrowsArgumentNullException(Type type)
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => Marshal.GetEndComSlot(type));
        }
        public static IEnumerable<object[]> GetStartComSlot_NotComVisibleType_TestData()
        {
            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<string>) };
            yield return new object[] { typeof(GenericStruct<>) };
            yield return new object[] { typeof(GenericStruct<string>) };
            yield return new object[] { typeof(GenericInterface<>) };
            yield return new object[] { typeof(GenericInterface<string>) };
            yield return new object[] { typeof(NonComVisibleClass) };
            yield return new object[] { typeof(NonComVisibleStruct) };
            yield return new object[] { typeof(NonComVisibleInterface) };
            yield return new object[] { typeof(int[]) };
            yield return new object[] { typeof(int[][]) };
            yield return new object[] { typeof(int[,]) };

#if !netstandard // TODO: Enable for netstandard2.1
             AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            yield return new object[] { collectibleType };
#endif
        }
        
        [Theory]
        [MemberData(nameof(GetStartComSlot_NotComVisibleType_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetEndComSlot_NotComVisibleType_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetEndComSlot(type));
        }
    }
}
