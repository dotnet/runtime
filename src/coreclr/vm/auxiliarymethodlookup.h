// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _AUXILIARYMETHODLOOKUP_H_
#define _AUXILIARYMETHODLOOKUP_H_

#ifdef FEATURE_AUXILIARY_R2R_MODULES

#include "auxiliarymethods.h"

enum class AuxiliaryMethodStateLookupType : uint8_t
{
    LookupRequest,
    LookupResponse,
    // For simulated delays, force a lookup
    ForceLookup
};

// Pending lookups carry the current state of the method.
// Resolved lookups carry the lookup result to the requester.
struct AuxiliaryMethodLookupState
{
    AuxiliaryMethodState auxMethodState;
    AuxiliaryMethodStateLookupType lookupStatus;

    AuxiliaryMethodLookupState(AuxiliaryMethodState state, AuxiliaryMethodStateLookupType status) : auxMethodState(state), lookupStatus(status) { }

public:
    inline AuxiliaryMethodState GetAuxMethodState() const { return auxMethodState; }
    inline bool IsResolved() const { return lookupStatus == AuxiliaryMethodStateLookupType::LookupResponse; }
    inline bool IsPending() const { return lookupStatus == AuxiliaryMethodStateLookupType::LookupRequest; }
    inline bool CanDelay() const { return lookupStatus != AuxiliaryMethodStateLookupType::ForceLookup; }

    inline static AuxiliaryMethodLookupState Resolved(AuxiliaryMethodState newState)
    {
        _ASSERTE(newState.discriminator != AuxiliaryMethodStateDiscriminator::Uninitialized);
        return AuxiliaryMethodLookupState(newState, AuxiliaryMethodStateLookupType::LookupResponse);
    }

    inline static AuxiliaryMethodLookupState Pending(AuxiliaryMethodState currentState)
    {
        _ASSERTE(currentState.discriminator != AuxiliaryMethodStateDiscriminator::Activated);
        return AuxiliaryMethodLookupState(currentState, AuxiliaryMethodStateLookupType::LookupRequest);
    }

    inline static AuxiliaryMethodLookupState ForceLookup()
    {
        return AuxiliaryMethodLookupState(AuxiliaryMethodState::Uninitialized(), AuxiliaryMethodStateLookupType::ForceLookup);
    }
};

#endif // FEATURE_AUXILIARY_R2R_MODULES

#endif // _AUXILIARYMETHODLOOKUP_H_
