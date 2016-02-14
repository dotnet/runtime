// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/

#ifdef DEBUG
#ifndef printf
#define printf logf
#endif

class Compiler;
class LogEnv {
public:
    LogEnv(ICorJitInfo* aCompHnd);
    ~LogEnv();
    static LogEnv* cur();           // get current logging environement
    static void cleanup();          // clean up cached information (TLS ID)
    void setCompiler(Compiler* val) { const_cast<Compiler*&>(compiler) = val; }

    ICorJitInfo* const compHnd;
    Compiler* const compiler;
private:
    static int tlsID;
    LogEnv* next;
};

BOOL vlogf(unsigned level, const char* fmt, va_list args);

int logf_stdout(const char* fmt, va_list args);
int logf(const char*, ...);
void gcDump_logf(const char* fmt, ...);

void logf(unsigned level, const char* fmt, ...);

#if defined(CROSSGEN_COMPILE) && !defined(PLATFORM_UNIX) && !defined(fprintf)
// On Windows, CrossGen configures its stdout to allow Unicode output only.
// The following wrapper allows fprintf to work with stdout.
inline int fprintfCrossgen(FILE *stream, const char *fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    int ret = stream == stdout ? logf_stdout(fmt, args) : vfprintf(stream, fmt, args);
    va_end(args);
    return ret;
}
#define fprintf fprintfCrossgen
#endif

extern  "C" 
void    __cdecl     assertAbort(const char *why, const char *file, unsigned line);

#undef  assert
// TODO-ARM64-NYI: Temporarily make all asserts in the JIT use the NYI code path
#ifdef _TARGET_ARM64_
extern void notYetImplemented(const char * msg, const char * file, unsigned line);
#define assert(p)   (void)((p) || (notYetImplemented("assert: " #p, __FILE__, __LINE__),0))
#else
#define assert(p)   (void)((p) || (assertAbort(#p, __FILE__, __LINE__),0))
#endif

#else // DEBUG

#undef  assert
#define assert(p)       (void) 0
#endif // DEBUG

/*****************************************************************************/
#ifndef _HOST_H_
#define _HOST_H_
/*****************************************************************************/

const   size_t      OS_page_size = (4*1024);

/*****************************************************************************/
#endif
/*****************************************************************************/
