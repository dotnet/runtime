// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for System.IO.Stream intrinsics.
    /// </summary>
    public static class StreamIntrinsics
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            Debug.Assert(((MetadataType)method.OwningType).Name == "Stream");

            bool isRead = method.Name == "HasOverriddenBeginEndRead";
            if (!isRead && method.Name != "HasOverriddenBeginEndWrite")
                return null;

            TypeDesc streamClass = method.OwningType;
            MethodDesc beginMethod = streamClass.GetMethod(isRead ? "BeginRead" : "BeginWrite", null);
            MethodDesc endMethod = streamClass.GetMethod(isRead ? "EndRead" : "EndWrite", null);

            ILEmitter emitter = new ILEmitter();
            ILCodeStream codestream = emitter.NewCodeStream();

            ILCodeLabel lOverriden = emitter.NewCodeLabel();

            ILToken beginMethodToken = emitter.NewToken(beginMethod);
            codestream.EmitLdArg(0);
            codestream.Emit(ILOpcode.ldvirtftn, beginMethodToken);
            codestream.Emit(ILOpcode.ldftn, beginMethodToken);
            codestream.Emit(ILOpcode.bne_un, lOverriden);

            ILToken endMethodToken = emitter.NewToken(endMethod);
            codestream.EmitLdArg(0);
            codestream.Emit(ILOpcode.ldvirtftn, endMethodToken);
            codestream.Emit(ILOpcode.ldftn, endMethodToken);
            codestream.Emit(ILOpcode.bne_un, lOverriden);

            codestream.EmitLdc(0);
            codestream.Emit(ILOpcode.ret);

            codestream.EmitLabel(lOverriden);
            codestream.EmitLdc(1);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(method);
        }
    }
}
