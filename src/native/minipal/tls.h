// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_TLS_H
#define HAVE_MINIPAL_TLS_H

#include <stdbool.h>

#ifdef HOST_WINDOWS
#include <windows.h>
#else
#include <pthread.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*minipal_tls_destructor_t)(void *);

#ifdef HOST_WINDOWS

typedef struct _minipal_tls_key {
    DWORD key;
} minipal_tls_key;

static inline bool minipal_tls_key_create(minipal_tls_key *k, minipal_tls_destructor_t dtor)
{
    k->key = FlsAlloc((PFLS_CALLBACK_FUNCTION)dtor);
    return k->key != FLS_OUT_OF_INDEXES;
}

static inline void minipal_tls_key_delete(minipal_tls_key *k)
{
    if (k->key != FLS_OUT_OF_INDEXES) {
        FlsFree(k->key);
        k->key = FLS_OUT_OF_INDEXES;
    }
}

static inline void * minipal_tls_get(minipal_tls_key *k)
{
    return FlsGetValue(k->key);
}

static inline bool minipal_tls_set(minipal_tls_key *k, void *value)
{
    return FlsSetValue(k->key, value) != 0;
}

#else

typedef struct _minipal_tls_key {
    pthread_key_t key;
} minipal_tls_key;

static inline bool minipal_tls_key_create(minipal_tls_key *k, minipal_tls_destructor_t dtor)
{
    return pthread_key_create(&k->key, dtor) == 0;
}

static inline void minipal_tls_key_delete(minipal_tls_key *k)
{
    pthread_key_delete(k->key);
}

static inline void * minipal_tls_get(minipal_tls_key *k)
{
    return pthread_getspecific(k->key);
}

static inline bool minipal_tls_set(minipal_tls_key *k, void *value)
{
    return pthread_setspecific(k->key, value) == 0;
}

#endif

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_TLS_H
