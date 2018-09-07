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
    public partial class ChangeWrapperHandleStrengthTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ChangeWrapperHandleStrength_ObjectNotCollectible_ThrowsNotSupportedException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type type = typeBuilder.CreateType();

            object o = Activator.CreateInstance(type);
            Assert.Throws<NotSupportedException>(() => Marshal.ChangeWrapperHandleStrength(o, fIsWeak: true));
        }
    }
}