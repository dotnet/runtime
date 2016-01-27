// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************
 **                                                                         **
 ** Cor.h - general header for the Runtime.                                 **
 **                                                                         **
 *****************************************************************************/


#ifndef _MSCORCFG_H_
#define _MSCORCFG_H_
#include <ole2.h>                       // Definitions of OLE types.    
#include <xmlparser.h>
#include <specstrings.h>

#ifdef __cplusplus
extern "C" {
#endif

// -----------------------------------------------------------------------
// Returns an XMLParsr object. This can be used to parse any XML file.
STDAPI GetXMLElementAttribute(LPCWSTR pwszAttributeName, __out_ecount(cchBuffer) LPWSTR pbuffer, DWORD cchBuffer, DWORD* dwLen);
STDAPI GetXMLElement(LPCWSTR wszFileName, LPCWSTR pwszTag);

STDAPI GetXMLObject(IXMLParser **ppv);
STDAPI CreateConfigStream(LPCWSTR pszFileName, IStream** ppStream);

#ifdef __cplusplus
}
#endif  // __cplusplus

#endif
