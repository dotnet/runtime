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

namespace ILAssembler
{
#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions : ICILVisitor<GrammarResult>
    {
        public GrammarResult VisitDataDecl(CILParser.DataDeclContext context)
        {
            _ = VisitDdHead(context.ddHead());
            _ = VisitDdBody(context.ddBody());
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitDdBody(CILParser.DdBodyContext context)
        {
            if (context.ddItemList() is CILParser.DdItemListContext ddItemList)
            {
                _ = VisitDdItemList(ddItemList);
            }
            else
            {
                foreach (var item in context.ddItem())
                {
                    _ = VisitDdItem(item);
                }
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitDdHead(CILParser.DdHeadContext context)
        {
            _ = VisitTls(context.tls());
            if (context.id() is CILParser.IdContext id)
            {
                string name = VisitId(id).Value;
                if (!_mappedFieldDataNames.ContainsKey(name))
                {
                    _mappedFieldDataNames.Add(name, _mappedFieldData.Count);
                }
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitDdItem(CILParser.DdItemContext context)
        {
            if (context.compQstring() is CILParser.CompQstringContext str)
            {
                var value = VisitCompQstring(str).Value;
                _mappedFieldData.WriteUTF16(value);
                return GrammarResult.SentinelValue.Result;
            }
            else if (context.id() is CILParser.IdContext id)
            {
                // Reference to another data label - this will be patched with the target's RVA
                // during PE serialization by VTableExportPEBuilder.ApplyDataLabelFixups()
                string name = VisitId(id).Value;
                if (!_mappedFieldDataReferenceFixups.TryGetValue(name, out var fixups))
                {
                    _mappedFieldDataReferenceFixups[name] = fixups = new();
                }

                // Reserve 4 bytes for the RVA that will be patched later
                fixups.Add(_mappedFieldData.ReserveBytes(4));
                return GrammarResult.SentinelValue.Result;
            }
            else if (context.bytes() is CILParser.BytesContext bytes)
            {
                _mappedFieldData.WriteBytes(VisitBytes(bytes));
                return GrammarResult.SentinelValue.Result;
            }

            int itemCount = VisitDdItemCount(context.ddItemCount()).Value;

            if (context.INT8() is not null)
            {
                _mappedFieldData.WriteBytes(context.int32() is CILParser.Int32Context int32 ? (byte)VisitInt32(int32).Value : (byte)0, itemCount);
            }
            else if (context.INT16() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteInt16(context.int32() is CILParser.Int32Context int32 ? (short)VisitInt32(int32).Value : (short)0);
                }
            }
            else if (context.INT32_() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteInt32(context.int32() is CILParser.Int32Context int32 ? VisitInt32(int32).Value : 0);
                }
            }
            else if (context.INT64_() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteInt64(context.int64() is CILParser.Int64Context int64 ? VisitInt64(int64).Value : 0);
                }
            }
            else if (context.FLOAT32() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteSingle(context.float64() is CILParser.Float64Context float64 ? (float)VisitFloat64(float64).Value : 0);
                }
            }
            else if (context.FLOAT64_() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteDouble(context.float64() is CILParser.Float64Context float64 ? VisitFloat64(float64).Value : 0);
                }
            }
            return GrammarResult.SentinelValue.Result;
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitDdItemCount(CILParser.DdItemCountContext context) => VisitDdItemCount(context);
        public GrammarResult.Literal<int> VisitDdItemCount(CILParser.DdItemCountContext context) => new(context.int32() is CILParser.Int32Context ? VisitInt32(context.int32()).Value : 1);
        public GrammarResult VisitDdItemList(CILParser.DdItemListContext context)
        {
            foreach (var item in context.ddItem())
            {
                VisitDdItem(item);
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitTls(CILParser.TlsContext context)
        {
            if (context.GetText() == "tls")
            {
                ReportError(
                    DiagnosticIds.UnsupportedTlsData,
                    DiagnosticMessageTemplates.UnsupportedTlsData,
                    context);
            }

            return GrammarResult.SentinelValue.Result;
        }

    }
}
