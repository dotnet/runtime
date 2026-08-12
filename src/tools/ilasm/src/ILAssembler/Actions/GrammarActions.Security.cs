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
        GrammarResult ICILVisitor<GrammarResult>.VisitSecAction(CILParser.SecActionContext context) => VisitSecAction(context);
        public static GrammarResult.Literal<DeclarativeSecurityAction> VisitSecAction(CILParser.SecActionContext context)
        {
            return context.GetText() switch
            {
                "request" => new(DeclarativeSecurityAction.Request),
                "demand" => new(DeclarativeSecurityAction.Demand),
                "assert" => new(DeclarativeSecurityAction.Assert),
                "deny" => new(DeclarativeSecurityAction.Deny),
                "permitonly" => new(DeclarativeSecurityAction.PermitOnly),
                "linkcheck" => new(DeclarativeSecurityAction.LinkDemand),
                "inheritcheck" => new(DeclarativeSecurityAction.InheritanceDemand),
                "reqmin" => new(DeclarativeSecurityAction.RequestMinimum),
                "reqopt" => new(DeclarativeSecurityAction.RequestOptional),
                "reqrefuse" => new(DeclarativeSecurityAction.RequestRefuse),
                "prejitgrant" => new(DeclarativeSecurityAction.PrejitGrant),
                "prejitdeny" => new(DeclarativeSecurityAction.PrejitDeny),
                "noncasdemand" => new(DeclarativeSecurityAction.NonCasDemand),
                "noncaslinkdemand" => new(DeclarativeSecurityAction.NonCasLinkDemand),
                "noncasinheritance" => new(DeclarativeSecurityAction.NonCasInheritanceDemand),
                _ => throw new UnreachableException()
            };
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitSecAttrBlob(CILParser.SecAttrBlobContext context) => VisitSecAttrBlob(context);
        public GrammarResult.FormattedBlob VisitSecAttrBlob(CILParser.SecAttrBlobContext context)
        {
            var blob = new BlobBuilder();

            string attributeName = string.Empty;

            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec && VisitTypeSpec(typeSpec).Value is EntityRegistry.IHasReflectionNotation reflectionNotation)
            {
                attributeName = reflectionNotation.ReflectionNotation;
            }
            else if (context.SQSTRING() is { } sqstring)
            {
                attributeName = StringHelpers.ParseQuotedString(sqstring.GetText());
            }

            blob.WriteSerializedString(attributeName);
            VisitCustomBlobNVPairs(context.customBlobNVPairs()).Value.WriteContentTo(blob);

            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSecAttrSetBlob(CILParser.SecAttrSetBlobContext context) => VisitSecAttrSetBlob(context);
        public GrammarResult.FormattedBlob VisitSecAttrSetBlob(CILParser.SecAttrSetBlobContext context)
        {
            BlobBuilder blob = new();
            var secAttributes = context.secAttrBlob();
            blob.WriteByte((byte)'.');
            blob.WriteCompressedInteger(secAttributes.Length);
            foreach (var secAttribute in secAttributes)
            {
                VisitSecAttrBlob(secAttribute).Value.WriteContentTo(blob);
            }
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSecDecl(CILParser.SecDeclContext context) => VisitSecDecl(context);
        public GrammarResult.Literal<EntityRegistry.DeclarativeSecurityAttributeEntity?> VisitSecDecl(CILParser.SecDeclContext context)
        {
            if (context.PERMISSION() is not null)
            {
                ReportError(DiagnosticIds.UnsupportedSecurityDeclaration,
                    DiagnosticMessageTemplates.UnsupportedSecurityDeclaration,
                    context);
                return new(null);
            }
            DeclarativeSecurityAction action = VisitSecAction(context.secAction()).Value;
            BlobBuilder value;
            if (context.secAttrSetBlob() is CILParser.SecAttrSetBlobContext setBlob)
            {
                value = VisitSecAttrSetBlob(setBlob).Value;
            }
            else if (context.bytes() is CILParser.BytesContext bytes)
            {
                value = new();
                value.WriteBytes(VisitBytes(bytes));
            }
            else if (context.compQstring() is CILParser.CompQstringContext str)
            {
                value = new BlobBuilder();
                value.WriteUTF16(VisitCompQstring(str).Value);
                value.WriteUTF16("\0");
            }
            else
            {
                throw new UnreachableException();
            }
            return new(_entityRegistry.CreateDeclarativeSecurityAttribute(action, value));
        }

    }
}
