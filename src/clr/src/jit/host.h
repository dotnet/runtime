//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

void logf_stdout(const char* fmt, va_list args);
void logf(const char*, ...);
void logf(unsigned level, const char* fmt, ...);

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
