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
            Debug.Assert(((MetadataType)method.OwningType).Name == "Unsafe");

            switch (method.Name)
            {
                case "AsPointer":
                    return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ldarg_0, (byte)ILOpcode.conv_u, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "SizeOf":
                    return EmitSizeOf(method);
                case "As":
                case "AsRef":
                    return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "Add":
                    return EmitAdd(method);
                case "AddByteOffset":
                    return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1, (byte)ILOpcode.add, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "InitBlockUnaligned":
                    return new ILStubMethodIL(method, new byte[] {
                        (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1, (byte)ILOpcode.ldarg_2,
                        (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.unaligned), 0x01,
                        (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.initblk),
                        (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "Read":
                    return EmitReadWrite(method, write: false);
                case "Write":
                    return EmitReadWrite(method, write: true);
                case "ReadUnaligned":
                    return EmitReadWrite(method, write: false, unaligned: true);
                case "WriteUnaligned":
                    return EmitReadWrite(method, write: true, unaligned: true);
                case "AreSame":
                    return new ILStubMethodIL(method, new byte[]
                    {
                        (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1,
                        (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.ceq),
                        (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "IsAddressGreaterThan":
                    return new ILStubMethodIL(method, new byte[]
                    {
                        (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1,
                        (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.cgt_un),
                        (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "IsAddressLessThan":
                    return new ILStubMethodIL(method, new byte[]
                    {
                        (byte)ILOpcode.ldarg_0, (byte)ILOpcode.ldarg_1,
                        (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.clt_un),
                        (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "ByteOffset":
                    return new ILStubMethodIL(method, new byte[]
                    {
                        (byte)ILOpcode.ldarg_1, (byte)ILOpcode.ldarg_0,
                        (byte)ILOpcode.sub,
                        (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "NullRef":
                    return new ILStubMethodIL(method, new byte[]
                    {
                        (byte)ILOpcode.ldc_i4_0, (byte)ILOpcode.conv_u,
                        (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "IsNullRef":
                    return new ILStubMethodIL(method, new byte[]
                    {
                        (byte)ILOpcode.ldarg_0, 
                        (byte)ILOpcode.ldc_i4_0, (byte)ILOpcode.conv_u,
                        (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.ceq),
                        (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                case "SkipInit":
                    return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
            }

            return null;
        }

        private static MethodIL EmitSizeOf(MethodDesc method)
        {
            Debug.Assert(method.Signature.IsStatic && method.Signature.Length == 0);

            TypeSystemContext context = method.Context;

            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();
            codeStream.Emit(ILOpcode.sizeof_, emit.NewToken(context.GetSignatureVariable(0, method: true)));
            codeStream.Emit(ILOpcode.ret);
            return emit.Link(method);
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
    }
}
