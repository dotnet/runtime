// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private bool TryProcessInstruction(CILParser.MethodDeclContext context)
    {
        if (context.instr() is null)
        {
            return false;
        }

        VisitMethodDecl(context);
        return true;
    }

#pragma warning disable CA1822 // Mark members as static
        public GrammarResult VisitInstr(CILParser.InstrContext context)
        {
            var instrContext = context.GetRuleContext<ParserRuleContext>(0);
            ILOpCode opcode = ((GrammarResult.Literal<ILOpCode>)instrContext.Accept(this)).Value;
            if (opcode == ILOpCode.Localloc)
            {
                _currentMethod!.Definition.HasDynamicStackAllocation = true;
            }
            switch (instrContext.RuleIndex)
            {
                case CILParser.RULE_instr_brtarget:
                    {
                        ParserRuleContext argument = context.GetRuleContext<ParserRuleContext>(1);
                        if (argument is CILParser.IdContext id)
                        {
                            string label = VisitId(id).Value;
                            if (!_currentMethod!.Labels.TryGetValue(label, out var handle))
                            {
                                handle = _currentMethod.Definition.MethodBody.DefineLabel();
                                _currentMethod.Labels[label] = handle;
                                // Track undefined label references for later validation
                                if (!_currentMethod.UndefinedLabelReferences.ContainsKey(label))
                                {
                                    _currentMethod.UndefinedLabelReferences[label] = context;
                                }
                            }
                            _currentMethod.Definition.MethodBody.Branch(opcode, handle);
                        }
                        if (argument is CILParser.Int32Context int32)
                        {
                            int offset = VisitInt32(int32).Value;
                            LabelHandle label = _currentMethod!.Definition.MethodBody.DefineLabel();
                            _currentMethod.Definition.MethodBody.Branch(opcode, label);
                            _currentMethod.Definition.MethodBody.MarkLabel(label, _currentMethod.Definition.MethodBody.Offset + offset);
                        }
                    }
                    break;
                case CILParser.RULE_instr_field:
                    {
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        if (context.mdtoken() is CILParser.MdtokenContext mdtoken)
                        {
                            var entity = VisitMdtoken(mdtoken).Value;
                            if (entity is EntityRegistry.TypeReferenceEntity mdTokenTypeRef)
                            {
                                mdTokenTypeRef.RecordBlobToWriteResolvedToken(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                            }
                            else
                            {
                                _currentMethod.Definition.MethodBody.Token(entity.Handle);
                            }
                        }
                        else
                        {
                            var fieldRef = VisitFieldRef(context.fieldRef()).Value;
                            if (fieldRef is EntityRegistry.MemberReferenceEntity memberRef)
                            {
                                memberRef.RecordBlobToWriteResolvedHandle(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                            }
                            else
                            {
                                _currentMethod.Definition.MethodBody.Token(fieldRef.Handle);
                            }
                        }
                    }
                    break;
                case CILParser.RULE_instr_i:
                    {
                        int arg = VisitInt32(context.int32()).Value;
                        if (opcode == ILOpCode.Ldc_i4 || opcode == ILOpCode.Ldc_i4_s)
                        {
                            _currentMethod!.Definition.MethodBody.LoadConstantI4(arg);
                        }
                        else
                        {
                            _currentMethod!.Definition.MethodBody.OpCode(opcode);
                            _currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)arg);
                        }
                    }
                    break;
                case CILParser.RULE_instr_i8:
                    Debug.Assert(opcode == ILOpCode.Ldc_i8);
                    _currentMethod!.Definition.MethodBody.LoadConstantI8(VisitInt64(context.int64()).Value);
                    break;
                case CILParser.RULE_instr_method:
                    {
                        if (opcode == ILOpCode.Callvirt || opcode == ILOpCode.Newobj)
                        {
                            _expectInstance = true;
                        }
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        var methodRef = VisitMethodRef(context.methodRef()).Value;
                        if (methodRef is EntityRegistry.MemberReferenceEntity memberRef)
                        {
                            memberRef.RecordBlobToWriteResolvedHandle(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                        }
                        else
                        {
                            _currentMethod.Definition.MethodBody.Token(methodRef.Handle);
                        }
                        // Reset the instance flag for the next instruction.
                        if (opcode == ILOpCode.Callvirt || opcode == ILOpCode.Newobj)
                        {
                            _expectInstance = false;
                        }
                    }
                    break;
                case CILParser.RULE_instr_none:
                    _currentMethod!.Definition.MethodBody.OpCode(opcode);
                    break;
                case CILParser.RULE_instr_r:
                    {
                        double value;
                        ParserRuleContext argument = context.GetRuleContext<ParserRuleContext>(1);
                        if (argument is CILParser.Float64Context float64)
                        {
                            value = VisitFloat64(float64).Value;
                        }
                        else if (argument is CILParser.Int64Context int64)
                        {
                            long intValue = VisitInt64(int64).Value;
                            value = intValue;
                        }
                        else if (argument is CILParser.BytesContext bytesContext)
                        {
                            var bytes = VisitBytes(bytesContext).ToArray();
                            if (bytes.Length >= 8)
                            {
                                value = BitConverter.ToDouble(bytes, 0);
                            }
                            else if (bytes.Length >= 4)
                            {
                                value = BitConverter.ToSingle(bytes, 0);
                            }
                            else
                            {
                                ReportError(DiagnosticIds.ByteArrayTooShort, DiagnosticMessageTemplates.ByteArrayTooShort, bytesContext);
                                value = 0.0d;
                            }
                        }
                        else
                        {
                            throw new UnreachableException();
                        }
                        if (opcode == ILOpCode.Ldc_r4)
                        {
                            _currentMethod!.Definition.MethodBody.LoadConstantR4((float)value);
                        }
                        else
                        {
                            _currentMethod!.Definition.MethodBody.LoadConstantR8(value);
                        }
                    }
                    break;
                case CILParser.RULE_instr_sig:
                    {
                        Debug.Assert(opcode == ILOpCode.Calli);
                        BlobBuilder signature = new();
                        byte callConv = VisitCallConv(context.callConv()).Value;
                        signature.WriteByte(callConv);
                        var args = VisitSigArgs(context.sigArgs()).Value;
                        signature.WriteCompressedInteger(args.Count(arg => !arg.IsSentinel));
                        // Write return type
                        VisitType(context.type()).Value.WriteContentTo(signature);
                        // Write arg signatures
                        foreach (var arg in args)
                        {
                            arg.SignatureBlob.WriteContentTo(signature);
                        }
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        _currentMethod!.Definition.MethodBody.Token(_entityRegistry.GetOrCreateStandaloneSignature(signature).Handle);
                    }
                    break;
                case CILParser.RULE_instr_string:
                    Debug.Assert(opcode == ILOpCode.Ldstr);
                    string str;
                    if (context.bytes() is CILParser.BytesContext rawBytes)
                    {
                        ReadOnlySpan<byte> bytes = VisitBytes(rawBytes).AsSpan();
                        ReadOnlySpan<char> bytesAsChars = MemoryMarshal.Cast<byte, char>(bytes);
                        if (!BitConverter.IsLittleEndian)
                        {
                            for (int i = 0; i < bytesAsChars.Length; i++)
                            {
                                BinaryPrimitives.ReverseEndianness(bytesAsChars[i]);
                            }
                        }
                        str = bytesAsChars.ToString();
                    }
                    else
                    {
                        var userString = context.compQstring();
                        Debug.Assert(userString is not null);
                        str = VisitCompQstring(userString!).Value;
                        if (context.ANSI() is not null)
                        {
                            // Emit the string not as a UTF-16 string (as per the spec), but directly as an ANSI string.
                            // Although the string is marked as ANSI, this always used the UTF-8 code page
                            // so we can emit this as UTF-8 bytes.
                            int byteCount = Encoding.UTF8.GetByteCount(str);
                            // Ensure we have an even number of bytes.
                            if ((byteCount % 1) != 0)
                            {
                                byteCount++;
                            }

                            Span<byte> utf8Bytes = new byte[byteCount];
                            Encoding.UTF8.GetBytes(str, utf8Bytes);

                            str = new string(MemoryMarshal.Cast<byte, char>(utf8Bytes));
                        }
                    }
                    _currentMethod!.Definition.MethodBody.LoadString(_metadataBuilder.GetOrAddUserString(str));
                    break;
                case CILParser.RULE_instr_switch:
                    {
                        var labels = new List<(LabelHandle Label, int? Offset)>();
                        if (context.labels()?.children is { } labelChildren)
                        {
                            foreach (var label in labelChildren)
                            {
                            if (label is CILParser.IdContext id)
                            {
                                string labelName = VisitId(id).Value;
                                if (!_currentMethod!.Labels.TryGetValue(labelName, out var handle))
                                {
                                    handle = _currentMethod.Definition.MethodBody.DefineLabel();
                                    _currentMethod.Labels[labelName] = handle;
                                    // Track undefined label references for later validation
                                    if (!_currentMethod.UndefinedLabelReferences.ContainsKey(labelName))
                                    {
                                        _currentMethod.UndefinedLabelReferences[labelName] = context;
                                    }
                                }
                                labels.Add((handle, null));
                            }
                            else if (label is CILParser.Int32Context int32)
                            {
                                int offset = VisitInt32(int32).Value;
                                LabelHandle labelHandle = _currentMethod!.Definition.MethodBody.DefineLabel();
                                labels.Add((labelHandle, offset));
                            }
                            }
                        }
                        if (labels.Count > 0)
                        {
                            var switchEncoder = _currentMethod!.Definition.MethodBody.Switch(labels.Count);
                            foreach (var label in labels)
                            {
                                switchEncoder.Branch(label.Label);
                            }
                        }
                        else
                        {
                            // Empty switch: emit opcode + 0 count manually
                            _currentMethod!.Definition.MethodBody.OpCode(ILOpCode.Switch);
                            _currentMethod.Definition.MethodBody.CodeBuilder.WriteInt32(0);
                        }
                        // Now that we've emitted the switch instruction, we can go back and mark the offset-based target labels
                        foreach (var label in labels)
                        {
                            if (label.Offset is int offset)
                            {
                                _currentMethod.Definition.MethodBody.MarkLabel(label.Label, _currentMethod.Definition.MethodBody.Offset + offset);
                            }
                        }
                    }
                    break;
                case CILParser.RULE_instr_tok:
                    _currentMethod!.Definition.MethodBody.OpCode(opcode);
                    if (context.int32() is { } tokenValue)
                    {
                        _currentMethod.Definition.MethodBody.CodeBuilder.WriteInt32(VisitInt32(tokenValue).Value);
                        break;
                    }

                    var tok = VisitOwnerType(context.ownerType()).Value;
                    if (tok is EntityRegistry.TypeReferenceEntity tokTypeRef)
                    {
                        tokTypeRef.RecordBlobToWriteResolvedToken(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                    }
                    else if (tok is EntityRegistry.MemberReferenceEntity tokMemberRef)
                    {
                        tokMemberRef.RecordBlobToWriteResolvedHandle(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                    }
                    else
                    {
                        _currentMethod.Definition.MethodBody.Token(tok.Handle);
                    }
                    break;
                case CILParser.RULE_instr_type:
                    {
                        var arg = VisitTypeSpec(context.typeSpec()).Value;
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        if (arg is EntityRegistry.TypeReferenceEntity argTypeRef)
                        {
                            argTypeRef.RecordBlobToWriteResolvedToken(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                        }
                        else
                        {
                            _currentMethod.Definition.MethodBody.Token(arg.Handle);
                        }
                    }
                    break;
                case CILParser.RULE_instr_var:
                    {
                        string instrName = opcode.ToString();
                        bool isShortForm = instrName.EndsWith("_s");
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        if (context.int32() is CILParser.Int32Context int32)
                        {
                            int value = VisitInt32(int32).Value;
                            if (isShortForm)
                            {
                                // Emit a byte instead of the int for the short form
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)value);
                            }
                            else
                            {
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteInt32(value);
                            }
                        }
                        else
                        {
                            Debug.Assert(context.id() is not null);
                            string varName = VisitId(context.id()!).Value;
                            int? index = null;
                            if (instrName.Contains("arg"))
                            {
                                if (_currentMethod!.ArgumentNames.TryGetValue(varName, out var argIndex))
                                {
                                    index = argIndex;

                                    if (_currentMethod.Definition.SignatureHeader.IsInstance)
                                    {
                                        index++;
                                    }
                                }
                                else
                                {
                                    ReportError(DiagnosticIds.ArgumentNotFound, string.Format(DiagnosticMessageTemplates.ArgumentNotFound, varName), context);
                                }
                            }
                            else
                            {
                                for (int i = _currentMethod!.LocalsScopes.Count - 1; i >= 0 ; i--)
                                {
                                    if (_currentMethod.LocalsScopes[i].TryGetValue(varName, out var localIndex))
                                    {
                                        index = localIndex;
                                        break;
                                    }
                                }
                                if (index is null)
                                {
                                    ReportError(DiagnosticIds.LocalNotFound, string.Format(DiagnosticMessageTemplates.LocalNotFound, varName), context);
                                }
                            }

                            index ??= -1;

                            if (isShortForm)
                            {
                                // Emit a byte instead of the int for the short form
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)index.Value);
                            }
                            else
                            {
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteInt32(index.Value);
                            }
                        }
                    }
                    break;
            }
            return GrammarResult.SentinelValue.Result;
        }

        public GrammarResult.Literal<ILOpCode> VisitInstr_brtarget(CILParser.Instr_brtargetContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_field(CILParser.Instr_fieldContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_i(CILParser.Instr_iContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_i8(CILParser.Instr_i8Context context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_method(CILParser.Instr_methodContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_none(CILParser.Instr_noneContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_r(CILParser.Instr_rContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_sig(CILParser.Instr_sigContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_string(CILParser.Instr_stringContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_switch(CILParser.Instr_switchContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_tok(CILParser.Instr_tokContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_type(CILParser.Instr_typeContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_var(CILParser.Instr_varContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        private static ILOpCode ParseOpCodeFromToken(IToken token)
        {
            string text = token.Text.TrimEnd('.');
            if (text == "unused")
            {
                // Native ilasm's keyword index uses the last matching opcode.def entry, CEE_UNUSED70.
                return ILOpCode.Unused;
            }

            string normalized = text.Replace('.', '_');

            // Handle instruction aliases that don't directly map to ILOpCode enum names
            normalized = normalized switch
            {
                "ldelem_u8" => "ldelem_i8",
                "ldind_u8" => "ldind_i8",
                "endfault" => "endfinally",
                _ => normalized
            };

            return (ILOpCode)Enum.Parse(typeof(ILOpCode), normalized, ignoreCase: true);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitInstr(CILParser.InstrContext context) => VisitInstr(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_brtarget(CILParser.Instr_brtargetContext context) => VisitInstr_brtarget(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_field(CILParser.Instr_fieldContext context) => VisitInstr_field(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_i(CILParser.Instr_iContext context) => VisitInstr_i(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_i8(CILParser.Instr_i8Context context) => VisitInstr_i8(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_method(CILParser.Instr_methodContext context) => VisitInstr_method(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_none(CILParser.Instr_noneContext context) => VisitInstr_none(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_r(CILParser.Instr_rContext context) => VisitInstr_r(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_sig(CILParser.Instr_sigContext context) => VisitInstr_sig(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_string(CILParser.Instr_stringContext context) => VisitInstr_string(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_switch(CILParser.Instr_switchContext context) => VisitInstr_switch(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_tok(CILParser.Instr_tokContext context) => VisitInstr_tok(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_type(CILParser.Instr_typeContext context) => VisitInstr_type(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_var(CILParser.Instr_varContext context) => VisitInstr_var(context);

        GrammarResult ICILVisitor<GrammarResult>.VisitMdtoken(ILAssembler.CILParser.MdtokenContext context) => VisitMdtoken(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMdtoken(CILParser.MdtokenContext context)
        {
            return new(_entityRegistry.ResolveHandleToEntity(MetadataTokens.EntityHandle(VisitInt32(context.int32()).Value)));
        }

#pragma warning restore CA1822 // Mark members as static
}
