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
        GrammarResult ICILVisitor<GrammarResult>.VisitBoolSeq(CILParser.BoolSeqContext context) => VisitBoolSeq(context);
        public static GrammarResult.FormattedBlob VisitBoolSeq(CILParser.BoolSeqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<bool>();

            foreach (var item in context.truefalse())
            {
                builder.AddRange(VisitTruefalse(item).Value);
            }

            return new(builder.ToImmutable().SerializeSequence());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCaValue(CILParser.CaValueContext context) => VisitCaValue(context);
        public GrammarResult.FormattedBlob VisitCaValue(CILParser.CaValueContext context)
        {
            BlobBuilder blob = new();
            if (context.truefalse() is CILParser.TruefalseContext truefalse)
            {
                blob.WriteByte((byte)SerializationTypeCode.Boolean);
                blob.WriteBoolean(VisitTruefalse(truefalse).Value);
            }
            else if (context.compQstring() is CILParser.CompQstringContext str)
            {
                blob.WriteUTF8(VisitCompQstring(str).Value);
                blob.WriteByte(0);
            }
            else if (context.className() is CILParser.ClassNameContext className)
            {
                var name = VisitClassName(className).Value;
                blob.WriteByte((byte)SerializationTypeCode.Enum);
                blob.WriteUTF8((name as EntityRegistry.IHasReflectionNotation)?.ReflectionNotation ?? "");
                blob.WriteByte(0);
                byte size = 4;
                if (context.INT8() is not null)
                {
                    size = 1;
                }
                else if (context.INT16() is not null)
                {
                    size = 2;
                }
                blob.WriteByte(size);
                blob.WriteInt32(VisitInt32(context.int32()).Value);
            }
            else
            {
                blob.WriteByte((byte)SerializationTypeCode.Int32);
                blob.WriteInt32(VisitInt32(context.int32()).Value);
            }
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomAttrDecl(CILParser.CustomAttrDeclContext context) => VisitCustomAttrDecl(context);
        public GrammarResult.Literal<EntityRegistry.CustomAttributeEntity?> VisitCustomAttrDecl(CILParser.CustomAttrDeclContext context)
        {
            if (context.dottedName() is { } dottedName)
            {
                // This is a typedef reference for a custom attribute
                string alias = VisitDottedName(dottedName).Value;
                var resolved = TryResolveTypedefAsCustomAttribute(alias);
                if (resolved is not null)
                {
                    var typedefAttribute = _entityRegistry.CreateCustomAttribute(resolved.Value.Constructor, resolved.Value.Value);
                    typedefAttribute.Location = Location.From(context.Start, _documents);
                    return new(typedefAttribute);
                }
                // Typedef not found - could report diagnostic here
                return new(null);
            }
            if (context.customDescrWithOwner() is {} descrWithOwner)
            {
                // Visit the custom attribute descriptor to record it,
                // but don't return it as it will already have its owner recorded.
                _ = VisitCustomDescrWithOwner(descrWithOwner);
                return new(null);
            }
            if (context.customDescr() is {} descr)
            {
#nullable disable // Disable nullability to work around lack of variance.
                return VisitCustomDescr(descr);
#nullable restore
            }
            throw new UnreachableException();
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomDescrInMethodBody(CILParser.CustomDescrInMethodBodyContext context) => VisitCustomDescrInMethodBody(context);
        public GrammarResult.Literal<EntityRegistry.CustomAttributeEntity?> VisitCustomDescrInMethodBody(CILParser.CustomDescrInMethodBodyContext context)
        {
            if (context.customDescrWithOwner() is {} descrWithOwner)
            {
                // Visit the custom attribute descriptor to record it,
                // but don't return it as it will already have its owner recorded.
                _ = VisitCustomDescrWithOwner(descrWithOwner);
                return new(null);
            }
            if (context.customDescr() is {} descr)
            {
#nullable disable // Disable nullability to work around lack of variance.
                return VisitCustomDescr(descr);
#nullable restore
            }
            throw new UnreachableException();
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomBlobArgs(CILParser.CustomBlobArgsContext context) => VisitCustomBlobArgs(context);
        public GrammarResult.FormattedBlob VisitCustomBlobArgs(CILParser.CustomBlobArgsContext context)
        {
            BlobBuilder blob = new();
            foreach (var item in context.serInit())
            {
                VisitSerInit(item).Value.WriteContentTo(blob);
            }
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomBlobDescr(CILParser.CustomBlobDescrContext context) => VisitCustomBlobDescr(context);
        public GrammarResult.FormattedBlob VisitCustomBlobDescr(CILParser.CustomBlobDescrContext context)
        {
            var blob = new BlobBuilder();
            // Custom attribute blob prolog is a 2-byte unsigned integer (ECMA-335 II.23.3)
            blob.WriteUInt16(CustomAttributeBlobFormatVersion);
            VisitCustomBlobArgs(context.customBlobArgs()).Value.WriteContentTo(blob);
            VisitCustomBlobNVPairs(context.customBlobNVPairs()).Value.WriteContentTo(blob);
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomBlobNVPairs(CILParser.CustomBlobNVPairsContext context) => VisitCustomBlobNVPairs(context);
        public GrammarResult.FormattedBlob VisitCustomBlobNVPairs(CILParser.CustomBlobNVPairsContext context)
        {
            var blob = new BlobBuilder();
            var fieldOrProps = context.fieldOrProp();
            var types = context.serializType();
            var names = context.dottedName();
            var values = context.serInit();

            blob.WriteInt16((short)fieldOrProps.Length);

            for (int i = 0; i < fieldOrProps.Length; i++)
            {
                var fieldOrProp = fieldOrProps[i].GetText() == "field" ? CustomAttributeNamedArgumentKind.Field : CustomAttributeNamedArgumentKind.Property;
                var type = VisitSerializType(types[i]).Value;
                var name = VisitDottedName(names[i]).Value;
                var value = VisitSerInit(values[i]).Value;
                blob.WriteByte((byte)fieldOrProp);
                type.WriteContentTo(blob);
                blob.WriteSerializedString(name);
                value.WriteContentTo(blob);
            }
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomDescr(CILParser.CustomDescrContext context) => VisitCustomDescr(context);
        public GrammarResult.Literal<EntityRegistry.CustomAttributeEntity> VisitCustomDescr(CILParser.CustomDescrContext context)
        {
            var ctor = VisitCustomType(context.customType()).Value;
            BlobBuilder value;
            if (context.customBlobDescr() is {} customBlobDescr)
            {
                value = VisitCustomBlobDescr(customBlobDescr).Value;
            }
            else if (context.bytes() is {} bytes)
            {
                value = new();
                value.WriteBytes(VisitBytes(bytes));
            }
            else if (context.compQstring() is {} str)
            {
                value = new();
                value.WriteUTF8(VisitCompQstring(str).Value);
                // COMPAT: We treat this string as a string-reprensentation of a blob,
                // so we don't emit the null terminator.
            }
            else
            {
                value = new();
                value.WriteUInt16(CustomAttributeBlobFormatVersion);
                value.WriteUInt16(0);
            }

            var attribute = _entityRegistry.CreateCustomAttribute(ctor, value);
            attribute.Location = Location.From(context.Start, _documents);
            return new(attribute);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomDescrWithOwner(CILParser.CustomDescrWithOwnerContext context) => VisitCustomDescrWithOwner(context);

        public GrammarResult.Literal<EntityRegistry.CustomAttributeEntity> VisitCustomDescrWithOwner(CILParser.CustomDescrWithOwnerContext context)
        {
            var ctor = VisitCustomType(context.customType()).Value;
            BlobBuilder value;
            if (context.customBlobDescr() is {} customBlobDescr)
            {
                value = VisitCustomBlobDescr(customBlobDescr).Value;
            }
            else if (context.bytes() is {} bytes)
            {
                value = new();
                value.WriteBytes(VisitBytes(bytes));
            }
            else if (context.compQstring() is {} str)
            {
                value = new();
                value.WriteUTF8(VisitCompQstring(str).Value);
                // COMPAT: We treat this string as a string-reprensentation of a blob,
                // so we don't emit the null terminator.
            }
            else
            {
                value = new();
                value.WriteUInt16(CustomAttributeBlobFormatVersion);
                value.WriteUInt16(0);
            }

            var attr = _entityRegistry.CreateCustomAttribute(ctor, value);

            attr.Location = Location.From(context.Start, _documents);
            attr.Owner = VisitOwnerType(context.ownerType()).Value;

            return new(attr);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomType(CILParser.CustomTypeContext context) => VisitCustomType(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitCustomType(CILParser.CustomTypeContext context) => VisitMethodRef(context.methodRef());

        GrammarResult ICILVisitor<GrammarResult>.VisitF32seq(CILParser.F32seqContext context) => VisitF32seq(context);
        public GrammarResult.FormattedBlob VisitF32seq(CILParser.F32seqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<float>(context.ChildCount);

            foreach (var item in context.children ?? [])
            {
                builder.Add((float)(item switch
                {
                    CILParser.Int32Context int32 => VisitInt32(int32).Value,
                    CILParser.Float64Context float64 => VisitFloat64(float64).Value,
                    _ => throw new UnreachableException()
                }));
            }
            return new(builder.ToImmutable().SerializeSequence());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitF64seq(CILParser.F64seqContext context) => VisitF64seq(context);
        public GrammarResult.FormattedBlob VisitF64seq(CILParser.F64seqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<double>(context.ChildCount);

            foreach (var item in context.children ?? [])
            {
                builder.Add((double)(item switch
                {
                    CILParser.Int64Context int64 => VisitInt64(int64).Value,
                    CILParser.Float64Context float64 => VisitFloat64(float64).Value,
                    _ => throw new UnreachableException()
                }));
            }
            return new(builder.ToImmutable().SerializeSequence());
        }

        private static object? ExtractConstantFromSerInit(BlobBuilder blob)
        {
            var bytes = blob.ToImmutableArray();
            if (bytes.Length == 0)
            {
                return null;
            }

            var typeCode = (SerializationTypeCode)bytes[0];
            var valueBytes = bytes.AsSpan().Slice(1);

            return typeCode switch
            {
                SerializationTypeCode.Boolean => valueBytes.Length >= 1 && valueBytes[0] != 0,
                SerializationTypeCode.Char => valueBytes.Length >= 2 ? BitConverter.ToChar(valueBytes) : '\0',
                SerializationTypeCode.SByte => valueBytes.Length >= 1 ? (sbyte)valueBytes[0] : (sbyte)0,
                SerializationTypeCode.Byte => valueBytes.Length >= 1 ? valueBytes[0] : (byte)0,
                SerializationTypeCode.Int16 => valueBytes.Length >= 2 ? BitConverter.ToInt16(valueBytes) : (short)0,
                SerializationTypeCode.UInt16 => valueBytes.Length >= 2 ? BitConverter.ToUInt16(valueBytes) : (ushort)0,
                SerializationTypeCode.Int32 => valueBytes.Length >= 4 ? BitConverter.ToInt32(valueBytes) : 0,
                SerializationTypeCode.UInt32 => valueBytes.Length >= 4 ? BitConverter.ToUInt32(valueBytes) : 0u,
                SerializationTypeCode.Int64 => valueBytes.Length >= 8 ? BitConverter.ToInt64(valueBytes) : 0L,
                SerializationTypeCode.UInt64 => valueBytes.Length >= 8 ? BitConverter.ToUInt64(valueBytes) : 0uL,
                SerializationTypeCode.Single => valueBytes.Length >= 4 ? BitConverter.ToSingle(valueBytes) : 0f,
                SerializationTypeCode.Double => valueBytes.Length >= 8 ? BitConverter.ToDouble(valueBytes) : 0d,
                SerializationTypeCode.String => Encoding.Unicode.GetString(valueBytes),
                // Type is encoded as a SerString (compressed length followed by UTF-8 type name)
                SerializationTypeCode.Type => ExtractSerString(valueBytes),
                // SZArray: element type followed by element count followed by elements
                // Return the raw bytes for arrays since we can't easily represent them
                SerializationTypeCode.SZArray => valueBytes.ToArray(),
                // TaggedObject: type tag followed by value - return raw bytes
                SerializationTypeCode.TaggedObject => valueBytes.ToArray(),
                // Enum: type name (SerString) followed by underlying value - return raw bytes
                SerializationTypeCode.Enum => valueBytes.ToArray(),
                // For unknown/future type codes, return the raw bytes to preserve the data
                _ => bytes.AsSpan().ToArray()
            };
        }

        /// <summary>
        /// Extracts a SerString (compressed length + UTF-8 string) from the given bytes.
        /// Returns null if the first byte is 0xFF (null string marker).
        /// </summary>
        private static string? ExtractSerString(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return null;
            }
            // 0xFF indicates null string
            if (bytes[0] == 0xFF)
            {
                return null;
            }
            // Decode compressed length
            int length;
            int bytesRead;
            if ((bytes[0] & 0x80) == 0)
            {
                // 1-byte length
                length = bytes[0];
                bytesRead = 1;
            }
            else if ((bytes[0] & 0xC0) == 0x80)
            {
                // 2-byte length
                if (bytes.Length < 2) return null;
                length = ((bytes[0] & 0x3F) << 8) | bytes[1];
                bytesRead = 2;
            }
            else
            {
                // 4-byte length
                if (bytes.Length < 4) return null;
                length = ((bytes[0] & 0x1F) << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
                bytesRead = 4;
            }
            if (bytes.Length < bytesRead + length)
            {
                return null;
            }
            return Encoding.UTF8.GetString(bytes.Slice(bytesRead, length));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitFieldSerInit(CILParser.FieldSerInitContext context) => VisitFieldSerInit(context);
        public GrammarResult.FormattedBlob VisitFieldSerInit(CILParser.FieldSerInitContext context)
        {
            // The max length for the majority of the blobs is 9 bytes. 1 for the type of blob, 8 for the max 64-bit value.
            // Byte arrays can be larger, so we handle that case separately.
            const int CommonMaxBlobLength = 9;
            BlobBuilder builder;
            var bytesNode = context.bytes();
            if (bytesNode is not null)
            {
                ImmutableArray<byte> bytesResult = VisitBytes(bytesNode);
                // Our blob length is the number of bytes in the byte array + the code for the byte array.
                builder = new BlobBuilder(bytesResult.Length + 1);
                builder.WriteByte((byte)SerializationTypeCode.String);
                builder.WriteBytes(bytesResult);
                return new(builder);
            }
            builder = new BlobBuilder(CommonMaxBlobLength);

            int tokenType = ((ITerminalNode)context.GetChild(0)).Symbol.Type;

            builder.WriteByte((byte)GetTypeCodeForToken(tokenType));

            switch (tokenType)
            {
                case CILParser.BOOL:
                    builder.WriteBoolean(VisitTruefalse(context.truefalse()).Value);
                    break;
                case CILParser.INT8:
                case CILParser.UINT8:
                    builder.WriteByte((byte)VisitInt32(context.int32()).Value);
                    break;
                case CILParser.CHAR:
                case CILParser.INT16:
                case CILParser.UINT16:
                    builder.WriteInt16((short)VisitInt32(context.int32()).Value);
                    break;
                case CILParser.INT32_:
                case CILParser.UINT32:
                    builder.WriteInt32(VisitInt32(context.int32()).Value);
                    break;
                case CILParser.INT64_:
                case CILParser.UINT64:
                    builder.WriteInt64(VisitInt64(context.int64()).Value);
                    break;
                case CILParser.FLOAT32:
                    {
                        if (context.float64() is CILParser.Float64Context float64)
                        {
                            string text = float64.GetText();
                            if (!text.Contains('.') &&
                                text.IndexOf('e') < 0 &&
                                text.IndexOf('E') < 0 &&
                                ParseIntegerValue(text.AsSpan(), out long rawValue))
                            {
                                builder.WriteSingle(BitConverter.Int32BitsToSingle((int)rawValue));
                            }
                            else
                            {
                                builder.WriteSingle((float)VisitFloat64(float64).Value);
                            }
                        }
                        if (context.int32() is CILParser.Int32Context int32)
                        {
                            int value = VisitInt32(int32).Value;
                            builder.WriteSingle(BitConverter.Int32BitsToSingle(value));
                        }
                        break;
                    }
                case CILParser.FLOAT64_:
                    {
                        if (context.float64() is CILParser.Float64Context float64)
                        {
                            string text = float64.GetText();
                            if (!text.Contains('.') &&
                                text.IndexOf('e') < 0 &&
                                text.IndexOf('E') < 0 &&
                                ParseIntegerValue(text.AsSpan(), out long rawValue))
                            {
                                builder.WriteDouble(BitConverter.Int64BitsToDouble(rawValue));
                            }
                            else
                            {
                                builder.WriteDouble(VisitFloat64(float64).Value);
                            }
                        }
                        if (context.int64() is CILParser.Int64Context int64)
                        {
                            long value = VisitInt64(int64).Value;
                            builder.WriteDouble(BitConverter.Int64BitsToDouble(value));
                        }
                        break;
                    }
            }

            return new(builder);
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitI16seq(CILParser.I16seqContext context) => VisitI16seq(context);
        public GrammarResult.FormattedBlob VisitI16seq(CILParser.I16seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<short>(values.Length);
            foreach (var value in values)
            {
                builder.Add((short)VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitI32seq(CILParser.I32seqContext context) => VisitI32seq(context);
        public GrammarResult.FormattedBlob VisitI32seq(CILParser.I32seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<int>(values.Length);
            foreach (var value in values)
            {
                builder.Add(VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitI8seq(CILParser.I8seqContext context) => VisitI8seq(context);
        public GrammarResult.FormattedBlob VisitI8seq(CILParser.I8seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<byte>(values.Length);
            foreach (var value in values)
            {
                builder.Add((byte)VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitI64seq(CILParser.I64seqContext context) => VisitI64seq(context);
        public GrammarResult.FormattedBlob VisitI64seq(CILParser.I64seqContext context)
        {
            var values = context.int64();
            var builder = ImmutableArray.CreateBuilder<long>(values.Length);
            foreach (var value in values)
            {
                builder.Add(VisitInt64(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitNameValPair(CILParser.NameValPairContext context) => VisitNameValPair(context);
        public GrammarResult.Literal<KeyValuePair<string, BlobBuilder>> VisitNameValPair(CILParser.NameValPairContext context)
        {
            return new(new(VisitCompQstring(context.compQstring()).Value, VisitCaValue(context.caValue()).Value));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitNameValPairs(CILParser.NameValPairsContext context) => VisitNameValPairs(context);
        public GrammarResult.Sequence<KeyValuePair<string, BlobBuilder>> VisitNameValPairs(CILParser.NameValPairsContext context) => new(context.nameValPair().Select(pair => VisitNameValPair(pair).Value).ToImmutableArray());

        GrammarResult ICILVisitor<GrammarResult>.VisitObjSeq(CILParser.ObjSeqContext context) => VisitObjSeq(context);
        public GrammarResult.FormattedBlob VisitObjSeq(CILParser.ObjSeqContext context)
        {
            BlobBuilder objSeqBlob = new();
            foreach (var item in context.serInit())
            {
                // Each element in object[] is encoded as FieldOrPropType + value,
                // where FieldOrPropType is the concrete type (bool, int32, string, etc.),
                // NOT TaggedObject (0x51). The object(...) wrapper is used to explicitly
                // box a value but does not change the element's concrete type in the encoding.
                // Unwrap any object(...) wrappers to get the actual typed inner element.
                CILParser.SerInitContext actualItem = item;
                while (actualItem.serInit() is { } inner)
                {
                    actualItem = inner;
                }
                WriteCustomAttributeFieldOrPropType(objSeqBlob, actualItem);
                objSeqBlob.LinkSuffix(VisitSerInit(actualItem).Value);
            }
            return new(objSeqBlob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitOwnerType(CILParser.OwnerTypeContext context) => VisitOwnerType(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitOwnerType(CILParser.OwnerTypeContext context)
        {
            if (context.memberRef() is CILParser.MemberRefContext memberRef)
            {
                return VisitMemberRef(memberRef);
            }
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                return new(VisitTypeSpec(typeSpec).Value);
            }
            throw new UnreachableException();
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitSerializType(CILParser.SerializTypeContext context) => VisitSerializType(context);
        public GrammarResult.FormattedBlob VisitSerializType(CILParser.SerializTypeContext context)
        {
            var blob = new BlobBuilder();
            if (context.ARRAY_TYPE_NO_BOUNDS() is not null)
            {
                blob.WriteByte((byte)SerializationTypeCode.SZArray);
            }
            VisitSerializTypeElement(context.serializTypeElement()).Value.WriteContentTo(blob);
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSerializTypeElement(CILParser.SerializTypeElementContext context) => VisitSerializTypeElement(context);
        public GrammarResult.FormattedBlob VisitSerializTypeElement(CILParser.SerializTypeElementContext context)
        {
            if (context.simpleType() is CILParser.SimpleTypeContext simpleType)
            {
                BlobBuilder blob = new(1);
                blob.WriteByte((byte)VisitSimpleType(simpleType).Value);
                return new(blob);
            }
            if (context.dottedName() is CILParser.DottedNameContext dottedName)
            {
                // Serialization type typedefs are not yet fully supported
                string alias = VisitDottedName(dottedName).Value;
                ReportError(DiagnosticIds.TypedefNotFound, string.Format(DiagnosticMessageTemplates.TypedefNotFound, alias), context);
                return new(new BlobBuilder(1));
            }
            if (context.TYPE() is not null)
            {
                BlobBuilder blob = new BlobBuilder(1);
                blob.WriteByte((byte)SerializationTypeCode.Type);
                return new(blob);
            }
            if (context.OBJECT() is not null)
            {
                BlobBuilder blob = new BlobBuilder(1);
                blob.WriteByte((byte)SerializationTypeCode.TaggedObject);
                return new(blob);
            }
            if (context.ENUM() is not null)
            {
                BlobBuilder blob = new BlobBuilder();
                blob.WriteByte((byte)SerializationTypeCode.Enum);
                if (context.SQSTRING() is ITerminalNode sqString)
                {
                    blob.WriteSerializedString(StringHelpers.ParseQuotedString(sqString.GetText()));
                }
                else
                {
                    Debug.Assert(context.className() is not null);
                    blob.WriteSerializedString((VisitClassName(context.className()).Value as EntityRegistry.IHasReflectionNotation)?.ReflectionNotation ?? "");
                }
                return new(blob);
            }
            throw new UnreachableException();
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSerInit(CILParser.SerInitContext context) => VisitSerInit(context);
        public GrammarResult.FormattedBlob VisitSerInit(CILParser.SerInitContext context)
        {
            if (context.fieldSerInit() is CILParser.FieldSerInitContext fieldSerInit)
            {
                if (fieldSerInit.bytes() is not null)
                {
                    ReportError(
                        DiagnosticIds.InvalidMetadataToken,
                        "bytearray is not a valid structured custom attribute value",
                        context);
                    var invalidValue = new BlobBuilder();
                    invalidValue.WriteSerializedString(null);
                    return new(invalidValue);
                }

                ImmutableArray<byte> encodedValue = VisitFieldSerInit(fieldSerInit).Value.ToImmutableArray();
                var value = new BlobBuilder(Math.Max(0, encodedValue.Length - 1));
                if (encodedValue.Length > 1)
                {
                    value.WriteBytes(encodedValue.AsSpan().Slice(1).ToArray());
                }
                return new(value);
            }

            if (context.serInit() is CILParser.SerInitContext serInit)
            {
                Debug.Assert(context.OBJECT() is not null);
                BlobBuilder taggedObjectBlob = new();
                WriteCustomAttributeFieldOrPropType(taggedObjectBlob, serInit);
                taggedObjectBlob.LinkSuffix(VisitSerInit(serInit).Value);
                return new(taggedObjectBlob);
            }

            if (context.int32() is not CILParser.Int32Context arrLength)
            {
                BlobBuilder blob = new();
                if (context.className() is CILParser.ClassNameContext className)
                {
                    blob.WriteSerializedString(VisitClassName(className).Value is EntityRegistry.IHasReflectionNotation reflection ? reflection.ReflectionNotation : string.Empty);
                }
                else
                {
                    blob.WriteSerializedString(
                        context.SQSTRING() is { } stringNode
                            ? StringHelpers.ParseQuotedString(stringNode.Symbol.Text)
                            : null);
                }
                return new(blob);
            }

            BlobBuilder arrayHeader = new(sizeof(int));
            arrayHeader.WriteInt32(VisitInt32(arrLength).Value);
            var sequenceResult = (GrammarResult.FormattedBlob)Visit(context.GetRuleContext<ParserRuleContext>(1));
            arrayHeader.LinkSuffix(sequenceResult.Value);
            return new(arrayHeader);
        }

        private static void WriteCustomAttributeFieldOrPropType(
            BlobBuilder builder,
            CILParser.SerInitContext context)
        {
            int tokenType = context.fieldSerInit() is { } fieldSerInit
                ? ((ITerminalNode)fieldSerInit.GetChild(0)).Symbol.Type
                : ((ITerminalNode)context.GetChild(0)).Symbol.Type;
            if (context.fieldSerInit()?.bytes() is not null)
            {
                builder.WriteByte((byte)SerializationTypeCode.String);
                return;
            }
            if (context.int32() is not null)
            {
                builder.WriteByte((byte)SerializationTypeCode.SZArray);
            }

            builder.WriteByte((byte)GetTypeCodeForToken(tokenType));
        }

        private static SerializationTypeCode GetTypeCodeForToken(int tokenType)
        {
            return tokenType switch
            {
                CILParser.INT8 => SerializationTypeCode.SByte,
                CILParser.UINT8 => SerializationTypeCode.Byte,
                CILParser.INT16 => SerializationTypeCode.Int16,
                CILParser.UINT16 => SerializationTypeCode.UInt16,
                CILParser.INT32_ => SerializationTypeCode.Int32,
                CILParser.UINT32 => SerializationTypeCode.UInt32,
                CILParser.INT64_ => SerializationTypeCode.Int64,
                CILParser.UINT64 => SerializationTypeCode.UInt64,
                CILParser.FLOAT32 => SerializationTypeCode.Single,
                CILParser.FLOAT64_ => SerializationTypeCode.Double,
                CILParser.CHAR => SerializationTypeCode.Char,
                CILParser.BOOL => SerializationTypeCode.Boolean,
                CILParser.STRING => SerializationTypeCode.String,
                CILParser.TYPE => SerializationTypeCode.Type,
                CILParser.OBJECT => SerializationTypeCode.TaggedObject,
                _ => throw new UnreachableException()
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSqstringSeq(CILParser.SqstringSeqContext context) => VisitSqstringSeq(context);

        public static GrammarResult.FormattedBlob VisitSqstringSeq(CILParser.SqstringSeqContext context)
        {
            var strings = ImmutableArray.CreateBuilder<string?>(context.ChildCount);
            foreach (var child in context.children ?? [])
            {
                string? str = null;

                if (child is ITerminalNode { Symbol: { Type: CILParser.SQSTRING, Text: string stringValue } })
                {
                    str = StringHelpers.ParseQuotedString(stringValue);
                }

                strings.Add(str);
            }
            return new(strings.MoveToImmutable().SerializeSequence());
        }

    }
}
