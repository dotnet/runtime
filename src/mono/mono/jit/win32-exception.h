#ifndef _MONO_WIN32_EXCEPTION_H_
#define _MONO_WIN32_EXCEPTION_H_

#include <config.h>

#ifdef __cplusplus
extern "C" {
#endif


#ifdef PLATFORM_WIN32

#include <windows.h>

/* use SIG* defines if possible */
#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

/* sigcontext surrogate */
struct sigcontext {
	unsigned int eax;
	unsigned int ebx;
	unsigned int ecx;
	unsigned int edx;
	unsigned int ebp;
	unsigned int esp;
	unsigned int esi;
	unsigned int edi;
	unsigned int eip;
};


typedef void (* MonoW32ExceptionHandler) (int);
void win32_seh_init(void);
void win32_seh_cleanup(void);
void win32_seh_set_handler(int type, MonoW32ExceptionHandler handler);

#ifndef SIGFPE
#define SIGFPE 4
#endif

#ifndef SIGILL
#define SIGILL 8
#endif

#ifndef	SIGSEGV
#define	SIGSEGV 11
#endif

#endif /* PLATFORM_WIN32 */


#ifdef __cplusplus
}
#endif

#endif /* _MONO_WIN32_EXCEPTION_H_ */
