/* FIXME.  This is only a placeholder for the AIX compiler.  		*/
/* It doesn't work.  Please send a patch.				*/
/* Memory model documented at http://www-106.ibm.com/developerworks/	*/
/* eserver/articles/archguide.html and (clearer)			*/
/* http://www-106.ibm.com/developerworks/eserver/articles/powerpc.html. */
/* There appears to be no implicit ordering between any kind of		*/
/* independent memory references.					*/
/* Architecture enforces some ordering based on control dependence.	*/
/* I don't know if that could help. 					*/
/* Data-dependent loads are always ordered.				*/
/* Based on the above references, eieio is intended for use on		*/
/* uncached memory, which we don't support.  It does not order loads	*/
/* from cached memory.							*/
/* Thanks to Maged Michael, Doug Lea, and Roger Hoover for helping to 	*/
/* track some of this down and correcting my misunderstandings. -HB	*/

#include "../all_aligned_atomic_load_store.h"

void AO_sync(void);
#pragma mc_func AO_sync { "7c0004ac" }

void AO_lwsync(void);
#pragma mc_func AO_lwsync { "7c2004ac" }

#define AO_nop_write() AO_lwsync()
#define AO_HAVE_nop_write

#define AO_nop_read() AO_lwsync()
#define AO_HAVE_nop_read

/* We explicitly specify load_acquire and store_release, since these	*/
/* rely on the fact that lwsync is also a LoadStore barrier.		*/
AO_INLINE AO_t
AO_load_acquire(volatile AO_t *addr)
{
  AO_t result = *addr;
  AO_lwsync();
  return result;
}

#define AO_HAVE_load_acquire

AO_INLINE void
AO_store_release(volatile AO_t *addr, AO_t value)
{
  AO_lwsync();
  *addr = value;
}

#define AO_HAVE_load_acquire

/* This is similar to the code in the garbage collector.  Deleting 	*/
/* this and having it synthesized from compare_and_swap would probably	*/
/* only cost us a load immediate instruction.				*/
AO_INLINE AO_TS_VAL_t
AO_test_and_set(volatile AO_TS_t *addr) {
# error Implement me
}

#define AO_have_test_and_set

AO_INLINE AO_TS_VAL_t
AO_test_and_set_acquire(volatile AO_TS_t *addr) {
  AO_TS_VAL_t result = AO_test_and_set(addr);
  AO_lwsync();
  return result;
}

#define AO_HAVE_test_and_set_acquire

AO_INLINE AO_TS_VAL_t
AO_test_and_set_release(volatile AO_TS_t *addr) {
  AO_lwsync();
  return AO_test_and_set(addr);
}

#define AO_HAVE_test_and_set_release

AO_INLINE AO_TS_VAL_t
AO_test_and_set_full(volatile AO_TS_t *addr) {
  AO_TS_VAL_t result;
  AO_lwsync();
  result = AO_test_and_set(addr);
  AO_lwsync();
  return result;
}

#define AO_HAVE_test_and_set_full

AO_INLINE AO_t
AO_compare_and_swap(volatile AO_t *addr, AO_t old, AO_t new_val) {
# error Implement me
}

#define AO_HAVE_compare_and_swap

AO_INLINE AO_t
AO_compare_and_swap_acquire(volatile AO_t *addr, AO_t old, AO_t new_val) {
  AO_t result = AO_compare_and_swap(addr, old, new_val);
  AO_lwsync();
  return result;
}

#define AO_HAVE_compare_and_swap_acquire

AO_INLINE AO_t
AO_compare_and_swap_release(volatile AO_t *addr, AO_t old, AO_t new_val) {
  AO_lwsync();
  return AO_compare_and_swap(addr, old, new_val);
}

#define AO_HAVE_compare_and_swap_release

AO_INLINE AO_t
AO_compare_and_swap_full(volatile AO_t *addr, AO_t old, AO_t new_val) {
  AO_t result;
  AO_lwsync();
  result = AO_compare_and_swap(addr, old, new_val);
  AO_lwsync();
  return result;
}

#define AO_HAVE_compare_and_swap_full

/* FIXME: We should also implement fetch_and_add and or primitives	*/
/* directly.								*/
