// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Dataflow
{
    /// <summary>
    /// Removes the concept of async variants from <see cref="MethodIL"/> by reporting
    /// <see cref="MethodILScope.OwningMethod"/> how it's generated in the metadata.
    /// Dataflow infrastructure doesn't expect async variant method signatures.
    /// </summary>
    internal sealed class AsyncMaskingILProvider : ILProvider
    {
        private readonly ILProvider _wrappedProvider;

        public AsyncMaskingILProvider(ILProvider wrappedProvider)
        {
            Debug.Assert(wrappedProvider is not AsyncMaskingILProvider);
            _wrappedProvider = wrappedProvider;
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            Debug.Assert(!method.IsAsyncVariant());

            MethodDesc methodForIL;
            if (method.IsAsync && method.GetTypicalMethodDefinition().Signature.ReturnsTaskOrValueTask())
                methodForIL = ((CompilerTypeSystemContext)method.Context).GetAsyncVariantMethod(method);
            else
                methodForIL = method;

            MethodIL methodIL = _wrappedProvider.GetMethodIL(methodForIL);

            return methodForIL == method ? methodIL : new AsyncMaskedMethodIL(method, methodIL);
        }

        public static MethodIL WrapIL(MethodIL methodIL)
        {
            MethodDesc owningMethod = methodIL.OwningMethod;
            if (owningMethod.IsAsyncVariant())
            {
                Debug.Assert(owningMethod.IsAsync);
                return new AsyncMaskedMethodIL(((CompilerTypeSystemContext)owningMethod.Context).GetTargetOfAsyncVariantMethod(owningMethod), methodIL);
            }

            return methodIL;
        }

        private sealed class AsyncMaskedMethodIL : MethodIL
        {
            private readonly MethodDesc _owner;
            private readonly MethodIL _wrappedIL;

            public AsyncMaskedMethodIL(MethodDesc owner, MethodIL wrappedIL)
                => (_owner, _wrappedIL) = (owner, wrappedIL);

            // Change the owner
            public override MethodDesc OwningMethod => _owner;

            // Everything else dispatches to the wrapper MethodIL
            public override MethodDebugInformation GetDebugInfo() => _wrappedIL.GetDebugInfo();
            public override ILExceptionRegion[] GetExceptionRegions() => _wrappedIL.GetExceptionRegions();
            public override byte[] GetILBytes() => _wrappedIL.GetILBytes();
            public override LocalVariableDefinition[] GetLocals() => _wrappedIL.GetLocals();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw) => _wrappedIL.GetObject(token, notFoundBehavior);
            public override bool IsInitLocals => _wrappedIL.IsInitLocals;
            public override int MaxStack => _wrappedIL.MaxStack;

            public override MethodIL GetMethodILDefinition()
            {
                Debug.Assert(_wrappedIL.GetMethodILDefinition() == _wrappedIL);
                return this;
            }
        }
    }
}
