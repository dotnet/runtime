// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDAC_H
#define CDAC_H

#include "cdac_reader.h"

class CDACImpl;

class CDAC final
{
public:
    static const CDAC* Create(uint64_t descriptorAddr, ICorDebugDataTarget *pDataTarget);
    virtual ~CDAC();
    CDAC(const CDAC&) = delete;
    CDAC& operator=(const CDAC&) = delete;

    IUnknown* SosInterface() const;

private:
    explicit CDAC(CDACImpl *impl);

private:
    CDACImpl* m_impl;

    friend class CDACImpl;
};

#endif // CDAC_H
