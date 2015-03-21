// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler
{
    internal static partial class DelegateHelpers
    {
#if FEATURE_CORECLR
        private const MethodAttributes CtorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
        private const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
        private const MethodAttributes InvokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
        private static readonly Type[] s_delegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };
#endif

        private static Type MakeNewCustomDelegate(Type[] types)
        {
#if FEATURE_CORECLR
            Type returnType = types[types.Length - 1];
            Type[] parameters = types.RemoveLast();

            TypeBuilder builder = AssemblyGen.DefineDelegateType("Delegate" + types.Length);
            builder.DefineConstructor(CtorAttributes, CallingConventions.Standard, s_delegateCtorSignature).SetImplementationFlags(ImplAttributes);
            builder.DefineMethod("Invoke", InvokeAttributes, returnType, parameters).SetImplementationFlags(ImplAttributes);
            return builder.CreateTypeInfo().AsType();
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
