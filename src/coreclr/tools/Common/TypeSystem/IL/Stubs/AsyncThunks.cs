// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public static class AsyncThunkILEmitter
    {
        public static MethodIL EmitTaskReturningThunk(MethodDesc taskReturningMethod, MethodDesc asyncMethod)
        {
            TypeSystemContext context = taskReturningMethod.Context;

            var emitter = new ILEmitter();
            var codestream = emitter.NewCodeStream();

            // TODO: match EmitTaskReturningThunk in CoreCLR VM

            MethodSignature sig = asyncMethod.Signature;
            int numParams = (sig.IsStatic || sig.IsExplicitThis) ? sig.Length : sig.Length + 1;
            for (int i = 0; i < numParams; i++)
                codestream.EmitLdArg(i);

            codestream.Emit(ILOpcode.call, emitter.NewToken(asyncMethod));

            if (sig.ReturnType.IsVoid)
            {
                codestream.Emit(ILOpcode.call, emitter.NewToken(context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("get_CompletedTask"u8, null)));
            }
            else
            {
                codestream.Emit(ILOpcode.call, emitter.NewToken(context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("FromResult"u8, null).MakeInstantiatedMethod(sig.ReturnType)));
            }

            codestream.Emit(ILOpcode.ret);

            return emitter.Link(taskReturningMethod);
        }
    }
}
