// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_MINIPAL_CRITSECT_H
#define HAVE_MINIPAL_MINIPAL_CRITSECT_H

#include <stdbool.h>

#ifdef HOST_WINDOWS
#include <windows.h>
typedef CRITICAL_SECTION DN_CRITSECT_IMPL;
#else // !HOST_WINDOWS
#include <pthread.h>
typedef pthread_mutex_t DN_CRITSECT_IMPL;
#endif // HOST_WINDOWS

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _DN_CRITSECT
{
    DN_CRITSECT_IMPL _impl;
} DN_CRITSECT;

// Initialize the critical section
bool minipal_critsect_init(DN_CRITSECT* cs);

// Destroy the critical section
void minipal_critsect_destroy(DN_CRITSECT* cs);

// Enter the critical section. Blocks until the section can be entered.
void minipal_critsect_enter(DN_CRITSECT* cs);

// Leave the critical section
void minipal_critsect_leave(DN_CRITSECT* cs);

#ifdef __cplusplus
}
#endif // __cplusplus

#ifdef __cplusplus
class DnCritSectHolder final
{
    DN_CRITSECT* _cs;

public:
    explicit DnCritSectHolder(DN_CRITSECT* cs)
        : _cs{ cs }
    {
        minipal_critsect_enter(_cs);
    }

    ~DnCritSectHolder() noexcept
    {
        minipal_critsect_leave(_cs);
    }

    DnCritSectHolder(DnCritSectHolder const&) = delete;
    DnCritSectHolder& operator=(DnCritSectHolder const&) = delete;

    DnCritSectHolder(DnCritSectHolder&&) = delete;
    DnCritSectHolder& operator=(DnCritSectHolder&&) = delete;
};
#endif // __cplusplus

#endif // HAVE_MINIPAL_MINIPAL_CRITSECT_H
