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
    public partial class IsComObjectTests
    {
        public static IEnumerable<object[]> IsComObject_TestData()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);
            yield return new object[] { collectibleObject };
            
            ConstructorInfo comImportConstructor = typeof(ComImportAttribute).GetConstructor(new Type[0]);
            var comImportAttributeBuilder = new CustomAttributeBuilder(comImportConstructor, new object[0]);

            AssemblyBuilder comImportAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder comImportModuleBuilder = comImportAssemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder comImportTypeBuilder = comImportModuleBuilder.DefineType("Type");
            comImportTypeBuilder.SetCustomAttribute(comImportAttributeBuilder);

            Type collectibleComImportObject = comImportTypeBuilder.CreateType();
            yield return new object[] { collectibleComImportObject };
        }

        [Theory]
        [MemberData(nameof(IsComObject_TestData))]
        public void IsComObject_NonComObject_ReturnsFalse(object value)
        {
            Assert.False(Marshal.IsComObject(value));
        }
    }
}
