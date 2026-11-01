// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header is shared by the interpreter and the VM so the era can be stored on InterpMethod.
#ifndef _AUXILIARYENTRYPOINTSERA_H_
#define _AUXILIARYENTRYPOINTSERA_H_

#ifdef FEATURE_AUXILIARY_R2R_MODULES

#include <stdint.h>

class ReadyToRunInfo;

// The auxiliary EntryPoints era observed by a method whose R2R lookup may have missed because
// auxiliary EntryPoints modules were not yet activated. Only ReadyToRunInfo creates or compares eras.
class AuxiliaryEntryPointsEra
{
    friend class ReadyToRunInfo;

    // 0 means not awaiting EntryPoints; the global era starts at 1.
    uint32_t m_value;

    explicit AuxiliaryEntryPointsEra(uint32_t value) : m_value(value) {}

public:
    AuxiliaryEntryPointsEra() : m_value(0) {}

    bool IsAwaiting() const { return m_value != 0; }
};

#endif // FEATURE_AUXILIARY_R2R_MODULES

#endif // _AUXILIARYENTRYPOINTSERA_H_
