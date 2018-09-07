// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

#pragma warning disable 618

namespace System.Runtime.InteropServices.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "GetNativeVariantForObject() not supported on UWP")]
    public partial class GetNativeVariantForObjectTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariantForObject_ObjectNotCollectible_ThrowsNotSupportedException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type type = typeBuilder.CreateType();

            object o = Activator.CreateInstance(type);

            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Assert.Throws<NotSupportedException>(() => Marshal.GetNativeVariantForObject(o, pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }  
    }
}

#pragma warning restore 618
