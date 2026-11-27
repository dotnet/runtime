// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    /// <summary>
    /// IL of an async variant method: the IL of the method it wraps, owned by the variant.
    /// </summary>
    public class AsyncMethodIL : MethodIL
    {
        private readonly AsyncMethodVariant _variant;
        private readonly MethodIL _wrappedIL;

        private AsyncMethodIL(AsyncMethodVariant variant, MethodIL wrappedIL)
            => (_variant, _wrappedIL) = (variant, wrappedIL);

        /// <summary>
        /// Wraps <paramref name="wrappedIL"/> as the IL of <paramref name="variant"/>. Ecma IL stays Ecma IL, with
        /// tokens relative to the wrapped IL's module, which is not necessarily the variant's owning module.
        /// </summary>
        public static AsyncMethodIL Create(AsyncMethodVariant variant, MethodIL wrappedIL)
            => wrappedIL is IEcmaMethodIL ecmaIL
                ? new EcmaAsyncMethodIL(variant, wrappedIL, ecmaIL.Module)
                : new AsyncMethodIL(variant, wrappedIL);

        // This is the reason we need this class - the method that owns the IL is the variant.
        public override MethodDesc OwningMethod => _variant;

        // Everything else dispatches to the wrapped IL
        public override MethodDebugInformation GetDebugInfo() => _wrappedIL.GetDebugInfo();
        public override ILExceptionRegion[] GetExceptionRegions() => _wrappedIL.GetExceptionRegions();
        public override byte[] GetILBytes() => _wrappedIL.GetILBytes();
        public override LocalVariableDefinition[] GetLocals() => _wrappedIL.GetLocals();
        public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw) => _wrappedIL.GetObject(token, notFoundBehavior);
        public override bool IsInitLocals => _wrappedIL.IsInitLocals;
        public override int MaxStack => _wrappedIL.MaxStack;

        private sealed class EcmaAsyncMethodIL : AsyncMethodIL, IEcmaMethodIL
        {
            public EcmaAsyncMethodIL(AsyncMethodVariant variant, MethodIL wrappedIL, IEcmaModule module)
                : base(variant, wrappedIL)
            {
                Module = module;
            }

            public IEcmaModule Module { get; }
        }
    }
}
