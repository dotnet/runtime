// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
};

#endif  // _AssemblyName_H

