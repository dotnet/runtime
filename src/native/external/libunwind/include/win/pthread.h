// This is an incomplete & imprecice implementation of the Posix
// standard file by the same name


// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

// #include <sched.h>
#include <sys/types.h>

#define PTHREAD_DESTRUCTOR_ITERATIONS 0
#define PTHREAD_MUTEX_INITIALIZER 0
#define PTHREAD_ONCE_INIT 0

typedef long pthread_key_t;
typedef long pthread_mutex_t;
typedef long pthread_mutexattr_t;
typedef long pthread_once_t;

int          pthread_key_create(pthread_key_t *, void (*)(void*));
int          pthread_mutex_init(pthread_mutex_t *, const pthread_mutexattr_t *);
int          pthread_mutex_lock(pthread_mutex_t *);
int          pthread_mutex_unlock(pthread_mutex_t *);
int          pthread_once(pthread_once_t *, void (*)(void));
int          pthread_setspecific(pthread_key_t, const void *);

#endif // _MSC_VER
