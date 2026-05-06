// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

module rec System.Formats.Cbor.Tests.DataModel.CborDocumentSerializer

open System
open System.Formats.Cbor
open System.Formats.Cbor.Tests.DataModel

let createWriter (context : CborPropertyTestContext) =
    new CborWriter(
        conformanceMode = context.ConformanceMode,
        convertIndefiniteLengthEncodings = context.ConvertIndefiniteLengthItems,
        allowMultipleRootLevelValues = (context.RootDocuments.Length > 1))

let createReader (context : CborPropertyTestContext) (encoding : byte[]) =
    new CborReader(
        data = ReadOnlyMemory<byte>.op_Implicit encoding,
        conformanceMode = context.ConformanceMode,
        allowMultipleRootLevelValues = (context.RootDocuments.Length > 1))

let encode (context : CborPropertyTestContext) =
    let writer = createWriter context
    for doc in context.RootDocuments do
        write writer doc
    writer.Encode()

let decode (context : CborPropertyTestContext) (data : byte[]) =
    let reader = createReader context data
    let values = new System.Collections.Generic.List<_>()
    while reader.PeekState() <> CborReaderState.Finished do
        values.Add(read reader)
    values.ToArray()

let rec write (writer : CborWriter) (doc : CborDocument) =
    match doc with
    | UnsignedInteger i -> writer.WriteUInt64 i
    | NegativeInteger i -> writer.WriteCborNegativeIntegerRepresentation i
    | ByteString bs -> writer.WriteByteString bs
    | TextString ts -> writer.WriteTextString ts
    | ByteStringIndefiniteLength bss ->
        writer.WriteStartIndefiniteLengthByteString()
        for bs in bss do
            writer.WriteByteString bs
        writer.WriteEndIndefiniteLengthByteString()

    | TextStringIndefiniteLength tss ->
        writer.WriteStartIndefiniteLengthTextString()
        for ts in tss do
            writer.WriteTextString ts
        writer.WriteEndIndefiniteLengthTextString()

    | Array (isDefiniteLength, elements) ->
        writer.WriteStartArray (if isDefiniteLength then Nullable elements.Length else Nullable())
        for elem in elements do
            write writer elem
        writer.WriteEndArray()

    | Map (isDefiniteLength, pairs) ->
        writer.WriteStartMap (if isDefiniteLength then Nullable pairs.Count else Nullable())
        for kv in pairs do
            write writer kv.Key ; write writer kv.Value
        writer.WriteEndMap()

    | Tag (tag, nested) ->
        writer.WriteTag (LanguagePrimitives.EnumOfValue tag)
        write writer nested

    | Double d -> writer.WriteDouble d
    | SimpleValue v -> writer.WriteSimpleValue(LanguagePrimitives.EnumOfValue v)

    | SemanticValue value ->
        writer.WriteTag CborTagExt.SemValueGuard
        writeSemanticValue writer value

let private writeSemanticValue (writer : CborWriter) (s : CborSemanticValue) =
    match s with
    | Null -> writer.WriteTag CborTagExt.Null; writer.WriteNull()
    | Bool b -> writer.WriteTag CborTagExt.Bool; writer.WriteBoolean b
    | Int32 i -> writer.WriteTag CborTagExt.Int32; writer.WriteInt32 i
    | Int64 i -> writer.WriteTag CborTagExt.Int64; writer.WriteInt64 i
    | Half f -> writer.WriteTag CborTagExt.Half; writer.WriteHalf f
    | Single f -> writer.WriteTag CborTagExt.Single; writer.WriteSingle f
    | DateTimeOffset d -> writer.WriteDateTimeOffset d
    | UnixTimeSeconds s -> writer.WriteUnixTimeSeconds s
    | BigInt i -> writer.WriteBigInteger i
    | Decimal d -> writer.WriteDecimal d

let rec read (reader : CborReader) : CborDocument =
    match reader.PeekState() with
    | CborReaderState.UnsignedInteger -> UnsignedInteger(reader.ReadUInt64())
    | CborReaderState.NegativeInteger -> NegativeInteger(reader.ReadCborNegativeIntegerRepresentation())
    | CborReaderState.ByteString -> ByteString(reader.ReadByteString())
    | CborReaderState.TextString -> TextString(reader.ReadTextString())
    | CborReaderState.StartArray ->
        let length = reader.ReadStartArray()
        if length.HasValue then
            let results = Array.zeroCreate<CborDocument> length.Value
            for i = 0 to results.Length - 1 do
                results.[i] <- read reader

            reader.ReadEndArray()
            Array(true, results)
        else
            let results = new System.Collections.Generic.List<_>()
            while reader.PeekState() <> CborReaderState.EndArray do
                results.Add(read reader)

            reader.ReadEndArray()
            Array(false, results.ToArray())

    | CborReaderState.StartIndefiniteLengthByteString ->
        reader.ReadStartIndefiniteLengthByteString()
        let chunks = new System.Collections.Generic.List<byte[]>()
        while reader.PeekState() <> CborReaderState.EndIndefiniteLengthByteString do
            chunks.Add(reader.ReadByteString())
        reader.ReadEndIndefiniteLengthByteString()
        ByteStringIndefiniteLength(chunks.ToArray())

    | CborReaderState.StartIndefiniteLengthTextString ->
        reader.ReadStartIndefiniteLengthTextString()
        let chunks = new System.Collections.Generic.List<string>()
        while reader.PeekState() <> CborReaderState.EndIndefiniteLengthTextString do
            chunks.Add(reader.ReadTextString())
        reader.ReadEndIndefiniteLengthTextString()
        TextStringIndefiniteLength(chunks.ToArray())

    | CborReaderState.StartMap ->
        let length = reader.ReadStartMap()
        if length.HasValue then
            let results = Array.zeroCreate<CborDocument * CborDocument> length.Value
            for i = 0 to results.Length - 1 do
                results.[i] <- (read reader, read reader)
            reader.ReadEndMap()
            Map(true, Map.ofArray results)
        else
            let results = new System.Collections.Generic.List<_>()
            while reader.PeekState() <> CborReaderState.EndMap do
                results.Add(read reader, read reader)
            reader.ReadEndMap()
            Map(false, Map.ofSeq results)

    | CborReaderState.Tag ->
        let tag = reader.ReadTag()
        if tag = CborTagExt.SemValueGuard then SemanticValue(readSemanticValue reader)
        else Tag (LanguagePrimitives.EnumToValue tag, read reader)

    | CborReaderState.HalfPrecisionFloat
    | CborReaderState.SinglePrecisionFloat
    | CborReaderState.DoublePrecisionFloat -> Double (reader.ReadDouble())

    | CborReaderState.Null
    | CborReaderState.Boolean
    | CborReaderState.SimpleValue ->
        let value = reader.ReadSimpleValue()
        SimpleValue(LanguagePrimitives.EnumToValue value)

    | state -> failwithf "Unrecognized reader state %O" state

let private readSemanticValue (reader : CborReader) : CborSemanticValue =
    match reader.PeekTag() with
    | CborTagExt.Null -> let _ = reader.ReadTag() in reader.ReadNull(); Null
    | CborTagExt.Bool -> let _ = reader.ReadTag() in Bool(reader.ReadBoolean())
    | CborTagExt.Int32 -> let _ = reader.ReadTag() in Int32(reader.ReadInt32())
    | CborTagExt.Int64 -> let _ = reader.ReadTag() in Int64(reader.ReadInt64())
    | CborTagExt.Half -> let _ = reader.ReadTag() in Half(reader.ReadHalf())
    | CborTagExt.Single -> let _ = reader.ReadTag() in Single(reader.ReadSingle())
    | CborTag.DateTimeString -> DateTimeOffset(reader.ReadDateTimeOffset())
    | CborTag.UnixTimeSeconds -> let dto = reader.ReadUnixTimeSeconds() in UnixTimeSeconds(dto.ToUnixTimeSeconds())
    | CborTag.UnsignedBigNum
    | CborTag.NegativeBigNum -> BigInt(reader.ReadBigInteger())
    | CborTag.DecimalFraction -> Decimal(reader.ReadDecimal())
    | tag -> failwithf "Unrecognized tag %O" tag

// defines a set of custom CBOR tags for semantic value encodings
module private CborTagExt =

    let [<Literal>] SemValueGuard : CborTag = LanguagePrimitives.EnumOfValue 50015001uL

    let [<Literal>] Null : CborTag = LanguagePrimitives.EnumOfValue 5001uL
    let [<Literal>] Bool : CborTag = LanguagePrimitives.EnumOfValue 5002uL
    let [<Literal>] Int32 : CborTag = LanguagePrimitives.EnumOfValue 5003uL
    let [<Literal>] Int64 : CborTag = LanguagePrimitives.EnumOfValue 5004uL
    let [<Literal>] Half : CborTag = LanguagePrimitives.EnumOfValue 5005uL
    let [<Literal>] Single : CborTag = LanguagePrimitives.EnumOfValue 5006uL
