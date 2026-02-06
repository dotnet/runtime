// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method body for the GetCanonType intrinsic.
    /// </summary>
    public static class GetCanonTypeIntrinsic
    {
        public static MethodIL EmitIL(MethodDesc target)
        {
            Debug.Assert(target.Signature.Length == 0);

            ILEmitter emitter = new ILEmitter();
            var codeStream = emitter.NewCodeStream();

            TypeSystemContext context = target.Context;
            TypeDesc runtimeTypeHandleType = context.GetWellKnownType(WellKnownType.RuntimeTypeHandle);
            Debug.Assert(target.Signature.ReturnType == runtimeTypeHandleType);

            codeStream.Emit(ILOpcode.ldtoken, emitter.NewToken(context.CanonType));
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(target);
        }
    }
}
