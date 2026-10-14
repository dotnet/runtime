// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Antlr4.Runtime;
using LabelHandle = ILAssembler.MethodBodyWriter.Label;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void EmitNoOperandInstruction(IToken opcodeToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
    }

    internal void EmitVariableIndexInstruction(IToken opcodeToken, IToken indexToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        WriteVariableIndex(instruction.Method, instruction.OpCode, ParseInt32(indexToken));
    }

    internal void EmitVariableNameInstruction(IToken opcodeToken, IToken nameToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        CurrentMethodContext method = instruction.Method;
        ILOpCode opcode = instruction.OpCode;
        string instructionName = opcode.ToString();
        string variableName = ParseIdentifier(nameToken);
        int? index = null;

        if (instructionName.Contains("arg", StringComparison.Ordinal))
        {
            if (method.ArgumentNames.TryGetValue(variableName, out int argumentIndex))
            {
                index = method.Definition.SignatureHeader.IsInstance ? argumentIndex + 1 : argumentIndex;
            }
            else
            {
                ReportError(
                    DiagnosticIds.ArgumentNotFound,
                    string.Format(DiagnosticMessageTemplates.ArgumentNotFound, variableName),
                    opcodeToken);
            }
        }
        else
        {
            for (int i = method.LocalsScopes.Count - 1; i >= 0; i--)
            {
                if (method.LocalsScopes[i].TryGetValue(variableName, out int localIndex))
                {
                    index = localIndex;
                    break;
                }
            }

            if (index is null)
            {
                ReportError(
                    DiagnosticIds.LocalNotFound,
                    string.Format(DiagnosticMessageTemplates.LocalNotFound, variableName),
                    opcodeToken);
            }
        }

        WriteVariableIndex(method, opcode, index ?? -1);
    }

    internal void EmitInt32Instruction(IToken opcodeToken, IToken valueToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        int value = ParseInt32(valueToken);
        if (instruction.OpCode is ILOpCode.Ldc_i4 or ILOpCode.Ldc_i4_s)
        {
            instruction.Method.Definition.MethodBody.LoadConstantI4(instruction.OpCode, value, _options.Optimize);
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        instruction.Method.Definition.MethodBody.CodeBuilder.WriteByte((byte)value);
    }

    internal void EmitInt64Instruction(IToken opcodeToken, IToken valueToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        Debug.Assert(instruction.OpCode == ILOpCode.Ldc_i8);
        instruction.Method.Definition.MethodBody.LoadConstantI8(ParseInt64(valueToken));
    }

    internal void EmitFloatingInstruction(IToken opcodeToken, double value)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        WriteFloatingInstruction(instruction.Method, instruction.OpCode, value);
    }

    internal void EmitFloatingInstruction(IToken opcodeToken, IToken valueToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        WriteFloatingInstruction(instruction.Method, instruction.OpCode, ParseInt64(valueToken));
    }

    internal void EmitBranchOffsetInstruction(IToken opcodeToken, IToken offsetToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        int offset = ParseInt32(offsetToken);
        instruction.Method.Definition.MethodBody.Branch(instruction.OpCode, offset,
            _options.Optimize, Location.From(opcodeToken, _documents));
    }

    internal void EmitBranchLabelInstruction(IToken opcodeToken, IToken labelToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        CurrentMethodContext method = instruction.Method;
        string labelName = ParseIdentifier(labelToken);
        LabelHandle label = GetOrCreateMethodLabel(method, labelName, opcodeToken);

        method.Definition.MethodBody.Branch(instruction.OpCode, label,
            _options.Optimize, Location.From(opcodeToken, _documents));
    }

    internal void EmitRawFloatingInstruction(
        IToken opcodeToken,
        ImmutableArray<byte> bytes,
        IToken bytesToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        double value;
        ReadOnlySpan<byte> byteSpan = bytes.AsSpan();
        if (byteSpan.Length >= sizeof(double))
        {
            value = BitConverter.ToDouble(byteSpan);
        }
        else if (byteSpan.Length >= sizeof(float))
        {
            value = BitConverter.ToSingle(byteSpan);
        }
        else
        {
            ReportError(
                DiagnosticIds.ByteArrayTooShort,
                DiagnosticMessageTemplates.ByteArrayTooShort,
                bytesToken);
            value = 0;
        }

        WriteFloatingInstruction(instruction.Method, instruction.OpCode, value);
    }

    internal void EmitRawStringInstruction(IToken opcodeToken, ImmutableArray<byte> bytes)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        string value = MemoryMarshal.Cast<byte, char>(bytes.AsSpan()).ToString();
        instruction.Method.Definition.MethodBody.LoadString(_metadataBuilder.GetOrAddUserString(value));
    }

    internal void EmitStringInstruction(IToken opcodeToken, string value)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        instruction.Method.Definition.MethodBody.LoadString(_metadataBuilder.GetOrAddUserString(value));
    }

    internal void EmitAnsiStringInstruction(IToken opcodeToken, string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if ((byteCount % 2) != 0)
        {
            byteCount++;
        }

        Span<byte> utf8Bytes = new byte[byteCount];
        Encoding.UTF8.GetBytes(value, utf8Bytes);
        EmitStringInstruction(opcodeToken, new string(MemoryMarshal.Cast<byte, char>(utf8Bytes)));
    }

    internal void EmitRawTokenInstruction(IToken opcodeToken, IToken valueToken)
    {
        if (StartInstruction(opcodeToken) is not { } instruction)
        {
            return;
        }

        instruction.Method.Definition.MethodBody.OpCode(instruction.OpCode);
        instruction.Method.Definition.MethodBody.CodeBuilder.WriteInt32(ParseInt32(valueToken));
    }

    internal void EmitByte(IToken valueToken)
    {
        _currentMethod?.Definition.MethodBody.CodeBuilder.WriteByte((byte)ParseInt32(valueToken));
    }

    internal void SetMaxStack(IToken valueToken)
    {
        if (_currentMethod is not null)
        {
            _currentMethod.Definition.MaxStack = ParseInt32(valueToken);
        }
    }

    internal void SetEntryPoint()
    {
        if (_currentMethod is not null)
        {
            _entityRegistry.EntryPoint = _currentMethod.Definition;
        }
    }

    internal void SetZeroInit()
    {
        if (_currentMethod is not null)
        {
            _currentMethod.Definition.BodyAttributes = MethodBodyAttributes.InitLocals;
        }
    }

    internal void DefineLabel(IToken nameToken)
    {
        if (_currentMethod is not { } method)
        {
            return;
        }

        string labelName = ParseIdentifier(nameToken);
        method.UndefinedLabelReferences.Remove(labelName);
        if (!method.Labels.TryGetValue(labelName, out LabelHandle label))
        {
            label = method.Definition.MethodBody.DefineLabel();
            method.Labels[labelName] = label;
        }

        method.Definition.MethodBody.MarkLabel(label);
    }

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal CILParser.SwitchInstructionBuilder CreateSwitchInstruction(IToken opcodeToken)
        => new(opcodeToken);

    internal void AddSwitchLabel(CILParser.SwitchInstructionBuilder builder, IToken labelToken)
        => builder.Operands.Add((labelToken, false));

    internal void AddSwitchOffset(CILParser.SwitchInstructionBuilder builder, IToken offsetToken)
        => builder.Operands.Add((offsetToken, true));
#pragma warning restore CA1822

    internal void CompleteSwitchInstruction(CILParser.SwitchInstructionBuilder? builder)
    {
        if (builder is null ||
            StartInstruction(builder.OpcodeToken) is not { } instruction)
        {
            return;
        }

        Debug.Assert(instruction.OpCode == ILOpCode.Switch);
        CurrentMethodContext method = instruction.Method;
        List<(LabelHandle Label, int? Offset)> labels = new(builder.Operands.Count);
        foreach ((IToken token, bool isOffset) in builder.Operands)
        {
            if (isOffset)
            {
                labels.Add((default, ParseInt32(token)));
                continue;
            }

            string labelName = ParseIdentifier(token);
            LabelHandle label = GetOrCreateMethodLabel(
                method,
                labelName,
                builder.OpcodeToken);

            labels.Add((label, null));
        }

        method.Definition.MethodBody.Switch(labels, Location.From(builder.OpcodeToken, _documents));
    }

    private (CurrentMethodContext Method, ILOpCode OpCode)? StartInstruction(IToken opcodeToken)
    {
        if (_currentMethod is not { } method)
        {
            return null;
        }

        ILOpCode opcode = ParseOpCodeFromToken(opcodeToken);
        return (method, opcode);
    }

    private void WriteVariableIndex(CurrentMethodContext method, ILOpCode opcode, int index)
        => method.Definition.MethodBody.Variable(opcode, index, _options.Optimize);

    private static void WriteFloatingInstruction(CurrentMethodContext method, ILOpCode opcode, double value)
    {
        if (opcode == ILOpCode.Ldc_r4)
        {
            method.Definition.MethodBody.LoadConstantR4((float)value);
        }
        else
        {
            method.Definition.MethodBody.LoadConstantR8(value);
        }
    }

    private static ILOpCode ParseOpCodeFromToken(IToken token)
    {
        string text = token.Text.TrimEnd('.');
        if (text == "unused")
        {
            return ILOpCode.Unused;
        }

        string normalized = text.Replace('.', '_');
        normalized = normalized switch
        {
            "ldelem_u8" => "ldelem_i8",
            "ldind_u8" => "ldind_i8",
            "endfault" => "endfinally",
            _ => normalized
        };

        return (ILOpCode)Enum.Parse(typeof(ILOpCode), normalized, ignoreCase: true);
    }

    internal EntityRegistry.EntityBase ResolveMetadataToken(CILParser.MdtokenContext context)
        => ResolveMetadataToken(context.Value);
}
