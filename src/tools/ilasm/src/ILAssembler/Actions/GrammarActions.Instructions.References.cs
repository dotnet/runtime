// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void EmitMethodReferenceInstruction(IToken opcodeToken, CILParser.MethodRefContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || context.HasSyntaxError)
        {
            return;
        }

        ILOpCode opcode = instruction.OpCode;
        CurrentMethodContext method = instruction.Method;
        bool expectInstance = opcode is ILOpCode.Callvirt or ILOpCode.Newobj;
        _expectInstance = expectInstance;
        try
        {
            method.Definition.MethodBody.OpCode(opcode);
            WriteInstructionToken(method, MaterializeMethodReference(context));
        }
        finally
        {
            if (expectInstance)
            {
                _expectInstance = false;
            }
        }
    }

    internal void EmitFieldReferenceInstruction(IToken opcodeToken, CILParser.FieldRefContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || context.HasSyntaxError)
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, MaterializeFieldReference(context));
    }

    internal void EmitMetadataTokenInstruction(IToken opcodeToken, CILParser.MdtokenContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || context.HasSyntaxError)
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, ResolveMetadataToken(context));
    }

    internal void EmitTypeReferenceInstruction(IToken opcodeToken, CILParser.TypeSpecContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || context.HasSyntaxError)
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, ResolveTypeSpecification(context));
    }

    internal void EmitCalliInstruction(IToken opcodeToken, CILParser.CalliSignatureContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || context.HasSyntaxError)
        {
            return;
        }

        Debug.Assert(instruction.OpCode == ILOpCode.Calli);
        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        instruction.Method.Definition.MethodBody.Token(
            _entityRegistry.GetOrCreateStandaloneSignature(
                MaterializeCalliSignature(context.Value)).Handle);
    }

    internal void EmitOwnerTokenInstruction(IToken opcodeToken, CILParser.OwnerTypeContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || context.HasSyntaxError)
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, MaterializeOwnerType(context));
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
