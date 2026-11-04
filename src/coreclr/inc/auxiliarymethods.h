// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _AUXILIARYMETHODS_H_
#define _AUXILIARYMETHODS_H_

#ifdef FEATURE_AUXILIARY_R2R_MODULES

#include <stdint.h>

class ReadyToRunInfo;

// An era is the global lookup generation, advanced when an auxiliary EntryPoints module is activated or an image with
// unrelated generic code is registered. A lookup miss records the current era to decide when to retry. ReadyToRunInfo
// creates and compares eras, and to other consumers it is opaque.
class AuxiliaryEntryPointsEra
{
    friend class ReadyToRunInfo;

    // 0 means no lookup-era retry is recorded; the global era starts at 1.
    uint32_t m_value;

    explicit AuxiliaryEntryPointsEra(uint32_t value) : m_value(value) {}

public:
    AuxiliaryEntryPointsEra() : m_value(0) {}

    bool IsAwaiting() const { return m_value != 0; }
};

enum class AuxiliaryMethodStateDiscriminator : uint8_t
{
    // The method has not been looked up yet and has no auxiliary state.
    Uninitialized,
    // The method is known to have no auxiliary code, or auxiliary code failed activation.
    InterpretOnly,
    // The method is known to be in an auxiliary module, but the module is not yet activated.
    CheckAuxiliaryModuleActivationFlag,
    // The method may be in an auxiliary module downloaded later.
    CheckAuxiliaryEntryPointsEra,
    // The method has been successfully activated.
    Activated
};

// Shared by all aliases of an interpreted body. Activated redirects retained bytecode to native code.
struct AuxiliaryMethodState
{
    union
    {
        // When CheckAuxiliaryModuleActivationFlag
#ifdef INTERPRETER_COMPILER_INTERNAL
        uint32_t* auxiliaryModuleActivationFlag;
#else
        PTR_DWORD auxiliaryModuleActivationFlag;
#endif // INTERPRETER_COMPILER_INTERNAL
        // When CheckAuxiliaryEntryPointsEra
        AuxiliaryEntryPointsEra auxiliaryEntryPointsEra;
    };
    AuxiliaryMethodStateDiscriminator discriminator;

    AuxiliaryMethodState() : auxiliaryModuleActivationFlag(nullptr), discriminator(AuxiliaryMethodStateDiscriminator::Uninitialized) {}

    static AuxiliaryMethodState Uninitialized() { return AuxiliaryMethodState(); }
    static AuxiliaryMethodState InterpretOnly()
    {
        AuxiliaryMethodState state;
        state.discriminator = AuxiliaryMethodStateDiscriminator::InterpretOnly;
        return state;
    }
    static AuxiliaryMethodState CheckAuxiliaryModuleActivationFlag(decltype(auxiliaryModuleActivationFlag) flag)
    {
        AuxiliaryMethodState state;
        state.auxiliaryModuleActivationFlag = flag;
        state.discriminator = AuxiliaryMethodStateDiscriminator::CheckAuxiliaryModuleActivationFlag;
        return state;
    }
    static AuxiliaryMethodState CheckAuxiliaryEntryPointsEra(AuxiliaryEntryPointsEra era)
    {
        AuxiliaryMethodState state;
        state.auxiliaryEntryPointsEra = era;
        state.discriminator = AuxiliaryMethodStateDiscriminator::CheckAuxiliaryEntryPointsEra;
        return state;
    }
    static AuxiliaryMethodState Activated()
    {
        AuxiliaryMethodState state;
        state.discriminator = AuxiliaryMethodStateDiscriminator::Activated;
        return state;
    }

    // Defined in the VM's interpexec.h, since it needs ReadyToRunInfo.
    inline bool ShouldResolveNativeCode() const;
};

#endif // FEATURE_AUXILIARY_R2R_MODULES

#endif // _AUXILIARYMETHODS_H_
