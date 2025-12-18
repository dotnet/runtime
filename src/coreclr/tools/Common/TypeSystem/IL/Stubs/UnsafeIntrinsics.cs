// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for generic System.Runtime.CompilerServices.Unsafe intrinsics.
    /// </summary>
    public static class UnsafeIntrinsics
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            Debug.Assert(((MetadataType)method.OwningType).Name.SequenceEqual("Unsafe"u8));

            bool Is(ReadOnlySpan<byte> name) => method.Name.SequenceEqual(name);

            if (Is("AsPointer"u8))
                return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ldarg_0, (byte)ILOpcode.conv_u, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("As"u8) || Is("AsRef"u8))
                return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("Add"u8))
                return EmitAdd(method);

            if (Is("AddByteOffset"u8))
                return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1, (byte)ILOpcode.add, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("Copy"u8))
                return EmitCopy(method);

            if (Is("CopyBlock"u8))
                return EmitCopyBlock(method, unaligned: false);

            if (Is("CopyBlockUnaligned"u8))
                return EmitCopyBlock(method, unaligned: true);

            if (Is("InitBlock"u8))
                return EmitInitBlock(method, unaligned: false);

            if (Is("InitBlockUnaligned"u8))
                return EmitInitBlock(method, unaligned: true);

            if (Is("Read"u8))
                return EmitReadWrite(method, write: false);

            if (Is("Write"u8))
                return EmitReadWrite(method, write: true);

            if (Is("ReadUnaligned"u8))
                return EmitReadWrite(method, write: false, unaligned: true);

            if (Is("WriteUnaligned"u8))
                return EmitReadWrite(method, write: true, unaligned: true);

            if (Is("AreSame"u8))
                return new ILStubMethodIL(method, new byte[]
                {
                    (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1,
                    (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.ceq),
                    (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("IsAddressGreaterThan"u8))
                return new ILStubMethodIL(method, new byte[]
                {
                    (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1,
                    (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.cgt_un),
                    (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("IsAddressLessThan"u8))
                return new ILStubMethodIL(method, new byte[]
                {
                    (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1,
                    (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.clt_un),
                    (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("ByteOffset"u8))
                return new ILStubMethodIL(method, new byte[]
                {
                    (byte)ILOpcode.ldarg_1, (byte)ILOpcode.ldarg_0,
                    (byte)ILOpcode.sub,
                    (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("NullRef"u8))
                return new ILStubMethodIL(method, new byte[]
                {
                    (byte)ILOpcode.ldc_i4_0, (byte)ILOpcode.conv_u,
                    (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("IsNullRef"u8))
                return new ILStubMethodIL(method, new byte[]
                {
                    (byte)ILOpcode.ldarg_0,
                    (byte)ILOpcode.ldc_i4_0, (byte)ILOpcode.conv_u,
                    (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.ceq),
                    (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("SkipInit"u8))
                return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("Subtract"u8))
                return EmitSubtract(method);

            if (Is("SubtractByteOffset"u8))
                return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1, (byte)ILOpcode.sub, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);

            if (Is("Unbox"u8))
                return EmitUnbox(method);

            return null;
        }

        private static MethodIL EmitAdd(MethodDesc method)
        {
            Debug.Assert(method.Signature.IsStatic && method.Signature.Length == 2);

            TypeSystemContext context = method.Context;

            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();
            codeStream.Emit(ILOpcode.ldarg_1);
            codeStream.Emit(ILOpcode.sizeof_, emit.NewToken(context.GetSignatureVariable(0, method: true)));
            codeStream.Emit(ILOpcode.conv_i);
            codeStream.Emit(ILOpcode.mul);
            codeStream.Emit(ILOpcode.ldarg_0);
            codeStream.Emit(ILOpcode.add);
            codeStream.Emit(ILOpcode.ret);
            return emit.Link(method);
        }

        private static MethodIL EmitSubtract(MethodDesc method)
        {
            Debug.Assert(method.Signature.IsStatic && method.Signature.Length == 2);

            TypeSystemContext context = method.Context;

            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.Emit(ILOpcode.ldarg_0);
            codeStream.Emit(ILOpcode.ldarg_1);
            codeStream.Emit(ILOpcode.sizeof_, emit.NewToken(context.GetSignatureVariable(0, method: true)));
            codeStream.Emit(ILOpcode.conv_i);
            codeStream.Emit(ILOpcode.mul);
            codeStream.Emit(ILOpcode.sub);
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(method);
        }

        private static MethodIL EmitReadWrite(MethodDesc method, bool write, bool unaligned = false)
        {
            Debug.Assert(method.Signature.IsStatic && method.Signature.Length == (write ? 2 : 1));

            TypeSystemContext context = method.Context;

            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.EmitLdArg(0);
            if (write) codeStream.EmitLdArg(1);
            if (unaligned) codeStream.EmitUnaligned();
            codeStream.Emit(write ? ILOpcode.stobj : ILOpcode.ldobj,
                emit.NewToken(context.GetSignatureVariable(0, method: true)));
            codeStream.Emit(ILOpcode.ret);
            return emit.Link(method);
        }

        private static MethodIL EmitCopy(MethodDesc method)
        {
            Debug.Assert(method.Signature.IsStatic && method.Signature.Length == 2);

            TypeSystemContext context = method.Context;

            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();
            ILToken token = emit.NewToken(context.GetSignatureVariable(0, method: true));

            codeStream.EmitLdArg(0);
            codeStream.EmitLdArg(1);
            codeStream.Emit(ILOpcode.ldobj, token);
            codeStream.Emit(ILOpcode.stobj, token);
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(method);
        }

        private static MethodIL EmitCopyBlock(MethodDesc method, bool unaligned)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.EmitLdArg(0);
            codeStream.EmitLdArg(1);
            codeStream.EmitLdArg(2);
            if (unaligned)
            {
                codeStream.EmitUnaligned();
            }
            codeStream.Emit(ILOpcode.cpblk);
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(method);
        }

        private static MethodIL EmitInitBlock(MethodDesc method, bool unaligned)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.EmitLdArg(0);
            codeStream.EmitLdArg(1);
            codeStream.EmitLdArg(2);
            if (unaligned)
            {
                codeStream.EmitUnaligned();
            }
            codeStream.Emit(ILOpcode.initblk);
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(method);
        }

        private static MethodIL EmitUnbox(MethodDesc method)
        {
            Debug.Assert(method.Signature.IsStatic && method.Signature.Length == 1);

            TypeSystemContext context = method.Context;

            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.unbox, emit.NewToken(context.GetSignatureVariable(0, method: true)));
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(method);
        }
    }
}
