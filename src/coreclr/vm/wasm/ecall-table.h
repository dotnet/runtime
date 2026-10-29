// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef ECALL_TABLE_H
#define ECALL_TABLE_H

struct ECFunc;

struct ECClass
{
    const char* m_szClassName;
    const char* m_szNameSpace;
    const ECFunc* const* m_pECFunc;
};

extern "C"
{
    extern const ECClass c_rgECClasses[];
    extern const int c_nECClasses;
}

#endif // ECALL_TABLE_H
