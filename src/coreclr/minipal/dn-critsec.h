// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_WINDOWS
#include <synchapi.h>
using CRIT_SEC_IMPL = CRITICAL_SECTION;
#else
#include <pthread.h>
using CRIT_SEC_IMPL = pthread_mutex_t;
#endif

class DnCritSec final
{
    bool _isInitialized = false;;
    CRIT_SEC_IMPL _cs;

public:
    DnCritSec() = default;
    ~DnCritSec() noexcept
    {
        Destroy();
    }

    DnCritSec(DnCritSec const&) = delete;
    DnCritSec& operator=(DnCritSec const&) = delete;
    DnCritSec(DnCritSec&&) noexcept = delete;
    DnCritSec& operator=(DnCritSec&&) noexcept = delete;

public:
    // Initialize the critical section
    bool Initialize();

    // Destroy the critical section
    void Destroy();

    // Enter the critical section. Blocks until the section can be entered.
    void Enter();

    // Leave the critical section
    void Leave();
};