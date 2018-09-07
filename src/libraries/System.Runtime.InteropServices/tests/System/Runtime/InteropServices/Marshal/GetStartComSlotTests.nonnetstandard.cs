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
    public partial class GetStartComSlotTests
    {
        public static IEnumerable<object[]> GetStartComSlot_NotComVisibleType_TestData()
        {
             AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            yield return new object[] { collectibleType };
        }
        
        [Theory]
        [MemberData(nameof(GetStartComSlot_NotComVisibleType_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetStartComSlot_NotComVisibleType_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetStartComSlot(type));
        }
    }
}
