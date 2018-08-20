// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
    public partial class ByteTests
    {
        [Fact]
        public void ReadByte_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.ReadByte(collectibleObject, 0));
        }

        [Fact]
        public void WriteByte_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.WriteByte(collectibleObject, 0, 0));
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
