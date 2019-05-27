// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetHINSTANCETests
    {
        [Fact]
        public void GetHINSTANCE_NormalModule_ReturnsSameInstance()
        {
            IntPtr ptr = Marshal.GetHINSTANCE(typeof(int).Module);
            Assert.NotEqual(IntPtr.Zero, ptr);
            Assert.Equal(ptr, Marshal.GetHINSTANCE(typeof(string).Module));
        }

        [Fact]
        public void GetHINSTANCE_ModuleBuilder_ReturnsSameInstance()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");

            IntPtr ptr = Marshal.GetHINSTANCE(moduleBuilder);
            Assert.NotEqual(IntPtr.Zero, ptr);
            Assert.Equal(ptr, Marshal.GetHINSTANCE(moduleBuilder));
        }

        [Fact]
        public void GetHINSTANCE_NullModule_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("m", () => Marshal.GetHINSTANCE(null));
        }

        [Fact]
        public void GetHINSTANCE_NonRuntimeModule_Returns_IntPtrMinusOne()
        {
            Assert.Equal((IntPtr)(-1), Marshal.GetHINSTANCE(new NonRuntimeModule()));
        }

        private class NonRuntimeModule : Module
        {
            public NonRuntimeModule()
            {
            }
        }
    }
}
