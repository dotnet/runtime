// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*
     * PREDEFATTR - enum of predefined attributes
     */
    internal enum PREDEFATTR
    {
        PA_ATTRIBUTEUSAGE,
        PA_OBSOLETE,
        PA_CLSCOMPLIANT,
        PA_CONDITIONAL,
        PA_REQUIRED,
        PA_FIXED,
        PA_DEBUGGABLE,
        PA_ASSEMBLYFLAGS,
        PA_ASSEMBLYVERSION,
        PA_ASSEMBLYCULTURE,
        PA_NAME,
        PA_DLLIMPORT,
        PA_COMIMPORT,
        PA_GUID,
        PA_IN,
        PA_OUT,
        PA_STRUCTOFFSET,
        PA_STRUCTLAYOUT,
        PA_PARAMARRAY,
        PA_COCLASS,
        PA_DEFAULTCHARSET,
        PA_DEFAULTVALUE,
        PA_UNMANAGEDFUNCTIONPOINTER,
        PA_COMPILATIONRELAXATIONS,
        PA_RUNTIMECOMPATIBILITY,
        PA_FRIENDASSEMBLY,
        PA_KEYFILE,
        PA_KEYNAME,
        PA_DELAYSIGN,
        PA_DEFAULTMEMBER,
        PA_TYPEFORWARDER,
        PA_EXTENSION,
        PA_COUNT
    }
}
