#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <stdarg.h>
#include <sys/mman.h>
#include <errno.h>
#include <dirent.h>

int sem_init(int a, int b, int c) { return 0; }
int sem_destroy(int x) { return 0; }
int sem_post(int x) { return 0; }
int sem_wait(int x) { return 0; }
int sem_trywait(int x) { return 0; }
int sem_timedwait (int *sem, const struct timespec *abs_timeout) { assert(0); }

void mono_wasm_link_icu_shim() {}

void schedule_background_exec() { assert(0); }

int getpid() { assert(0); return 0; }

int __errno_location() { return 0; }

int siprintf(char *str, const char *format, ...) {
    va_list myargs;
    va_start(myargs, format);
    int ret = vprintf(format, myargs);
    va_end(myargs);
    return ret;
}
int fiprintf(char *str, const char *format, ...) {
    va_list myargs;
    va_start(myargs, format);
    int ret = vprintf(format, myargs);
    va_end(myargs);
    return ret;
}
int __small_sprintf(char *str, const char *format, ...) {
    va_list myargs;
    va_start(myargs, format);
    int ret = vprintf(format, myargs);
    va_end(myargs);
    return ret;
}
int __small_fprintf(char *str, const char *format, ...) {
    va_list myargs;
    va_start(myargs, format);
    int ret = vprintf(format, myargs);
    va_end(myargs);
    return ret;
}

void udata_setCommonData(int a, int b) { assert(0); }
int u_errorName(int a) { assert(0); return 0; }

int emscripten_stack_get_end() { assert(0); return 0; }
int emscripten_stack_get_base() {assert(0); return 0; }
void mono_set_timeout(int a, int b) { assert(0); }

int __cxa_allocate_exception(int a) { assert(0); return 0; }
void __cxa_throw(int a, int b, int c) { assert(0); }
int __cxa_begin_catch(int a) { assert(0); return 0; }
void __cxa_end_catch() { assert(0); }

int __THREW__;

void invoke_vi(int a, int b) { assert(0); }
int __cxa_find_matching_catch_3(int a) { assert(0); return 0; }
int getTempRet0() { assert(0); return 0; }
int llvm_eh_typeid_for(int a) { assert(0); return 0; }
void __resumeException(int a) { assert(0); }

void mono_log_close_syslog () { assert(0); }
void mono_log_open_syslog (const char *a, void *b) { assert(0); }
void mono_log_write_syslog (const char *a, int b, int c, const char *d) { assert(0); }

void mono_runtime_setup_stat_profiler () { assert(0); }
int mono_thread_state_init_from_handle (int *tctx, int *info, void *sigctx) { assert(0); }
