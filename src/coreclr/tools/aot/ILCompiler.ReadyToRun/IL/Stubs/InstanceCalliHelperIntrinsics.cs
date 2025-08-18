// Licensed to the .NET Foundation under one or more agreements.
 // The .NET Foundation licenses this file to you under the MIT license.
 
 using System;
 using System.Reflection.Metadata.Ecma335;
 using Internal.TypeSystem;
 using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    public class InstanceCalliHelperIntrinsics
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            MethodIL methodIL = EcmaMethodIL.Create((EcmaMethod)method);

            if (method.Name.StartsWith("Invoke", StringComparison.Ordinal))
            {
                methodIL = new ExplicitThisCall(methodIL);
            }

            return methodIL;
        }

        private class ExplicitThisCall : MethodIL
        {
            private readonly MethodIL _wrappedMethodIL;

            public ExplicitThisCall(MethodIL wrapped)
            {
                _wrappedMethodIL = wrapped;
            }

            // MethodIL overrides:
            public override int MaxStack => _wrappedMethodIL.MaxStack;
            public override bool IsInitLocals => _wrappedMethodIL.IsInitLocals;
            public override byte[] GetILBytes() => _wrappedMethodIL.GetILBytes();
            public override LocalVariableDefinition[] GetLocals() => _wrappedMethodIL.GetLocals();
            public override ILExceptionRegion[] GetExceptionRegions() => _wrappedMethodIL.GetExceptionRegions();
            public override MethodDebugInformation GetDebugInfo() => _wrappedMethodIL.GetDebugInfo();

            // MethodILScope overrides:
            public override MethodIL GetMethodILDefinition() => _wrappedMethodIL.GetMethodILDefinition();
            public override MethodDesc OwningMethod => _wrappedMethodIL.OwningMethod;
            public override string ToString() => _wrappedMethodIL.ToString();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior)
            {
                object item = _wrappedMethodIL.GetObject(token, notFoundBehavior);
                if (item is MethodSignature sig)
                {
                    var builder = new MethodSignatureBuilder(sig);
                    builder.Flags = (sig.Flags | MethodSignatureFlags.ExplicitThis) & ~MethodSignatureFlags.Static;
                    item = builder.ToSignature();
                }

                return item;
            }
        }
    }
}
