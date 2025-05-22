// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CRITSECT_H
#define HAVE_MINIPAL_CRITSECT_H

#include <stdbool.h>

#ifdef HOST_WINDOWS
#include <windows.h>
typedef CRITICAL_SECTION MINIPAL_CRITSECT_IMPL;
#else // !HOST_WINDOWS
#include <pthread.h>
typedef pthread_mutex_t MINIPAL_CRITSECT_IMPL;
#endif // HOST_WINDOWS

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _minipal_critsect
{
    MINIPAL_CRITSECT_IMPL _impl;
} minipal_critsect;

// Initialize the critical section
bool minipal_critsect_init(minipal_critsect* cs);

// Destroy the critical section
void minipal_critsect_destroy(minipal_critsect* cs);

// Enter the critical section. Blocks until the section can be entered.
void minipal_critsect_enter(minipal_critsect* cs);

// Leave the critical section
void minipal_critsect_leave(minipal_critsect* cs);

#ifdef __cplusplus
}
#endif // __cplusplus

#ifdef __cplusplus
class MinipalCritSectHolder final
{
    minipal_critsect* _cs;

public:
    explicit MinipalCritSectHolder(minipal_critsect* cs)
        : _cs{ cs }
    {
        minipal_critsect_enter(_cs);
    }

    ~MinipalCritSectHolder() noexcept
    {
        minipal_critsect_leave(_cs);
    }

    MinipalCritSectHolder(MinipalCritSectHolder const&) = delete;
    MinipalCritSectHolder& operator=(MinipalCritSectHolder const&) = delete;

    MinipalCritSectHolder(MinipalCritSectHolder&&) = delete;
    MinipalCritSectHolder& operator=(MinipalCritSectHolder&&) = delete;
};
#endif // __cplusplus

#endif // HAVE_MINIPAL_CRITSECT_H
