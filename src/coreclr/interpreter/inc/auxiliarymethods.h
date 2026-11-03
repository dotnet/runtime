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
    Uninitialized,
    InterpretOnly,
    CheckAuxiliaryModuleActivationFlag,
    CheckAuxiliaryEntryPointsEra,
    Activated
};

// Caches information to determine whether to reattempt method activation.
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

    bool MayActivateMethod() const
    {
        return discriminator == AuxiliaryMethodStateDiscriminator::CheckAuxiliaryModuleActivationFlag
            || discriminator == AuxiliaryMethodStateDiscriminator::CheckAuxiliaryEntryPointsEra;
    }

    // Defined in the VM's interpexec.h, since it needs ReadyToRunInfo.
    inline bool ShouldTryDelayedActivation() const;
};

enum class AuxiliaryMethodStateRequestType : uint8_t
{
    ActivationRequest,
    ActivationReply
};

// Pending requests allow lookup to delay activation and carry any cached state.
// Resolved requests carry the lookup result.
struct AuxiliaryMethodStateRequest
{
    AuxiliaryMethodState auxMethodState;
    AuxiliaryMethodStateRequestType requestStatus;

    AuxiliaryMethodStateRequest(AuxiliaryMethodState state, AuxiliaryMethodStateRequestType status) : auxMethodState(state), requestStatus(status) {}

public:

    inline AuxiliaryMethodState GetAuxMethodState() const { return auxMethodState; }
    inline bool IsResolved() const { return requestStatus == AuxiliaryMethodStateRequestType::ActivationReply; }
    inline bool IsPending() const { return requestStatus == AuxiliaryMethodStateRequestType::ActivationRequest; }


    inline static AuxiliaryMethodStateRequest Resolved(AuxiliaryMethodState newState)
    {
        return AuxiliaryMethodStateRequest(newState, AuxiliaryMethodStateRequestType::ActivationReply);
    }

    inline static AuxiliaryMethodStateRequest Pending(AuxiliaryMethodState currentState)
    {
        return AuxiliaryMethodStateRequest(currentState, AuxiliaryMethodStateRequestType::ActivationRequest);
    }
};

#endif // FEATURE_AUXILIARY_R2R_MODULES

#endif // _AUXILIARYMETHODS_H_
