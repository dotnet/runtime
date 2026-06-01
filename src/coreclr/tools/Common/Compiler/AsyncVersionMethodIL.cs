// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public sealed class AsyncVersionMethodIL : MethodIL
    {
        private readonly MethodDesc _variant;
        private readonly MethodIL _ecmaIL;
        private readonly MethodIL _unwrappedIL;

        public MethodIL WrappedIL => _ecmaIL;

        public AsyncVersionMethodIL(MethodDesc variant, MethodIL ecmaIL, MethodIL thunkIL)
            => (_variant, _ecmaIL, _unwrappedIL) = (variant, ecmaIL, thunkIL);

        // This is the reason we need this class - the method that owns the IL is the variant.
        public override MethodDesc OwningMethod => _variant;

        // Everything else dispatches to MethodIL
        public override MethodDebugInformation GetDebugInfo() => _ecmaIL.GetDebugInfo();
        public override ILExceptionRegion[] GetExceptionRegions() => _ecmaIL.GetExceptionRegions();
        public override byte[] GetILBytes() => _ecmaIL.GetILBytes();
        public override LocalVariableDefinition[] GetLocals() => _ecmaIL.GetLocals();
        public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw) => _ecmaIL.GetObject(token, notFoundBehavior);
        public override bool IsInitLocals => _ecmaIL.IsInitLocals;
        public override int MaxStack => _ecmaIL.MaxStack;

        public static MethodIL GetWrappedIfAsyncVersion(MethodDesc method, Compilation compilation)
        {
            if (method.SupportsAsyncVersionCodegen())
            {
                MethodDesc targetMethod = method.GetTargetOfAsyncVariant();

                MethodDesc methodDef = method.GetTypicalMethodDefinition();
                MethodDesc targetMethodDef = targetMethod.GetTypicalMethodDefinition();
                MethodIL methodIL = new AsyncVersionMethodIL(
                    methodDef,
                    compilation.GetMethodIL(targetMethodDef),
                    compilation.GetMethodIL(method));
                if (method != methodDef)
                {
                    methodIL = new InstantiatedMethodIL(method, methodIL);
                }

                return methodIL;
            }

            return compilation.GetMethodIL(method);
        }

        public static MethodIL UnwrapIfAsyncVersion(MethodIL il)
        {
            AsyncVersionMethodIL asIL =
                il switch
                {
                    InstantiatedMethodIL instIL => instIL.GetMethodILDefinition() as AsyncVersionMethodIL,
                    AsyncVersionMethodIL asyncIL => asyncIL,
                    _ => null
                };

            if (asIL != null)
            {
                return asIL._unwrappedIL;
            }

            return il;
        }
    }
}
