// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>

#define SYNTHETIC_PTHREAD_KEYS_MAX 32

typedef struct {
    int is_created;
    const void* current_value;
    void* destructor_func;
} synthetic_pthread_key;

// Since we're not really supporting multiple threads, there's just a single global list of pthread keys
// (If we did have real threads, there would be a separate set for each thread)
synthetic_pthread_key synthetic_key_list[SYNTHETIC_PTHREAD_KEYS_MAX];

int pthread_key_create(int *key_index, void* destructor) {
    int i;
    for (i = 0; i < SYNTHETIC_PTHREAD_KEYS_MAX; i++) {
        if (!synthetic_key_list[i].is_created) {
            break;
        }
    }

    if (i == SYNTHETIC_PTHREAD_KEYS_MAX) {
        return -1;
    }

    synthetic_key_list[i].is_created = 1;
    synthetic_key_list[i].destructor_func = destructor; // Not really used since we never destroy keys
    *key_index = i;
    return 0;
}

int pthread_setspecific(int key_index, const void* value) {
    if (!synthetic_key_list[key_index].is_created)
        return -1;

    synthetic_key_list[key_index].current_value = value;
    return 0;
}

const void* pthread_getspecific(int key_index) {
    assert (synthetic_key_list[key_index].is_created);
    return synthetic_key_list[key_index].current_value;
}

// Since we're not really supporting threads, mutex operations are no-ops
// It would be more robust if we actually just aborted if you tried to lock an already-locked mutex
int pthread_mutex_lock(void *mutex) { /*printf("pthread_mutex_lock with mutex=%i\n", mutex);*/ return 0; }
int pthread_mutex_unlock(int *mutex) { /*printf("pthread_mutex_unlock with mutex=%i\n", mutex);*/ return 0; }

// Unused pthread APIs
int pthread_self() { assert(0); return 0; }
int pthread_cond_signal(int a) { assert(0); return 0; }
int pthread_cond_init(int a, int b) { return 0; }
int pthread_cond_wait(int a, int b) { return 0; }
int pthread_cond_destroy(int a) { return 0; }
int pthread_mutex_init(int a, int b) { return 0; }
int pthread_mutex_destroy(int a) { return 0; }
int pthread_cond_timedwait(int a, int b, int c) { return 0; }
