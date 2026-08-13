// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

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
        else if (context.compQstring() is CILParser.CompQstringContext userString)
        {
            EmitComposedStringInstruction(method, context, userString);
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
        else if (opcode == ILOpCode.Switch)
        {
            EmitSwitchInstruction(method, context, context.labels());
        }
        else
        {
            EmitParsedFloatingInstruction(method, opcode, context);
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

    private void EmitParsedFloatingInstruction(
        CurrentMethodContext method,
        ILOpCode opcode,
        CILParser.InstructionIslandContext context)
    {
        double value = context.float64() is CILParser.Float64Context float64
            ? VisitFloat64(float64).Value
            : VisitInt64(context.int64()).Value;

        if (opcode == ILOpCode.Ldc_r4)
        {
            method.Definition.MethodBody.LoadConstantR4((float)value);
        }
        else
        {
            method.Definition.MethodBody.LoadConstantR8(value);
        }
    }

    private void EmitComposedStringInstruction(
        CurrentMethodContext method,
        CILParser.InstructionIslandContext context,
        CILParser.CompQstringContext userString)
    {
        string value = VisitCompQstring(userString).Value;
        if (context.ANSI() is not null)
        {
            int byteCount = Encoding.UTF8.GetByteCount(value);
            if ((byteCount % 1) != 0)
            {
                byteCount++;
            }

            Span<byte> utf8Bytes = new byte[byteCount];
            Encoding.UTF8.GetBytes(value, utf8Bytes);
            value = new string(MemoryMarshal.Cast<byte, char>(utf8Bytes));
        }

        method.Definition.MethodBody.LoadString(_metadataBuilder.GetOrAddUserString(value));
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

    private void EmitSwitchInstruction(
        CurrentMethodContext method,
        CILParser.InstructionIslandContext context,
        CILParser.LabelsContext? labelsContext)
    {
        List<(LabelHandle Label, int? Offset)> labels = new();
        if (labelsContext?.children is { } labelChildren)
        {
            foreach (IParseTree label in labelChildren)
            {
                if (label is CILParser.IdContext id)
                {
                    string labelName = VisitId(id).Value;
                    if (!method.Labels.TryGetValue(labelName, out LabelHandle handle))
                    {
                        handle = method.Definition.MethodBody.DefineLabel();
                        method.Labels[labelName] = handle;
                        method.UndefinedLabelReferences.TryAdd(labelName, context.Start);
                    }
                    labels.Add((handle, null));
                }
                else if (label is CILParser.Int32Context int32)
                {
                    labels.Add((method.Definition.MethodBody.DefineLabel(), VisitInt32(int32).Value));
                }
            }
        }

        if (labels.Count > 0)
        {
            SwitchInstructionEncoder switchEncoder = method.Definition.MethodBody.Switch(labels.Count);
            foreach ((LabelHandle label, _) in labels)
            {
                switchEncoder.Branch(label);
            }
        }
        else
        {
            method.Definition.MethodBody.OpCode(ILOpCode.Switch);
            method.Definition.MethodBody.CodeBuilder.WriteInt32(0);
        }

        foreach ((LabelHandle label, int? offset) in labels)
        {
            if (offset is int value)
            {
                method.Definition.MethodBody.MarkLabel(
                    label,
                    method.Definition.MethodBody.Offset + value);
            }
        }
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
