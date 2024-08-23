#include "libunwind.h"
#include "compiler.h"
#include <sys/types.h>
#include <unistd.h>
#include <string.h>

#include <stdlib.h>

#include <signal.h>
#include <stdio.h>
#include <assert.h>

static const int max_steps = 10;

#if defined __FreeBSD__
#define	TRAMPOLINE_DEPTH	4
#else
#define	TRAMPOLINE_DEPTH	2
#endif

int stepper(unw_cursor_t* c) {
  int steps = 0;
  int ret = 1;
  while (ret) {

    ret = unw_step(c);
    if (!ret) {
      break;
    }
    steps++;
    if (steps > max_steps) break;
  }
  return steps;
}

/* Verify that we can step from both ucontext, and from getcontext()
 * roughly the same.  This tests that the IP from ucontext is used
 * correctly (see impl of unw_init_local2) */
void handler(int num UNUSED, siginfo_t* info UNUSED, void* ucontext) {
  unw_cursor_t c;
  unw_context_t context;
  unw_getcontext(&context);
  int ret = unw_init_local2(&c, ucontext, UNW_INIT_SIGNAL_FRAME);
  assert(!ret);
  int ucontext_steps = stepper(&c);

  ret = unw_init_local(&c, &context);
  (void)ret;
  assert(!ret);
  int getcontext_steps = stepper(&c);
  if (ucontext_steps == getcontext_steps - TRAMPOLINE_DEPTH) {
    exit(0);
  }
  printf("unw_getcontext steps was %i, ucontext steps was %i, should be %i\n",
    getcontext_steps, ucontext_steps, getcontext_steps - TRAMPOLINE_DEPTH);
  exit(-1);
}

int foo(volatile int* f);

int main(){
  struct sigaction a;
  memset(&a, 0, sizeof(struct sigaction));
  a.sa_sigaction = &handler;
  a.sa_flags = SA_SIGINFO;
  sigaction(SIGSEGV, &a, NULL);

  foo(NULL);
  return 0;
}
