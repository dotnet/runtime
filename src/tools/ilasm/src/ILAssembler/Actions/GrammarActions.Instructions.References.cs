// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void EmitMethodReferenceInstruction(IToken opcodeToken, CILParser.MethodRefContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || ContainsSyntaxError(context))
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

    internal void EmitFieldReferenceInstruction(IToken opcodeToken, CILParser.FieldRefContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || ContainsSyntaxError(context))
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, VisitFieldRef(context).Value);
    }

    internal void EmitMetadataTokenInstruction(IToken opcodeToken, CILParser.MdtokenContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || ContainsSyntaxError(context))
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, VisitMdtoken(context).Value);
    }

    internal void EmitTypeReferenceInstruction(IToken opcodeToken, CILParser.TypeSpecContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || ContainsSyntaxError(context))
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, VisitTypeSpec(context).Value);
    }

    internal void EmitCalliInstruction(IToken opcodeToken, CILParser.CalliSignatureContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || ContainsSyntaxError(context))
        {
            return;
        }

        Debug.Assert(instruction.OpCode == ILOpCode.Calli);
        BlobBuilder signature = new();
        signature.WriteByte(VisitCallConv(context.callConv()).Value);
        ImmutableArray<SignatureArg> arguments = VisitSigArgs(context.sigArgs()).Value;
        signature.WriteCompressedInteger(arguments.Count(argument => !argument.IsSentinel));
        VisitType(context.type()).Value.WriteContentTo(signature);
        foreach (SignatureArg argument in arguments)
        {
            argument.SignatureBlob.WriteContentTo(signature);
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        instruction.Method.Definition.MethodBody.Token(_entityRegistry.GetOrCreateStandaloneSignature(signature).Handle);
    }

    internal void EmitOwnerTokenInstruction(IToken opcodeToken, CILParser.OwnerTypeContext context)
    {
        if (StartInstruction(opcodeToken) is not { } instruction || ContainsSyntaxError(context))
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        WriteInstructionToken(instruction.Method, VisitOwnerType(context).Value);
    }

    private static bool ContainsSyntaxError(IParseTree tree)
    {
        if (tree is IErrorNode or ParserRuleContext { exception: not null })
        {
            return true;
        }

        for (int i = 0; i < tree.ChildCount; i++)
        {
            if (ContainsSyntaxError(tree.GetChild(i)))
            {
                return true;
            }
        }

        return false;
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

    GrammarResult ICILVisitor<GrammarResult>.VisitCalliSignature(CILParser.CalliSignatureContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);
}
