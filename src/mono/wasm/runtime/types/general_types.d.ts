// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces that we use. These namespaces are not defined explicitly until
 the dotnet.js file is merged, so we pretend they exist by defining them here.

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/

// EMSDK NON MODULE RELATED /////////////////////////////////////////////////////////////////
declare var ENVIRONMENT_IS_WEB: boolean;
declare var ENVIRONMENT_IS_SHELL: boolean;
declare var ENVIRONMENT_IS_NODE: boolean;
declare var ENVIRONMENT_IS_WORKER: boolean;

declare function locateFile(path: string): string;

// OTHER ////////////////////////////////////////////////////////////////////////////////////
declare function read (path: string): string;
declare function load (path: string): string;
declare function require (path: string): object;
