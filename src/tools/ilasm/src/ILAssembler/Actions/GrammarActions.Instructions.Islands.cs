// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void ProcessInstructionIsland(CILParser.InstructionIslandContext context)
    {
        if (StartInstruction(context.Start) is not { } instruction)
        {
            return;
        }

        ILOpCode opcode = instruction.OpCode;
        CurrentMethodContext method = instruction.Method;

        if (context.methodRef() is CILParser.MethodRefContext methodRef)
        {
            EmitMethodReferenceInstruction(method, opcode, methodRef);
        }
        else if (context.fieldRef() is CILParser.FieldRefContext fieldRef)
        {
            method.Definition.MethodBody.OpCode(opcode);
            WriteInstructionToken(method, VisitFieldRef(fieldRef).Value);
        }
        else if (context.mdtoken() is CILParser.MdtokenContext mdtoken)
        {
            method.Definition.MethodBody.OpCode(opcode);
            WriteInstructionToken(method, VisitMdtoken(mdtoken).Value);
        }
        else if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
        {
            method.Definition.MethodBody.OpCode(opcode);
            WriteInstructionToken(method, VisitTypeSpec(typeSpec).Value);
        }
        else if (context.callConv() is CILParser.CallConvContext callConv)
        {
            EmitCalliInstruction(method, opcode, context, callConv);
        }
        else if (context.ownerType() is CILParser.OwnerTypeContext ownerType)
        {
            method.Definition.MethodBody.OpCode(opcode);
            WriteInstructionToken(method, VisitOwnerType(ownerType).Value);
        }
        else
        {
            throw new UnreachableException();
        }
    }

    private void EmitMethodReferenceInstruction(
        CurrentMethodContext method,
        ILOpCode opcode,
        CILParser.MethodRefContext context)
    {
        bool expectInstance = opcode is ILOpCode.Callvirt or ILOpCode.Newobj;
        _expectInstance = expectInstance;
        try
        {
            method.Definition.MethodBody.OpCode(opcode);
            WriteInstructionToken(method, VisitMethodRef(context).Value);
        }
        finally
        {
            if (expectInstance)
            {
                _expectInstance = false;
            }
        }
    }

    private void EmitCalliInstruction(
        CurrentMethodContext method,
        ILOpCode opcode,
        CILParser.InstructionIslandContext context,
        CILParser.CallConvContext callConv)
    {
        Debug.Assert(opcode == ILOpCode.Calli);
        BlobBuilder signature = new();
        signature.WriteByte(VisitCallConv(callConv).Value);
        ImmutableArray<SignatureArg> arguments = VisitSigArgs(context.sigArgs()).Value;
        signature.WriteCompressedInteger(arguments.Count(argument => !argument.IsSentinel));
        VisitType(context.type()).Value.WriteContentTo(signature);
        foreach (SignatureArg argument in arguments)
        {
            argument.SignatureBlob.WriteContentTo(signature);
        }

        method.Definition.MethodBody.OpCode(opcode);
        method.Definition.MethodBody.Token(_entityRegistry.GetOrCreateStandaloneSignature(signature).Handle);
    }

    private static void WriteInstructionToken(CurrentMethodContext method, EntityRegistry.EntityBase entity)
    {
        if (entity is EntityRegistry.TypeReferenceEntity typeReference)
        {
            typeReference.RecordBlobToWriteResolvedToken(
                method.Definition.MethodBody.CodeBuilder.ReserveBytes(sizeof(int)));
        }
        else if (entity is EntityRegistry.MemberReferenceEntity memberReference)
        {
            memberReference.RecordBlobToWriteResolvedHandle(
                method.Definition.MethodBody.CodeBuilder.ReserveBytes(sizeof(int)));
        }
        else
        {
            method.Definition.MethodBody.Token(entity.Handle);
        }
    }
}
