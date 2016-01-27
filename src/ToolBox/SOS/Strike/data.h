// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#ifndef __data_h__
#define __data_h__

#include "cor.h"
#include "corhdr.h"
#include "cor.h"
#include "dacprivate.h"

BOOL FileExist (const char *filename);
BOOL FileExist (const WCHAR *filename);

// We use global variables
// because move returns void if it fails
//typedef DWORD DWORD_PTR;
//typedef ULONG ULONG_PTR;

// Max length in WCHAR for a buffer to store metadata name
const int mdNameLen = 2048;
extern WCHAR g_mdName[mdNameLen];

const int nMDIMPORT = 128;
struct MDIMPORT
{
    enum MDType {InMemory, InFile, Dynamic};
    WCHAR *name;
    size_t base;    // base of the PE module
    size_t mdBase;  // base of the metadata
    char *metaData;
    ULONG metaDataSize;
    MDType type;
    IMetaDataImport *pImport;

    MDIMPORT *left;
    MDIMPORT *right;
};

class Module;

extern "C" BOOL ControlC;
extern IMetaDataDispenserEx *pDisp;

#endif // __data_h__
