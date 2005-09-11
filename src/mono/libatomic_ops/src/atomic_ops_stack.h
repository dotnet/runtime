/*
 * The implementation of the routines described here is covered by the GPL.
 * This header file is covered by the following license:
 */

/*
 * Copyright (c) 2005 Hewlett-Packard Development Company, L.P.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. 
 */

/* Almost lock-free LIFO linked lists (linked stacks).	*/
#ifndef AO_STACK_H
#define AO_STACK_H

#include "atomic_ops.h"

#if !defined(AO_HAVE_compare_double_and_swap_double) \
    && !defined(AO_HAVE_compare_double_and_swap) \
    && defined(AO_HAVE_compare_and_swap)
# define AO_USE_ALMOST_LOCK_FREE
#else
  /* If we have no compare-and-swap operation defined, we assume	*/
  /* that we will actually be using CAS emulation.  If we do that,	*/
  /* it's cheaper to use the version-based implementation.		*/
# define AO_STACK_IS_LOCK_FREE
#endif

/*
 * These are not guaranteed to be completely lock-free.
 * List insertion may spin under extremely unlikely conditions.
 * It cannot deadlock due to recursive reentry unless AO_list_remove
 * is called while at least AO_BL_SIZE activations of 
 * AO_list_remove are currently active in the same thread, i.e.
 * we must have at least AO_BL_SIZE recursive signal handler
 * invocations.
 *
 * All operations take an AO_list_aux argument.  It is safe to
 * share a single AO_list_aux structure among all lists, but that
 * may increase contention.  Any given list must always be accessed
 * with the same AO_list_aux structure.
 *
 * We make some machine-dependent assumptions:
 *   - We have a compare-and-swap operation.
 *   - At least _AO_N_BITS low order bits in pointers are
 *     zero and normally unused.
 *   - size_t and pointers have the same size.
 *
 * We do use a fully lock-free implementation if double-width
 * compare-and-swap operations are available.
 */

#ifdef AO_USE_ALMOST_LOCK_FREE
/* The number of low order pointer bits we can use for a small	*/
/* version number.						*/
# if defined(__LP64__) || defined(_LP64) || defined(_WIN64)
   /* WIN64 isn't really supported yet.	*/
#  define AO_N_BITS 3
# else
#  define AO_N_BITS 2
# endif

# define AO_BIT_MASK ((1 << AO_N_BITS) - 1)
/*
 * AO_stack_aux should be treated as opaque.
 * It is fully defined here, so it can be allocated, and to facilitate
 * debugging.
 */
#ifndef AO_BL_SIZE
#  define AO_BL_SIZE 2
#endif

#if AO_BL_SIZE > (1 << AO_N_BITS)
#  error AO_BL_SIZE too big
#endif

typedef struct AO__stack_aux {
  volatile AO_t AO_stack_bl[AO_BL_SIZE];
} AO_stack_aux;

/* The stack implementation knows only about the lecation of 	*/
/* link fields in nodes, and nothing about the rest of the 	*/
/* stack elements.  Link fields hold an AO_t, which is not	*/
/* necessarily a real pointer.  This converts the AO_t to a 	*/
/* real (AO_t *) which is either o, or points at the link	*/
/* field in the next node.					*/
#define AO_REAL_NEXT_PTR(x) (AO_t *)((x) & ~AO_BIT_MASK)

/* The following two routines should not normally be used directly.	*/
/* We make them visible here for the rare cases in which it makes sense	*/
/* to share the an AO_stack_aux between stacks.				*/
void
AO_stack_push_explicit_aux_release(volatile AO_t *list, AO_t *x,
	       		          AO_stack_aux *);

AO_t *
AO_stack_pop_explicit_aux_acquire(volatile AO_t *list, AO_stack_aux *);

/* And now AO_stack_t for the real interface:				*/

typedef struct AO__stack {
  volatile AO_t AO_ptr;
  AO_stack_aux AO_aux;
} AO_stack_t;

#define AO_STACK_INITIALIZER {0}

AO_INLINE void AO_stack_init(AO_stack_t *list)
{
# if AO_BL_SIZE == 2
    list -> AO_aux.AO_stack_bl[0] = 0;
    list -> AO_aux.AO_stack_bl[1] = 0;
# else
    int i;
    for (i = 0; i < AO_BL_SIZE; ++i)
      list -> AO_aux.AO_stack_bl[i] = 0;
# endif
  list -> AO_ptr = 0;
}

/* Convert an AO_stack_t to a pointer to the link field in	*/
/* the first element.						*/
#define AO_REAL_HEAD_PTR(x) AO_REAL_NEXT_PTR((x).AO_ptr)

#define AO_stack_push_release(l, e) \
	AO_stack_push_explicit_aux_release(&((l)->AO_ptr), e, &((l)->AO_aux))
#define AO_HAVE_stack_push_release

#define AO_stack_pop_acquire(l) \
	AO_stack_pop_explicit_aux_acquire(&((l)->AO_ptr), &((l)->AO_aux))
#define AO_HAVE_stack_pop_acquire

# else /* Use fully non-blocking data structure, wide CAS	*/

#ifndef AO_HAVE_double_t
  /* Can happen if we're using CAS emulation, since we don't want to	*/
  /* force that here, in case other atomic_ops clients don't want it.	*/
# include "atomic_ops/sysdeps/standard_ao_double_t.h"
#endif

typedef volatile AO_double_t AO_stack_t;
/* AO_val1 is version, AO_val2 is pointer.	*/

#define AO_STACK_INITIALIZER {0}

AO_INLINE void AO_stack_init(AO_stack_t *list)
{
  list -> AO_val1 = 0;
  list -> AO_val2 = 0;
}

#define AO_REAL_HEAD_PTR(x) (AO_t *)((x).AO_val2)
#define AO_REAL_NEXT_PTR(x) (AO_t *)(x)

void AO_stack_push_release(AO_stack_t *list, AO_t *new_element);
#define AO_HAVE_stack_push_release
AO_t * AO_stack_pop_acquire(AO_stack_t *list);
#define AO_HAVE_stack_pop_acquire

#endif /* Wide CAS case */

#if defined(AO_HAVE_stack_push_release) && !defined(AO_HAVE_stack_push)
# define AO_stack_push(l, e) AO_stack_push_release(l, e)
# define AO_HAVE_stack_push
#endif

#if defined(AO_HAVE_stack_pop_acquire) && !defined(AO_HAVE_stack_pop)
# define AO_stack_pop(l) AO_stack_pop_acquire(l)
# define AO_HAVE_stack_pop
#endif

#endif /* !AO_STACK_H */
