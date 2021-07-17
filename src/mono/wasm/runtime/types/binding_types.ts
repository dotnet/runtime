// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces that we use. These namespaces are not defined explicitly until
 the dotnet.js file is merged, so we pretend they exist by defining them here.

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/

// VARIOUS C FUNCTIONS THAT WE CALL INTO ////////////////////////////////////////////////////
interface BINDING_C_FUNCS {
    mono_wasm_typed_array_new (a: string, b: string, c: number, d: number): number;
    assembly_load (a: string): number;
    find_corlib_class (a: string, b: string): number;
    find_class (a: number, b: string, c: string): number;
    _find_method (a: number, b: string, c: number): number; 
    invoke_method (a: number, b: number, c: number, d: number): number;
    mono_string_get_utf8 (a: number): number;
    mono_wasm_string_from_utf16 (a: number, b: number): number;
    mono_get_obj_type (a: number): number;
    mono_array_length (a: number): number;
    mono_array_get (a: number, b: number): number;
    mono_obj_array_new (a: number): number;
    mono_obj_array_set (a: number, b: number, c: number): void;
    mono_wasm_register_bundled_satellite_assemblies (): void;
    mono_wasm_try_unbox_primitive_and_get_type (a: number, b: number): number;
    mono_wasm_box_primitive (a: number, b: number, c: number): number;
    mono_wasm_intern_string (a: number): number;
    assembly_get_entry_point (a: number): number;
    mono_wasm_string_array_new (a: number): number;
    mono_wasm_typed_array_new (a: number, b: number, c: number, d: number): number;
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
var BINDING: typeof BindingSupportLib.$BINDING & BINDING_C_FUNCS;
