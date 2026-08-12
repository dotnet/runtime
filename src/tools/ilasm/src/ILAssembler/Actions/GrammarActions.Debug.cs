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
        public GrammarResult VisitCompControl(CILParser.CompControlContext context)
        {
            // All compilation control directives that need special handling will be handled
            // directly in the token stream before parsing.
            // Any that reach here can be ignored.
            return GrammarResult.SentinelValue.Result;
        }

        // esHead is '.line' or '#line' - this is just the keyword, actual parsing is in VisitExtSourceSpec.
        public GrammarResult VisitEsHead(CILParser.EsHeadContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        public GrammarResult VisitExtSourceSpec(CILParser.ExtSourceSpecContext context)
        {
            // Parse .line directive to extract source location info
            // Grammar: esHead int32 (',' int32)? (':' int32 (',' int32)?)? (SQSTRING | QSTRING)?
            var int32s = context.int32();
            var sqstring = context.SQSTRING();
            var qstring = context.QSTRING();

            // Extract line/column info based on number of int32s
            int startLine = 0, endLine = 0, startColumn = 0, endColumn = 0;

            if (int32s.Length >= 1)
            {
                startLine = VisitInt32(int32s[0]).Value;
                endLine = startLine;
            }
            if (int32s.Length >= 2)
            {
                // Could be endLine or startColumn depending on separator
                string contextText = context.GetText();
                if (contextText.Contains(',') && contextText.IndexOf(',') < contextText.IndexOf(':'))
                {
                    // Format: startLine,endLine:...
                    endLine = VisitInt32(int32s[1]).Value;
                }
                else
                {
                    // Format: line:column...
                    startColumn = VisitInt32(int32s[1]).Value;
                    endColumn = startColumn;
                }
            }
            if (int32s.Length >= 3)
            {
                startColumn = VisitInt32(int32s[2]).Value;
                endColumn = startColumn;
            }
            if (int32s.Length >= 4)
            {
                endColumn = VisitInt32(int32s[3]).Value;
            }

            // Extract filename if present
            string? filePath = null;
            if (sqstring is not null)
            {
                filePath = StringHelpers.ParseQuotedString(sqstring.GetText());
            }
            else if (qstring is not null)
            {
                filePath = StringHelpers.ParseQuotedString(qstring.GetText());
            }

            // Update current document path if specified
            if (filePath is not null)
            {
                _currentDocumentPath = filePath;
            }

            // If we're in a method, record the sequence point
            if (_currentMethod is not null && _currentDocumentPath is not null)
            {
                int ilOffset = _currentMethod.Definition.MethodBody.Offset;
                _currentMethod.Definition.DebugInfo.DocumentPath ??= _currentDocumentPath;

                // 0xFEEFEE indicates a hidden sequence point
                if (startLine == 0xFEEFEE)
                {
                    AddSequencePoint(EntityRegistry.SequencePoint.Hidden(ilOffset));
                }
                else
                {
                    if (endLine == startLine && endColumn == startColumn)
                    {
                        endColumn++;
                    }

                    AddSequencePoint(
                        new EntityRegistry.SequencePoint(
                            ilOffset,
                            startLine,
                            startColumn,
                            endLine,
                            endColumn));
                }

                void AddSequencePoint(EntityRegistry.SequencePoint sequencePoint)
                {
                    List<EntityRegistry.SequencePoint> sequencePoints =
                        _currentMethod.Definition.DebugInfo.SequencePoints;
                    if (sequencePoints.Count > 0 && sequencePoints[^1].ILOffset == ilOffset)
                    {
                        sequencePoints[^1] = sequencePoint;
                    }
                    else
                    {
                        sequencePoints.Add(sequencePoint);
                    }
                }
            }

            return GrammarResult.SentinelValue.Result;
        }

        public GrammarResult VisitLanguageDecl(CILParser.LanguageDeclContext context)
        {
            // .language languageString (',' languageString (',' languageString)?)?
            // First GUID: language (e.g., C#, VB, IL)
            // Second GUID: vendor (optional)
            // Third GUID: document type (optional)
            var strings = context.languageString();
            if (strings.Length >= 1 && Guid.TryParse(VisitLanguageString(strings[0]).Value, out var languageGuid))
            {
                _currentLanguageGuid = languageGuid;
            }
            if (strings.Length >= 2 && Guid.TryParse(VisitLanguageString(strings[1]).Value, out var vendorGuid))
            {
                _currentLanguageVendorGuid = vendorGuid;
            }
            if (strings.Length >= 3 && Guid.TryParse(VisitLanguageString(strings[2]).Value, out var docTypeGuid))
            {
                _currentDocumentTypeGuid = docTypeGuid;
            }
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitLanguageString(CILParser.LanguageStringContext context) => VisitLanguageString(context);
        public GrammarResult.String VisitLanguageString(CILParser.LanguageStringContext context)
        {
            if (context.SQSTRING() is not null)
            {
                return new(StringHelpers.ParseQuotedString(context.SQSTRING().GetText()));
            }
            return new(StringHelpers.ParseQuotedString(context.QSTRING().GetText()));
        }

    }
}
