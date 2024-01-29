/*
 * Verify that unwinding from a signal handler works when variable width
 * SVE registers are pushed onto the stack
 */

#if defined(__ARM_FEATURE_SVE) && defined(__ARM_FEATURE_SVE_VECTOR_OPERATORS)

#include <arm_sve.h>
#include <libunwind.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <unistd.h>

int64_t z[100];

void signal_handler(int signum)
{
  unw_cursor_t cursor;
  unw_context_t context;

  const char* expected[] = {
    "signal_frame",
    "kill",
    "sum",
    "square",
    "main",
  };

  unw_getcontext(&context);
  unw_init_local(&cursor, &context);

  for (unsigned int depth = 0; depth < sizeof(expected) / sizeof(expected[0]); ++depth)
    {
      unw_word_t offset, pc;
      int unw_rc = unw_step(&cursor);
      if (unw_rc <= 0) {
        printf("Frame: %d  unw_step error: %d\n", depth, unw_rc);
        exit(-1);
      }

      unw_rc = unw_get_reg(&cursor, UNW_REG_IP, &pc);
      if (pc == 0 || unw_rc != 0) {
        printf("Frame: %d  unw_get_reg error: %d\n", depth, unw_rc);
        exit(-1);
      }

      char sym[256];
      unw_rc = unw_is_signal_frame(&cursor);
      if (unw_rc > 0)
        {
          strcpy(sym, "signal_frame");
        }
      else if (unw_rc < 0)
        {
          printf("Frame: %d  unw_is_signal_frame error: %d\n", depth, unw_rc);
          exit(-1);
        }
      else
        {
          unw_rc = unw_get_proc_name(&cursor, sym, sizeof(sym), &offset);
          if (unw_rc)
            {
              printf("Frame: %d  unw_get_proc_name error: %d\n", depth, unw_rc);
              exit(-1);
            }
        }

      if (strcmp(sym, expected[depth]) != 0)
        {
          printf("Frame: %d  expected %s but found %s\n", depth, expected[depth], sym);
          exit(-1);
        }
    }

  exit(0); /* PASS */
}

int64_t sum(svint64_t z0)
{
  int64_t ret = svaddv_s64(svptrue_b64(), z0);
  kill (getpid (), SIGUSR1);
  return ret;
}

int64_t square(svint64_t z0)
{
  int64_t res = 0;
  for (int i = 0; i < 100; ++i)
    {
      z0 = svmul_s64_z(svptrue_b64(), z0, z0);
      res += sum(z0);
    }
  return res;
}

int main()
{
  signal(SIGUSR1, signal_handler);
  for (unsigned int i = 0; i < sizeof(z) / sizeof(z[0]); ++i)
    z[i] = rand();

  svint64_t z0 = svld1(svptrue_b64(), &z[0]);
  square(z0);

  /*
   * Shouldn't get here, exit is called from signal handler
   */
  printf("Signal handler wasn't called\n");
  return -1;
}

#else /* !__ARM_FEATURE_SVE */
int
main ()
{
  return 77; /* SKIP */
}
#endif
