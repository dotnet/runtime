// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetDelegateForFunctionPointerTests
    {
        [Fact]
        public void GetDelegateForFunctionPointer_CollectibleType_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetDelegateForFunctionPointerTests).GetMethod(nameof(Method));
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(object), typeof(IntPtr) });
            constructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, targetMethod.ReturnType, targetMethod.GetParameters().Select(p => p.ParameterType).ToArray());
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            Type type = typeBuilder.CreateType();

            Delegate functionDelegate = Marshal.GetDelegateForFunctionPointer(ptr, type);
            GC.KeepAlive(d);
            VerifyDelegate(functionDelegate, targetMethod);
        }
    }
}
