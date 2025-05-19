// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include <windows.h>
using DN_CRIT_IMPL = CRITICAL_SECTION;
#else // !TARGET_WINDOWS
#include <pthread.h>
using DN_CRIT_IMPL = pthread_mutex_t;
#endif // TARGET_WINDOWS

struct DN_CRIT_SEC final
{
    DN_CRIT_IMPL _impl;
};

// Initialize the critical section
bool DnCritSec_Initialize(DN_CRIT_SEC* cs);

// Destroy the critical section
void DnCritSec_Destroy(DN_CRIT_SEC* cs);

// Enter the critical section. Blocks until the section can be entered.
void DnCritSec_Enter(DN_CRIT_SEC* cs);

// Leave the critical section
void DnCritSec_Leave(DN_CRIT_SEC* cs);
