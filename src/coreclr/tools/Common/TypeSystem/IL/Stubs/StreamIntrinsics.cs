// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
            Debug.Assert(((MetadataType)method.OwningType).Name.SequenceEqual("Stream"u8));

            bool isRead = method.Name.SequenceEqual("HasOverriddenBeginEndRead"u8);
            if (!isRead && !method.Name.SequenceEqual("HasOverriddenBeginEndWrite"u8))
                return null;

            TypeDesc streamClass = method.OwningType;
            MethodDesc beginMethod = streamClass.GetMethod(isRead ? "BeginRead"u8 : "BeginWrite"u8, null);
            MethodDesc endMethod = streamClass.GetMethod(isRead ? "EndRead"u8 : "EndWrite"u8, null);

            ILEmitter emitter = new ILEmitter();
            ILCodeStream codestream = emitter.NewCodeStream();

            ILCodeLabel lOverridden = emitter.NewCodeLabel();

            ILToken beginMethodToken = emitter.NewToken(beginMethod);
            codestream.EmitLdArg(0);
            codestream.Emit(ILOpcode.ldvirtftn, beginMethodToken);
            codestream.Emit(ILOpcode.ldftn, beginMethodToken);
            codestream.Emit(ILOpcode.bne_un, lOverridden);

            ILToken endMethodToken = emitter.NewToken(endMethod);
            codestream.EmitLdArg(0);
            codestream.Emit(ILOpcode.ldvirtftn, endMethodToken);
            codestream.Emit(ILOpcode.ldftn, endMethodToken);
            codestream.Emit(ILOpcode.bne_un, lOverridden);

            codestream.EmitLdc(0);
            codestream.Emit(ILOpcode.ret);

            codestream.EmitLabel(lOverridden);
            codestream.EmitLdc(1);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(method);
        }
    }
}
