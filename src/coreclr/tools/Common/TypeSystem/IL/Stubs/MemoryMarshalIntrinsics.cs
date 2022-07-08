// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for System.Runtime.InteropServices.MemoryMarshal intrinsics.
    /// </summary>
    public static class MemoryMarshalIntrinsics
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            Debug.Assert(((MetadataType)method.OwningType).Name == "MemoryMarshal");
            string methodName = method.Name;

            if (method.Instantiation.Length != 1)
            {
                return null; // we only handle the generic method GetArrayDataReference<T>(T[])
            }

            if (methodName == "GetArrayDataReference")
            {
                var rawArrayData = method.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices", "RawArrayData");
#if READYTORUN
                if (!rawArrayData.IsNonVersionable())
                    return null; // This is only an intrinsic if we can prove that RawArrayData is known to be of fixed offset
#endif
                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();
                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldflda, emit.NewToken(rawArrayData.GetField("Data")));
                codeStream.Emit(ILOpcode.ret);
                return emit.Link(method);
            }

            // unknown method
            return null;
        }
    }
}
