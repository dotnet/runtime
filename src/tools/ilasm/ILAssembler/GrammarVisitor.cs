// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ILAssembler
{
    internal abstract record GrammarResult
    {
        protected GrammarResult() { }

        public sealed record String(string Value): GrammarResult;

        public sealed record Literal<T>(T Value): GrammarResult;

        public sealed record Sequence<T>(ImmutableArray<T> Value): GrammarResult;

        /// <summary>
        /// A sequence of bytes written by a BlobBuilder.
        /// </summary>
        /// <param name="Value">The bytes of the blob.</param>
        public sealed record FormattedBlob(ImmutableArray<byte> Value): GrammarResult;

        public sealed record SentinelValue
        {
            public static SentinelValue Instance { get; } = new();

            public static Literal<SentinelValue> Result { get; } = new(Instance);
        }
    }

    internal sealed class GrammarVisitor : ICILVisitor<GrammarResult>
    {
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        private readonly SourceText _document;

        public GrammarVisitor(SourceText document)
        {
            _document = document;
        }

        public GrammarResult Visit(IParseTree tree) => tree.Accept(this);
        public GrammarResult VisitAlignment(CILParser.AlignmentContext context) => throw new NotImplementedException();
        public GrammarResult VisitAsmAttr(CILParser.AsmAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitAsmAttrAny(CILParser.AsmAttrAnyContext context) => throw new NotImplementedException();
        public GrammarResult VisitAsmOrRefDecl(CILParser.AsmOrRefDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyBlock(CILParser.AssemblyBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyDecl(CILParser.AssemblyDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyDecls(CILParser.AssemblyDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyRefDecl(CILParser.AssemblyRefDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyRefDecls(CILParser.AssemblyRefDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyRefHead(CILParser.AssemblyRefHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitAtOpt(CILParser.AtOptContext context) => throw new NotImplementedException();
        GrammarResult ICILVisitor<GrammarResult>.VisitBoolSeq(CILParser.BoolSeqContext context) => VisitBoolSeq(context);
        public static GrammarResult.Sequence<bool> VisitBoolSeq(CILParser.BoolSeqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<bool>();

            foreach (var item in context.truefalse())
            {
                builder.AddRange(VisitTruefalse(item).Value);
            }

            return new(builder.ToImmutable());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitBound(CILParser.BoundContext context) => VisitBound(context);
        public GrammarResult.Sequence<int> VisitBound(CILParser.BoundContext context)
        {
            const int UndefinedBoundValue = 0x7FFFFFFF;
            bool hasEllipsis = context.ELLIPSIS() is not null;
            if (context.ChildCount == 0 || (context.ChildCount == 1 && hasEllipsis))
            {
                return new(ImmutableArray.Create(UndefinedBoundValue, UndefinedBoundValue));
            }

            var indicies = context.int32();

            int firstValue = VisitInt32(indicies[0]).Value;

            return (indicies.Length, hasEllipsis) switch
            {
                (1, false) => new(ImmutableArray.Create(0, firstValue)),
                (1, true) => new(ImmutableArray.Create(firstValue, UndefinedBoundValue)),
                (2, false) => new(ImmutableArray.Create(firstValue, VisitInt32(indicies[1]).Value - firstValue + 1)),
                _ => throw new InvalidOperationException("unreachable")
            };
        }

        public GrammarResult VisitBounds(CILParser.BoundsContext context) => throw new NotImplementedException();
        GrammarResult ICILVisitor<GrammarResult>.VisitBytes(CILParser.BytesContext context) => VisitBytes(context);
        public static GrammarResult.Sequence<byte> VisitBytes(CILParser.BytesContext context)
        {
            var builder = ImmutableArray.CreateBuilder<byte>();

            foreach (var item in context.hexbytes())
            {
                builder.AddRange(VisitHexbytes(item).Value);
            }

            return new(builder.ToImmutable());
        }
        public GrammarResult VisitCallConv(CILParser.CallConvContext context) => throw new NotImplementedException();
        public GrammarResult VisitCallKind(CILParser.CallKindContext context) => throw new NotImplementedException();
        public GrammarResult VisitCatchClause(CILParser.CatchClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitCaValue(CILParser.CaValueContext context) => throw new NotImplementedException();
        public GrammarResult VisitChildren(IRuleNode node)
        {
            throw new NotImplementedException("Generic child visitor");
        }
        public GrammarResult VisitClassAttr(CILParser.ClassAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitClassDecl(CILParser.ClassDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitClassDecls(CILParser.ClassDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitClassHead(CILParser.ClassHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitClassHeadBegin(CILParser.ClassHeadBeginContext context) => throw new NotImplementedException();
        public GrammarResult VisitClassName(CILParser.ClassNameContext context) => throw new NotImplementedException();
        public GrammarResult VisitClassSeq(CILParser.ClassSeqContext context) => throw new NotImplementedException();
        public GrammarResult VisitClassSeqElement(CILParser.ClassSeqElementContext context) => throw new NotImplementedException();
        public GrammarResult VisitCompControl(CILParser.CompControlContext context)
        {
            throw new NotImplementedException("Compilation control directives should be handled by a custom token stream.");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCompQstring(CILParser.CompQstringContext context)
        {
            return VisitCompQstring(context);
        }

        private static GrammarResult.String VisitCompQstring(CILParser.CompQstringContext context)
        {
            StringBuilder builder = new();
            foreach (var item in context.QSTRING())
            {
                builder.Append(item.Symbol.Text);
            }
            return new(builder.ToString());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCorflags(CILParser.CorflagsContext context) => VisitCorflags(context);
        public GrammarResult.Literal<int> VisitCorflags(CILParser.CorflagsContext context) => VisitInt32(context.int32());
        public GrammarResult VisitCustomAttrDecl(CILParser.CustomAttrDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomBlobArgs(CILParser.CustomBlobArgsContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomBlobDescr(CILParser.CustomBlobDescrContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomBlobNVPairs(CILParser.CustomBlobNVPairsContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomDescr(CILParser.CustomDescrContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomDescrWithOwner(CILParser.CustomDescrWithOwnerContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomType(CILParser.CustomTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitDataDecl(CILParser.DataDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdBody(CILParser.DdBodyContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdHead(CILParser.DdHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdItem(CILParser.DdItemContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdItemCount(CILParser.DdItemCountContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdItemList(CILParser.DdItemListContext context) => throw new NotImplementedException();
        public GrammarResult VisitDecl(CILParser.DeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitDecls(CILParser.DeclsContext context) => throw new NotImplementedException();

        GrammarResult ICILVisitor<GrammarResult>.VisitDottedName(CILParser.DottedNameContext context)
        {
            return VisitDottedName(context);
        }

        public static GrammarResult.String VisitDottedName(CILParser.DottedNameContext context)
        {
            StringBuilder builder = new();
            foreach (var item in context.ID())
            {
                builder.Append(item.Symbol.Text).Append('.');
            }
            return new(builder.ToString());
        }
        public GrammarResult VisitElementType(CILParser.ElementTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitErrorNode(IErrorNode node) => throw new NotImplementedException();
        public GrammarResult VisitEsHead(CILParser.EsHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventAttr(CILParser.EventAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventDecl(CILParser.EventDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventDecls(CILParser.EventDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventHead(CILParser.EventHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitExportHead(CILParser.ExportHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptAttr(CILParser.ExptAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptypeDecl(CILParser.ExptypeDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptypeDecls(CILParser.ExptypeDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptypeHead(CILParser.ExptypeHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitExtendsClause(CILParser.ExtendsClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitExtSourceSpec(CILParser.ExtSourceSpecContext context) => throw new NotImplementedException();
        GrammarResult ICILVisitor<GrammarResult>.VisitF32seq(CILParser.F32seqContext context) => VisitF32seq(context);
        public GrammarResult.Sequence<float> VisitF32seq(CILParser.F32seqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<float>();

            foreach (var item in context.children)
            {
                builder.Add((float)(item switch
                {
                    CILParser.Int32Context int32 => VisitInt32(int32).Value,
                    CILParser.Float64Context float64 => VisitFloat64(float64).Value,
                    _ => throw new InvalidOperationException("unreachable")
                }));
            }
            return new(builder.MoveToImmutable());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitF64seq(CILParser.F64seqContext context) => VisitF64seq(context);
        public GrammarResult.Sequence<double> VisitF64seq(CILParser.F64seqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<double>();

            foreach (var item in context.children)
            {
                builder.Add((double)(item switch
                {
                    CILParser.Int64Context int64 => VisitInt64(int64).Value,
                    CILParser.Float64Context float64 => VisitFloat64(float64).Value,
                    _ => throw new InvalidOperationException("unreachable")
                }));
            }
            return new(builder.MoveToImmutable());
        }
        public GrammarResult VisitFaultClause(CILParser.FaultClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldAttr(CILParser.FieldAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldDecl(CILParser.FieldDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldInit(CILParser.FieldInitContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldOrProp(CILParser.FieldOrPropContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldSerInit(CILParser.FieldSerInitContext context) => throw new NotImplementedException();
        public GrammarResult VisitFileAttr(CILParser.FileAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitFileDecl(CILParser.FileDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitFileEntry(CILParser.FileEntryContext context) => throw new NotImplementedException();
        public GrammarResult VisitFilterClause(CILParser.FilterClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitFilterHead(CILParser.FilterHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitFinallyClause(CILParser.FinallyClauseContext context) => throw new NotImplementedException();

        GrammarResult ICILVisitor<GrammarResult>.VisitFloat64(CILParser.Float64Context context) => throw new NotImplementedException();
        public GrammarResult.Literal<double> VisitFloat64(CILParser.Float64Context context)
        {
            if (context.FLOAT64() is ITerminalNode float64)
            {
                string text = float64.Symbol.Text;
                bool neg = text.StartsWith("-", StringComparison.InvariantCulture);
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    result = neg ? double.MaxValue : double.MinValue;
                }
                return new(result);
            }
            else if (context.int32() is CILParser.Int32Context int32)
            {
                int value = VisitInt32(int32).Value;
                return new(Unsafe.As<int, float>(ref value));
            }
            else if (context.int64() is CILParser.Int64Context int64)
            {
                long value = VisitInt64(int64).Value;
                return new(Unsafe.As<long, double>(ref value));
            }
            throw new InvalidOperationException("unreachable");
        }
        public GrammarResult VisitGenArity(CILParser.GenArityContext context) => throw new NotImplementedException();
        public GrammarResult VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context) => throw new NotImplementedException();
        public GrammarResult VisitHandlerBlock(CILParser.HandlerBlockContext context) => throw new NotImplementedException();

        GrammarResult ICILVisitor<GrammarResult>.VisitHexbytes(CILParser.HexbytesContext context)
        {
            return VisitHexbytes(context);
        }

        public static GrammarResult.Sequence<byte> VisitHexbytes(CILParser.HexbytesContext context)
        {
            ITerminalNode[] bytes = context.HEXBYTE();
            var builder = ImmutableArray.CreateBuilder<byte>(bytes.Length);
            foreach (var @byte in bytes)
            {
                builder.Add(byte.Parse(@byte.Symbol.Text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
            }
            return new(builder.MoveToImmutable());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitI16seq(CILParser.I16seqContext context) => VisitI16seq(context);
        public GrammarResult.Sequence<short> VisitI16seq(CILParser.I16seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<short>(values.Length);
            foreach (var value in values)
            {
                builder.Add((short)VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitI32seq(CILParser.I32seqContext context) => VisitI32seq(context);
        public GrammarResult.Sequence<int> VisitI32seq(CILParser.I32seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<int>(values.Length);
            foreach (var value in values)
            {
                builder.Add(VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitI8seq(CILParser.I8seqContext context) => VisitI8seq(context);
        public GrammarResult.Sequence<byte> VisitI8seq(CILParser.I8seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<byte>(values.Length);
            foreach (var value in values)
            {
                builder.Add((byte)VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitI64seq(CILParser.I64seqContext context) => VisitI64seq(context);
        public GrammarResult.Sequence<long> VisitI64seq(CILParser.I64seqContext context)
        {
            var values = context.int64();
            var builder = ImmutableArray.CreateBuilder<long>(values.Length);
            foreach (var value in values)
            {
                builder.Add(VisitInt64(value).Value);
            }
            return new(builder.MoveToImmutable());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitId(CILParser.IdContext context) => VisitId(context);
        public static GrammarResult.String VisitId(CILParser.IdContext context)
        {
            return new GrammarResult.String((context.ID() ?? context.SQSTRING()).Symbol.Text);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitIidParamIndex(CILParser.IidParamIndexContext context) => VisitIidParamIndex(context);
        public GrammarResult.Literal<int> VisitIidParamIndex(CILParser.IidParamIndexContext context)
            => context.int32() is CILParser.Int32Context int32 ? VisitInt32(int32) : new(-1);

        GrammarResult ICILVisitor<GrammarResult>.VisitImagebase(CILParser.ImagebaseContext context) => VisitImagebase(context);
        public GrammarResult.Literal<long> VisitImagebase(CILParser.ImagebaseContext context) => VisitInt64(context.int64());

        public GrammarResult VisitImplAttr(CILParser.ImplAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitImplClause(CILParser.ImplClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitImplList(CILParser.ImplListContext context) => throw new NotImplementedException();
        public GrammarResult VisitInitOpt(CILParser.InitOptContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr(CILParser.InstrContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_brtarget(CILParser.Instr_brtargetContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_field(CILParser.Instr_fieldContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_i(CILParser.Instr_iContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_i8(CILParser.Instr_i8Context context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_method(CILParser.Instr_methodContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_none(CILParser.Instr_noneContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_r(CILParser.Instr_rContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_r_head(CILParser.Instr_r_headContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_sig(CILParser.Instr_sigContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_string(CILParser.Instr_stringContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_switch(CILParser.Instr_switchContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_tok(CILParser.Instr_tokContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_type(CILParser.Instr_typeContext context) => throw new NotImplementedException();
        public GrammarResult VisitInstr_var(CILParser.Instr_varContext context) => throw new NotImplementedException();


        private static ReadOnlySpan<char> ProcessIntegerValue(ReadOnlySpan<char> value, out NumberStyles parseStyle, out bool negate)
        {
            parseStyle = NumberStyles.None;
            negate = false;
            if (value.StartsWith("-".AsSpan()))
            {
                negate = true;
            }

            if (value.StartsWith("0x".AsSpan()))
            {
                parseStyle = NumberStyles.AllowHexSpecifier;
                value = value.Slice(2);
            }
            else if (value.StartsWith("0".AsSpan()))
            {
                // TODO: Handle octal
            }

            return value;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitInt32(ILAssembler.CILParser.Int32Context context)
        {
            return VisitInt32(context);
        }

        public GrammarResult.Literal<int> VisitInt32(CILParser.Int32Context context)
        {
            IToken node = context.INT32().Symbol;

            ReadOnlySpan<char> value = node.Text.AsSpan();

            NumberStyles parseStyle;
            bool negate;
            value = ProcessIntegerValue(value, out parseStyle, out negate);

            string trimmedValue = value.ToString();
            if (!long.TryParse(trimmedValue, parseStyle, CultureInfo.InvariantCulture, out long num))
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticIds.LiteralOutOfRange,
                    DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, trimmedValue),
                    Location.From(node, _document)));
                return new GrammarResult.Literal<int>(0);
            }

            if (negate)
            {
                num = -num;
            }

            return new GrammarResult.Literal<int>((int)num);
        }


        GrammarResult ICILVisitor<GrammarResult>.VisitInt64(ILAssembler.CILParser.Int64Context context)
        {
            return VisitInt64(context);
        }

        public GrammarResult.Literal<long> VisitInt64(CILParser.Int64Context context)
        {
            IToken node = context.INT32().Symbol;

            ReadOnlySpan<char> value = node.Text.AsSpan();

            NumberStyles parseStyle;
            bool negate;
            value = ProcessIntegerValue(value, out parseStyle, out negate);

            string trimmedValue = value.ToString();
            if (!long.TryParse(trimmedValue, parseStyle, CultureInfo.InvariantCulture, out long num))
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticIds.LiteralOutOfRange,
                    DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, trimmedValue),
                    Location.From(node, _document)));
                return new GrammarResult.Literal<long>(0);
            }

            if (negate)
            {
                num = -num;
            }

            return new GrammarResult.Literal<long>(num);
        }

        public GrammarResult VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => throw new NotImplementedException();
        public GrammarResult VisitLabels(CILParser.LabelsContext context) => throw new NotImplementedException();
        public GrammarResult VisitLanguageDecl(CILParser.LanguageDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitLocalsHead(CILParser.LocalsHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitManifestResDecl(CILParser.ManifestResDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitManifestResDecls(CILParser.ManifestResDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitManifestResHead(CILParser.ManifestResHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitManresAttr(CILParser.ManresAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitMarshalBlob(CILParser.MarshalBlobContext context) => throw new NotImplementedException();
        public GrammarResult VisitMarshalBlobHead(CILParser.MarshalBlobHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitMarshalClause(CILParser.MarshalClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitMdtoken(CILParser.MdtokenContext context) => throw new NotImplementedException();
        public GrammarResult VisitMemberRef(CILParser.MemberRefContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethAttr(CILParser.MethAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethodDecl(CILParser.MethodDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethodDecls(CILParser.MethodDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethodHead(CILParser.MethodHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethodName(CILParser.MethodNameContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethodRef(CILParser.MethodRefContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethodSpec(CILParser.MethodSpecContext context) => throw new NotImplementedException();
        public GrammarResult VisitModuleHead(CILParser.ModuleHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitMscorlib(CILParser.MscorlibContext context) => throw new NotImplementedException();
        public GrammarResult VisitNameSpaceHead(CILParser.NameSpaceHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitNameValPair(CILParser.NameValPairContext context) => throw new NotImplementedException();
        public GrammarResult VisitNameValPairs(CILParser.NameValPairsContext context) => throw new NotImplementedException();
        public GrammarResult VisitNativeType(CILParser.NativeTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitNativeTypeElement(CILParser.NativeTypeElementContext context) => throw new NotImplementedException();
        public GrammarResult VisitObjSeq(CILParser.ObjSeqContext context) => throw new NotImplementedException();
        public GrammarResult VisitOwnerType(CILParser.OwnerTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitParamAttr(CILParser.ParamAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitParamAttrElement(CILParser.ParamAttrElementContext context) => throw new NotImplementedException();
        public GrammarResult VisitPinvAttr(CILParser.PinvAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitPropAttr(CILParser.PropAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitPropDecl(CILParser.PropDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitPropDecls(CILParser.PropDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitPropHead(CILParser.PropHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitRepeatOpt(CILParser.RepeatOptContext context) => throw new NotImplementedException();
        public GrammarResult VisitScopeBlock(CILParser.ScopeBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecAction(CILParser.SecActionContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecAttrBlob(CILParser.SecAttrBlobContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecAttrSetBlob(CILParser.SecAttrSetBlobContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecDecl(CILParser.SecDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitSehBlock(CILParser.SehBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitSehClause(CILParser.SehClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitSehClauses(CILParser.SehClausesContext context) => throw new NotImplementedException();
        public GrammarResult VisitSeralizType(CILParser.SeralizTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitSeralizTypeElement(CILParser.SeralizTypeElementContext context) => throw new NotImplementedException();
        public GrammarResult VisitSerInit(CILParser.SerInitContext context) => throw new NotImplementedException();
        public GrammarResult VisitSigArg(CILParser.SigArgContext context) => throw new NotImplementedException();
        public GrammarResult VisitSigArgs(CILParser.SigArgsContext context) => throw new NotImplementedException();
        public GrammarResult VisitSimpleType(CILParser.SimpleTypeContext context) => throw new NotImplementedException();
        GrammarResult ICILVisitor<GrammarResult>.VisitSlashedName(CILParser.SlashedNameContext context)
        {
            return VisitSlashedName(context);
        }

        public static GrammarResult.String VisitSlashedName(CILParser.SlashedNameContext context)
        {
            StringBuilder builder = new();
            foreach (var item in context.dottedName())
            {
                builder.Append(VisitDottedName(item)).Append('/');
            }
            return new(builder.ToString());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSqstringSeq(ILAssembler.CILParser.SqstringSeqContext context) => VisitSqstringSeq(context);

        public static GrammarResult.Sequence<string?> VisitSqstringSeq(CILParser.SqstringSeqContext context)
        {
            var strings = ImmutableArray.CreateBuilder<string?>(context.ChildCount);
            foreach (var child in context.children)
            {
                string? str = null;

                if (child is ITerminalNode { Symbol: {Type: CILParser.SQSTRING, Text: string stringValue } })
                {
                    str = stringValue;
                }

                strings.Add(str);
            }
            return new(strings.ToImmutable());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitStackreserve(CILParser.StackreserveContext context) => VisitStackreserve(context);
        public GrammarResult.Literal<long> VisitStackreserve(CILParser.StackreserveContext context) => VisitInt64(context.int64());

        GrammarResult ICILVisitor<GrammarResult>.VisitSubsystem(CILParser.SubsystemContext context) => VisitSubsystem(context);
        public GrammarResult.Literal<int> VisitSubsystem(CILParser.SubsystemContext context) => VisitInt32(context.int32());

        public GrammarResult VisitTerminal(ITerminalNode node) => throw new NotImplementedException();
        public GrammarResult VisitTls(CILParser.TlsContext context) => throw new NotImplementedException();

        GrammarResult ICILVisitor<GrammarResult>.VisitTruefalse(ILAssembler.CILParser.TruefalseContext context) => VisitTruefalse(context);

        public static GrammarResult.Literal<bool> VisitTruefalse(CILParser.TruefalseContext context)
        {
            return new(bool.Parse(context.GetText()));
        }

        public GrammarResult VisitTryBlock(CILParser.TryBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitTryHead(CILParser.TryHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitTyBound(CILParser.TyBoundContext context) => throw new NotImplementedException();
        public GrammarResult VisitTyparAttrib(CILParser.TyparAttribContext context) => throw new NotImplementedException();
        public GrammarResult VisitTyparAttribs(CILParser.TyparAttribsContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypars(CILParser.TyparsContext context) => throw new NotImplementedException();
        public GrammarResult VisitTyparsClause(CILParser.TyparsClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitTyparsRest(CILParser.TyparsRestContext context) => throw new NotImplementedException();
        public GrammarResult VisitType(CILParser.TypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypeArgs(CILParser.TypeArgsContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypedefDecl(CILParser.TypedefDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypelist(CILParser.TypelistContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypeList(CILParser.TypeListContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypeListNotEmpty(CILParser.TypeListNotEmptyContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypeSpec(CILParser.TypeSpecContext context) => throw new NotImplementedException();
        public GrammarResult VisitVariantType(CILParser.VariantTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitVariantTypeElement(CILParser.VariantTypeElementContext context) => throw new NotImplementedException();
        public GrammarResult VisitVtableDecl(CILParser.VtableDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitVtfixupAttr(CILParser.VtfixupAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitVtfixupDecl(CILParser.VtfixupDeclContext context) => throw new NotImplementedException();
    }
}
