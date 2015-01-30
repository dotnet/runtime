//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*============================================================
**
** Header:  AssemblyName.hpp
**
** Purpose: Implements AssemblyName (loader domain) architecture
**
**


**
===========================================================*/
#ifndef _AssemblyName_H
#define _AssemblyName_H

class AssemblyNameNative
{
public:    
    static FCDECL1(Object*, GetFileInformation, StringObject* filenameUNSAFE);
    static FCDECL1(Object*, ToString, Object* refThisUNSAFE);
    static FCDECL1(Object*, GetPublicKeyToken, Object* refThisUNSAFE);
    static FCDECL1(Object*, EscapeCodeBase, StringObject* filenameUNSAFE);
    static FCDECL4(void, Init, Object * refThisUNSAFE, OBJECTREF * pAssemblyRef, CLR_BOOL fForIntrospection, CLR_BOOL fRaiseResolveEvent);
    static FCDECL3(FC_BOOL_RET, ReferenceMatchesDefinition, AssemblyNameBaseObject* refUNSAFE, AssemblyNameBaseObject* defUNSAFE, CLR_BOOL fParse);
};

#endif  // _AssemblyName_H

