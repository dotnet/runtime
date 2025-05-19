// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_MINIPAL_CRITSEC_H
#define HAVE_MINIPAL_MINIPAL_CRITSEC_H

#include <stdbool.h>

#ifdef HOST_WINDOWS
#include <windows.h>
typedef CRITICAL_SECTION DN_CRIT_IMPL;
#else // !HOST_WINDOWS
#include <pthread.h>
typedef pthread_mutex_t DN_CRIT_IMPL;
#endif // HOST_WINDOWS

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _DN_CRIT_SEC
{
    DN_CRIT_IMPL _impl;
} DN_CRIT_SEC;

// Initialize the critical section
bool minipal_critsec_init(DN_CRIT_SEC* cs);

// Destroy the critical section
void minipal_critsec_destroy(DN_CRIT_SEC* cs);

// Enter the critical section. Blocks until the section can be entered.
void minipal_critsec_enter(DN_CRIT_SEC* cs);

// Leave the critical section
void minipal_critsec_leave(DN_CRIT_SEC* cs);

#ifdef __cplusplus
}
#endif // __cplusplus

#ifdef __cplusplus
class DnCritSecHolder final
{
    DN_CRIT_SEC* _cs;

public:
    explicit DnCritSecHolder(DN_CRIT_SEC* cs)
        : _cs{ cs }
    {
        minipal_critsec_enter(_cs);
    }

    ~DnCritSecHolder() noexcept
    {
        minipal_critsec_leave(_cs);
    }

    DnCritSecHolder(DnCritSecHolder const&) = delete;
    DnCritSecHolder& operator=(DnCritSecHolder const&) = delete;

    DnCritSecHolder(DnCritSecHolder&&) = delete;
    DnCritSecHolder& operator=(DnCritSecHolder&&) = delete;
};
#endif // __cplusplus

#endif // HAVE_MINIPAL_MINIPAL_CRITSEC_H
