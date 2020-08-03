// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor.Tests.DataModel

open System

// --
// Models a CBOR document type using a discriminated union.
// Discriminated unions have structural equality for free
// and random instances can be generated using FsCheck.
//
// The type and serialization helpers are used by the
// property tests in the main test project.
//

type CborDocument =
    // Major type 0
    | UnsignedInteger of uint64
    // Major type 1
    | NegativeInteger of uint64
    // Major type 2
    | ByteString of byte[]
    | ByteStringIndefiniteLength of byte[][]
    // Major type 3
    | TextString of string
    | TextStringIndefiniteLength of string[]
    // Major type 4
    | Array of isDefiniteLength:bool * CborDocument[]
    // Major type 5
    | Map of isDefiniteLength:bool * Map<CborDocument, CborDocument>
    // Major type 6
    | Tag of tag:uint64 * CborDocument
    | SemanticValue of CborSemanticValue
    // Major type 7
    | Double of double
    | SimpleValue of value:byte

// Defines a set of semantic values drawing either from the CBOR spec
// or invented here for the purposes of this test.
and CborSemanticValue =
    | Null
    | Bool of bool
    | Int32 of int
    | Int64 of int64
    | Half of Half
    | Single of single
    | DateTimeOffset of DateTimeOffset
    | UnixTimeSeconds of seconds:int64
    | BigInt of bigint
    | Decimal of decimal
