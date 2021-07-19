// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces that we use. These namespaces are not defined explicitly until
 the dotnet.js file is merged, so we pretend they exist by defining them here.

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/

// VARIOUS C FUNCTIONS THAT WE CALL INTO ////////////////////////////////////////////////////
interface DOTNET_C_FUNCS {
    mono_wasm_string_from_js (a: string): number;
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
declare var DOTNET: typeof DotNetSupportLib.$DOTNET & DOTNET_C_FUNCS;
