// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _SIGFORMAT_H
#define _SIGFORMAT_H

#include "invokeutil.h"
#include "field.h"

#define SIG_INC 256

class SigFormat
{
public:
    SigFormat();

        //@GENERICS: the owning type handle is required because pMeth may be shared between instantiations
    SigFormat(MethodDesc* pMeth, TypeHandle owner, BOOL fIgnoreMethodName = false);
    SigFormat(MetaSig &metaSig, LPCUTF8 memberName, LPCUTF8 className = NULL, LPCUTF8 ns = NULL);

    ~SigFormat();

    const char * GetCString();
    const char * GetCStringParmsOnly();

    void AddType(TypeHandle th);

protected:
    void FormatSig(MetaSig &metaSig, LPCUTF8 memberName, LPCUTF8 className = NULL, LPCUTF8 ns = NULL);

    char*       _fmtSig;
    size_t      _size;
    size_t      _pos;

    void AddString(LPCUTF8 s);
    void AddTypeString(Module* pModule, SigPointer sig, const SigTypeContext *pTypeContext);

};

#endif // _SIGFORMAT_H
