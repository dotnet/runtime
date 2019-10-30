// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//*****************************************************************************
// This code supports formatting a method and it's signature in a friendly
// and consistent format.
//
//*****************************************************************************

#ifndef __PrettyPrintSig_h__
#define __PrettyPrintSig_h__

#include <cor.h>

class CQuickBytes;

//
// The return value is either NULL or a null-terminated ASCII string
//

//---------------------------------------------------------------------------------------
//
// Prints a signature in a human readable format.
// No one should use this function.  This exists strictly for backwards compatibility with external formats.
//
// Arguments:
//      sigPtr - Method/field sig to convert
//      sigLen - length of sig
//      name - the name of the method for this sig. Can be L""
//      scratch - scratch buffer to use
//      pIMDI - Import api to use.
//
// Return Value:
//      The formatted string.
//
// Assumptions:
//      None
//
// Notes:
//      Dev's SHOULD NOT create new callers to this function.  Use
//      code:PrettyPrintSig in formatype.h instead.  This function exists for
//      legacy support in the CLR.  There are places that depend on the format
//      of this string.
//

LPCWSTR PrettyPrintSigLegacy(
    PCCOR_SIGNATURE sigPtr,             // Method/field sig to convert
    unsigned    sigLen,                 // length of sig
    LPCWSTR     name,                   // the name of the method for this sig. Can be L""
    CQuickBytes *scratch,               // scratch buffer to use
    IMetaDataImport *pIMDI);            // Import api to use.

struct IMDInternalImport;

//---------------------------------------------------------------------------------------
//
// Prints a signature in a human readable format.
// No one should use this function.  This exists strictly for backwards compatibility with external formats.
//
// Arguments:
//      sigPtr - Method/field sig to convert
//      sigLen - length of sig
//      name - the name of the method for this sig. Can be L""
//      out - The buffer in which to write the pretty printed string.
//      pIMDI - Import api to use.
//
// Return Value:
//      An HRESULT and the formatted string is in out as a unicode string.
//
// Assumptions:
//      None
//
// Notes:
//      Dev's SHOULD NOT create new callers to this function.  Use
//      code:PrettyPrintSig in formattype.h instead.  This function exists for
//      legacy support in the CLR.  There are places that depend on the format
//      of this string.
//
HRESULT PrettyPrintSigInternalLegacy(   // S_OK or error.
    PCCOR_SIGNATURE sigPtr,             // sig to convert,     
    unsigned    sigLen,                 // length of sig
    LPCSTR  name,                       // can be "", the name of the method for this sig
    CQuickBytes *out,                   // where to put the pretty printed string   
    IMDInternalImport *pIMDI);          // Import api to use.

//
// On success, the null-terminated ASCII string is in "out.Ptr()"
//

#endif // __PrettyPrintSig_h__
