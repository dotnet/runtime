// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler
{
    internal static partial class DelegateHelpers
    {
#if FEATURE_COMPILE
        private const MethodAttributes CtorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
        private const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
        private const MethodAttributes InvokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
        private static readonly Type[] s_delegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };
#endif

        private static Type MakeNewCustomDelegate(Type[] types)
        {
#if FEATURE_COMPILE
            Type returnType = types[types.Length - 1];
            Type[] parameters = types.RemoveLast();

            TypeBuilder builder = AssemblyGen.DefineDelegateType("Delegate" + types.Length);
            builder.DefineConstructor(CtorAttributes, CallingConventions.Standard, s_delegateCtorSignature).SetImplementationFlags(ImplAttributes);
            builder.DefineMethod("Invoke", InvokeAttributes, returnType, parameters).SetImplementationFlags(ImplAttributes);
            return builder.CreateTypeInfo();
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
