// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor.Tests.DataModel

open System.Formats.Cbor
open System.Formats.Cbor.Tests.DataModel

/// Randomly generated record containing parameters for a CBOR property-based test
[<CLIMutable>]
type CborPropertyTestContext =
    {
        RootDocuments : CborDocument[]
        ConformanceMode : CborConformanceMode
        ConvertIndefiniteLengthItems : bool
    }

module rec CborPropertyTestContextHelper =

    /// Identifies & transforms documents that might not be accepted under the supplied conformance mode
    let create (mode : CborConformanceMode) (convertIndefiniteLengthItems : bool) (docs : CborDocument[]) =
        {
            RootDocuments = Array.map (normalize mode convertIndefiniteLengthItems) docs
            ConvertIndefiniteLengthItems = convertIndefiniteLengthItems
            ConformanceMode = mode
        }

    /// Gets the expected value that we would expected to see after a full serialization/deserialization roundtrip
    let getExpectedRoundtripValues (context : CborPropertyTestContext) =
        Array.map (getExpectedRoundtripValue context.ConvertIndefiniteLengthItems) context.RootDocuments

    /// Identifies & transforms documents that might not be accepted under the supplied conformance mode
    let private normalize (mode : CborConformanceMode) (convertIndefiniteLengthItems : bool) (doc : CborDocument) =
        // normalization can lead to collisions in conformance modes that require field uniqueness
        // mitigate by wrapping with a unique CBOR node
        let mutable counter = 19000uL
        let createUniqueWrapper (doc : CborDocument) =
            counter <- counter + 1uL
            Array(true, [|UnsignedInteger counter; doc|])

        // not accepted by strict & canonical conformance modes
        let trimNonCanonicalSimpleValue doc =
            match doc with
            | SimpleValue value when value >= 24uy && value < 32uy -> createUniqueWrapper(SimpleValue 0uy)
            | _ -> doc

        // completely replace indefinite-length nodes in canonical conformance modes
        let trimIndefiniteLengthNode doc =
            match doc with
            | ByteStringIndefiniteLength bss -> createUniqueWrapper(ByteString (Array.concat bss))
            | TextStringIndefiniteLength tss -> createUniqueWrapper(TextString (String.concat "" tss))
            | Array(false, elems) -> createUniqueWrapper(Array(true, elems))
            | Map(false, fields) -> createUniqueWrapper(Map(true, fields))
            | _ -> doc

        // CTAP2 does not allow major type 6, at all
        let trimTagNode doc =
            match doc with
            | Tag (tag, doc) -> Array(true, [| UnsignedInteger (uint64 tag) ; doc|])
            | SemanticValue _ -> createUniqueWrapper(UnsignedInteger 0uL)
            | _ -> doc

        match mode with
        | CborConformanceMode.Lax -> doc
        | CborConformanceMode.Strict
        | CborConformanceMode.Canonical -> map (trimNonCanonicalSimpleValue << trimIndefiniteLengthNode) doc
        | CborConformanceMode.Ctap2Canonical -> map (trimNonCanonicalSimpleValue << trimIndefiniteLengthNode << trimTagNode) doc
        | _ -> doc

    /// Gets the expected value that we would expected to see after a full serialization/deserialization roundtrip
    let private getExpectedRoundtripValue (convertIndefiniteLengthItems : bool) (doc : CborDocument) =
        if not convertIndefiniteLengthItems then doc else

        let trimIndefiniteLengthNode doc =
            match doc with
            | ByteStringIndefiniteLength bss -> ByteString (Array.concat bss)
            | TextStringIndefiniteLength tss -> TextString (String.concat "" tss)
            | Array(false, elems) -> Array(true, elems)
            | Map(false, fields) -> Map(true, fields)
            | _ -> doc

        map trimIndefiniteLengthNode doc

    /// Depth-first application of update function on a CBOR doc tree
    let rec private map (updater : CborDocument -> CborDocument) (doc : CborDocument) =
        match updater doc with
        | UnsignedInteger _
        | NegativeInteger _
        | ByteString _
        | ByteStringIndefiniteLength _
        | TextString _
        | TextStringIndefiniteLength _
        | SemanticValue _
        | SimpleValue _
        | Double _ as updated -> updated

        | Tag(tag, value) -> Tag(tag, map updater value)
        | Array(isDefiniteLength, elems) -> Array(isDefiniteLength, Array.map (map updater) elems)
        | Map(isDefiniteLength, fields) ->
            let updatedFields =
                fields
                |> Map.toSeq
                |> Seq.map (fun (k,v) -> map updater k, map updater v)
                |> Map.ofSeq

            Map(isDefiniteLength, updatedFields)
