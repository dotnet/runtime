// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { NativePointer } from "../types";

export interface JSMarshalerArguments extends NativePointer {
    __brand: "JSMarshalerArguments"
}

export interface JSFunctionSignature extends NativePointer {
    __brand: "JSFunctionSignatures"
}

export interface JSMarshalerType extends NativePointer {
    __brand: "JSMarshalerType"
}

export interface JSMarshalerArgument extends NativePointer {
    __brand: "JSMarshalerArgument"
}

export * from "../types";
