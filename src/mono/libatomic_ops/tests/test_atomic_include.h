/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: )\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "")

void test_atomic(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop)
    AO_nop();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store)
    AO_store(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load)
    TA_assert(AO_load(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set)
    assert(AO_test_and_set(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set(&z) == AO_TS_SET);
    assert(AO_test_and_set(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add)
    TA_assert(AO_fetch_and_add(&x, 42) == 13);
    TA_assert(AO_fetch_and_add(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1)
    TA_assert(AO_fetch_and_add1(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1)
    TA_assert(AO_fetch_and_sub1(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store)
    AO_short_store(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add)
    TA_assert(AO_short_fetch_and_add(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1)
    TA_assert(AO_short_fetch_and_add1(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1)
    TA_assert(AO_short_fetch_and_sub1(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store)
    AO_char_store(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add)
    TA_assert(AO_char_fetch_and_add(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1)
    TA_assert(AO_char_fetch_and_add1(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1)
    TA_assert(AO_char_fetch_and_sub1(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store)
    AO_int_store(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add)
    TA_assert(AO_int_fetch_and_add(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1)
    TA_assert(AO_int_fetch_and_add1(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1)
    TA_assert(AO_int_fetch_and_sub1(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap)
    TA_assert(!AO_compare_and_swap(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or)
    AO_or(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double)
    TA_assert(!AO_compare_double_and_swap_double(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double)
    TA_assert(!AO_compare_and_swap_double(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: _release)\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "_release")

void test_atomic_release(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set_release)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop_release)
    AO_nop_release();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store_release)
    AO_store_release(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load_release)
    TA_assert(AO_load_release(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set_release)
    assert(AO_test_and_set_release(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set_release(&z) == AO_TS_SET);
    assert(AO_test_and_set_release(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add_release)
    TA_assert(AO_fetch_and_add_release(&x, 42) == 13);
    TA_assert(AO_fetch_and_add_release(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1_release)
    TA_assert(AO_fetch_and_add1_release(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1_release)
    TA_assert(AO_fetch_and_sub1_release(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store_release)
    AO_short_store_release(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load_release)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add_release)
    TA_assert(AO_short_fetch_and_add_release(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add_release(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1_release)
    TA_assert(AO_short_fetch_and_add1_release(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1_release)
    TA_assert(AO_short_fetch_and_sub1_release(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store_release)
    AO_char_store_release(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load_release)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add_release)
    TA_assert(AO_char_fetch_and_add_release(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add_release(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1_release)
    TA_assert(AO_char_fetch_and_add1_release(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1_release)
    TA_assert(AO_char_fetch_and_sub1_release(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store_release)
    AO_int_store_release(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load_release)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add_release)
    TA_assert(AO_int_fetch_and_add_release(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add_release(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1_release)
    TA_assert(AO_int_fetch_and_add1_release(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1_release)
    TA_assert(AO_int_fetch_and_sub1_release(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap_release)
    TA_assert(!AO_compare_and_swap_release(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap_release(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or_release)
    AO_or_release(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double_release)
    TA_assert(!AO_compare_double_and_swap_double_release(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double_release(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double_release(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double_release)
    TA_assert(!AO_compare_and_swap_double_release(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double_release(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double_release(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: _acquire)\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "_acquire")

void test_atomic_acquire(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set_acquire)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop_acquire)
    AO_nop_acquire();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store_acquire)
    AO_store_acquire(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load_acquire)
    TA_assert(AO_load_acquire(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set_acquire)
    assert(AO_test_and_set_acquire(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set_acquire(&z) == AO_TS_SET);
    assert(AO_test_and_set_acquire(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add_acquire)
    TA_assert(AO_fetch_and_add_acquire(&x, 42) == 13);
    TA_assert(AO_fetch_and_add_acquire(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1_acquire)
    TA_assert(AO_fetch_and_add1_acquire(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1_acquire)
    TA_assert(AO_fetch_and_sub1_acquire(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store_acquire)
    AO_short_store_acquire(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load_acquire)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add_acquire)
    TA_assert(AO_short_fetch_and_add_acquire(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add_acquire(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1_acquire)
    TA_assert(AO_short_fetch_and_add1_acquire(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1_acquire)
    TA_assert(AO_short_fetch_and_sub1_acquire(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store_acquire)
    AO_char_store_acquire(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load_acquire)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add_acquire)
    TA_assert(AO_char_fetch_and_add_acquire(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add_acquire(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1_acquire)
    TA_assert(AO_char_fetch_and_add1_acquire(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1_acquire)
    TA_assert(AO_char_fetch_and_sub1_acquire(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store_acquire)
    AO_int_store_acquire(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load_acquire)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add_acquire)
    TA_assert(AO_int_fetch_and_add_acquire(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add_acquire(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1_acquire)
    TA_assert(AO_int_fetch_and_add1_acquire(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1_acquire)
    TA_assert(AO_int_fetch_and_sub1_acquire(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap_acquire)
    TA_assert(!AO_compare_and_swap_acquire(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap_acquire(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or_acquire)
    AO_or_acquire(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double_acquire)
    TA_assert(!AO_compare_double_and_swap_double_acquire(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double_acquire(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double_acquire(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double_acquire)
    TA_assert(!AO_compare_and_swap_double_acquire(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double_acquire(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double_acquire(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: _read)\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "_read")

void test_atomic_read(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set_read)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop_read)
    AO_nop_read();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store_read)
    AO_store_read(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load_read)
    TA_assert(AO_load_read(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set_read)
    assert(AO_test_and_set_read(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set_read(&z) == AO_TS_SET);
    assert(AO_test_and_set_read(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add_read)
    TA_assert(AO_fetch_and_add_read(&x, 42) == 13);
    TA_assert(AO_fetch_and_add_read(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1_read)
    TA_assert(AO_fetch_and_add1_read(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1_read)
    TA_assert(AO_fetch_and_sub1_read(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store_read)
    AO_short_store_read(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load_read)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add_read)
    TA_assert(AO_short_fetch_and_add_read(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add_read(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1_read)
    TA_assert(AO_short_fetch_and_add1_read(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1_read)
    TA_assert(AO_short_fetch_and_sub1_read(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store_read)
    AO_char_store_read(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load_read)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add_read)
    TA_assert(AO_char_fetch_and_add_read(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add_read(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1_read)
    TA_assert(AO_char_fetch_and_add1_read(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1_read)
    TA_assert(AO_char_fetch_and_sub1_read(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store_read)
    AO_int_store_read(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load_read)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add_read)
    TA_assert(AO_int_fetch_and_add_read(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add_read(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1_read)
    TA_assert(AO_int_fetch_and_add1_read(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1_read)
    TA_assert(AO_int_fetch_and_sub1_read(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap_read)
    TA_assert(!AO_compare_and_swap_read(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap_read(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or_read)
    AO_or_read(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double_read)
    TA_assert(!AO_compare_double_and_swap_double_read(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double_read(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double_read(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double_read)
    TA_assert(!AO_compare_and_swap_double_read(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double_read(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double_read(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: _write)\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "_write")

void test_atomic_write(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set_write)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop_write)
    AO_nop_write();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store_write)
    AO_store_write(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load_write)
    TA_assert(AO_load_write(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set_write)
    assert(AO_test_and_set_write(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set_write(&z) == AO_TS_SET);
    assert(AO_test_and_set_write(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add_write)
    TA_assert(AO_fetch_and_add_write(&x, 42) == 13);
    TA_assert(AO_fetch_and_add_write(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1_write)
    TA_assert(AO_fetch_and_add1_write(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1_write)
    TA_assert(AO_fetch_and_sub1_write(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store_write)
    AO_short_store_write(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load_write)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add_write)
    TA_assert(AO_short_fetch_and_add_write(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add_write(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1_write)
    TA_assert(AO_short_fetch_and_add1_write(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1_write)
    TA_assert(AO_short_fetch_and_sub1_write(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store_write)
    AO_char_store_write(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load_write)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add_write)
    TA_assert(AO_char_fetch_and_add_write(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add_write(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1_write)
    TA_assert(AO_char_fetch_and_add1_write(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1_write)
    TA_assert(AO_char_fetch_and_sub1_write(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store_write)
    AO_int_store_write(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load_write)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add_write)
    TA_assert(AO_int_fetch_and_add_write(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add_write(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1_write)
    TA_assert(AO_int_fetch_and_add1_write(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1_write)
    TA_assert(AO_int_fetch_and_sub1_write(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap_write)
    TA_assert(!AO_compare_and_swap_write(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap_write(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or_write)
    AO_or_write(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double_write)
    TA_assert(!AO_compare_double_and_swap_double_write(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double_write(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double_write(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double_write)
    TA_assert(!AO_compare_and_swap_double_write(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double_write(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double_write(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: _full)\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "_full")

void test_atomic_full(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set_full)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop_full)
    AO_nop_full();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store_full)
    AO_store_full(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load_full)
    TA_assert(AO_load_full(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set_full)
    assert(AO_test_and_set_full(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set_full(&z) == AO_TS_SET);
    assert(AO_test_and_set_full(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add_full)
    TA_assert(AO_fetch_and_add_full(&x, 42) == 13);
    TA_assert(AO_fetch_and_add_full(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1_full)
    TA_assert(AO_fetch_and_add1_full(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1_full)
    TA_assert(AO_fetch_and_sub1_full(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store_full)
    AO_short_store_full(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load_full)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add_full)
    TA_assert(AO_short_fetch_and_add_full(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add_full(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1_full)
    TA_assert(AO_short_fetch_and_add1_full(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1_full)
    TA_assert(AO_short_fetch_and_sub1_full(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store_full)
    AO_char_store_full(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load_full)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add_full)
    TA_assert(AO_char_fetch_and_add_full(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add_full(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1_full)
    TA_assert(AO_char_fetch_and_add1_full(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1_full)
    TA_assert(AO_char_fetch_and_sub1_full(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store_full)
    AO_int_store_full(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load_full)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add_full)
    TA_assert(AO_int_fetch_and_add_full(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add_full(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1_full)
    TA_assert(AO_int_fetch_and_add1_full(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1_full)
    TA_assert(AO_int_fetch_and_sub1_full(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap_full)
    TA_assert(!AO_compare_and_swap_full(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap_full(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or_full)
    AO_or_full(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double_full)
    TA_assert(!AO_compare_double_and_swap_double_full(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double_full(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double_full(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double_full)
    TA_assert(!AO_compare_and_swap_double_full(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double_full(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double_full(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: _release_write)\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "_release_write")

void test_atomic_release_write(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set_release_write)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop_release_write)
    AO_nop_release_write();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store_release_write)
    AO_store_release_write(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load_release_write)
    TA_assert(AO_load_release_write(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set_release_write)
    assert(AO_test_and_set_release_write(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set_release_write(&z) == AO_TS_SET);
    assert(AO_test_and_set_release_write(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add_release_write)
    TA_assert(AO_fetch_and_add_release_write(&x, 42) == 13);
    TA_assert(AO_fetch_and_add_release_write(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1_release_write)
    TA_assert(AO_fetch_and_add1_release_write(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1_release_write)
    TA_assert(AO_fetch_and_sub1_release_write(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store_release_write)
    AO_short_store_release_write(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load_release_write)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add_release_write)
    TA_assert(AO_short_fetch_and_add_release_write(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add_release_write(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1_release_write)
    TA_assert(AO_short_fetch_and_add1_release_write(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1_release_write)
    TA_assert(AO_short_fetch_and_sub1_release_write(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store_release_write)
    AO_char_store_release_write(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load_release_write)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add_release_write)
    TA_assert(AO_char_fetch_and_add_release_write(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add_release_write(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1_release_write)
    TA_assert(AO_char_fetch_and_add1_release_write(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1_release_write)
    TA_assert(AO_char_fetch_and_sub1_release_write(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store_release_write)
    AO_int_store_release_write(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load_release_write)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add_release_write)
    TA_assert(AO_int_fetch_and_add_release_write(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add_release_write(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1_release_write)
    TA_assert(AO_int_fetch_and_add1_release_write(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1_release_write)
    TA_assert(AO_int_fetch_and_sub1_release_write(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap_release_write)
    TA_assert(!AO_compare_and_swap_release_write(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap_release_write(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or_release_write)
    AO_or_release_write(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double_release_write)
    TA_assert(!AO_compare_double_and_swap_double_release_write(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double_release_write(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double_release_write(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double_release_write)
    TA_assert(!AO_compare_and_swap_double_release_write(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double_release_write(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double_release_write(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

/* Some basic sanity tests.  These do not test the barrier semantics. */

#undef TA_assert
#define TA_assert(e) \
  if (!(e)) { fprintf(stderr, "Assertion failed %s:%d (barrier: _acquire_read)\n", \
		    __FILE__, __LINE__), exit(1); }

#undef MISSING
#define MISSING(name) \
  fprintf(stderr, "Missing: %s\n", #name "_acquire_read")

void test_atomic_acquire_read(void)
{
  AO_t x;
  unsigned char b;
  unsigned short s;
  unsigned int zz;
# if defined(AO_HAVE_test_and_set_acquire_read)
    AO_TS_t z = AO_TS_INITIALIZER;
# endif
# if defined(AO_HAVE_double_t)
    AO_double_t w;
    w.AO_val1 = 0;
    w.AO_val2 = 0;
# endif

# if defined(AO_HAVE_nop_acquire_read)
    AO_nop_acquire_read();
# else
    MISSING(AO_nop);
# endif
# if defined(AO_HAVE_store_acquire_read)
    AO_store_acquire_read(&x, 13);
    TA_assert (x == 13);
# else
    MISSING(AO_store);
    x = 13;
# endif
# if defined(AO_HAVE_load_acquire_read)
    TA_assert(AO_load_acquire_read(&x) == 13);
# else
    MISSING(AO_load);
# endif
# if defined(AO_HAVE_test_and_set_acquire_read)
    assert(AO_test_and_set_acquire_read(&z) == AO_TS_CLEAR);
    assert(AO_test_and_set_acquire_read(&z) == AO_TS_SET);
    assert(AO_test_and_set_acquire_read(&z) == AO_TS_SET);
    AO_CLEAR(&z);
# else
    MISSING(AO_test_and_set);
# endif
# if defined(AO_HAVE_fetch_and_add_acquire_read)
    TA_assert(AO_fetch_and_add_acquire_read(&x, 42) == 13);
    TA_assert(AO_fetch_and_add_acquire_read(&x, -42) == 55);
# else
    MISSING(AO_fetch_and_add);
# endif
# if defined(AO_HAVE_fetch_and_add1_acquire_read)
    TA_assert(AO_fetch_and_add1_acquire_read(&x) == 13);
# else
    MISSING(AO_fetch_and_add1);
    ++x;
# endif
# if defined(AO_HAVE_fetch_and_sub1_acquire_read)
    TA_assert(AO_fetch_and_sub1_acquire_read(&x) == 14);
# else
    MISSING(AO_fetch_and_sub1);
    --x;
# endif
# if defined(AO_HAVE_short_store_acquire_read)
    AO_short_store_acquire_read(&s, 13);
# else
    MISSING(AO_short_store);
    s = 13;
# endif
# if defined(AO_HAVE_short_load_acquire_read)
    TA_assert(AO_short_load(&s) == 13);
# else
    MISSING(AO_short_load);
# endif
# if defined(AO_HAVE_short_fetch_and_add_acquire_read)
    TA_assert(AO_short_fetch_and_add_acquire_read(&s, 42) == 13);
    TA_assert(AO_short_fetch_and_add_acquire_read(&s, -42) == 55);
# else
    MISSING(AO_short_fetch_and_add);
# endif
# if defined(AO_HAVE_short_fetch_and_add1_acquire_read)
    TA_assert(AO_short_fetch_and_add1_acquire_read(&s) == 13);
# else
    MISSING(AO_short_fetch_and_add1);
    ++s;
# endif
# if defined(AO_HAVE_short_fetch_and_sub1_acquire_read)
    TA_assert(AO_short_fetch_and_sub1_acquire_read(&s) == 14);
# else
    MISSING(AO_short_fetch_and_sub1);
    --s;
# endif
# if defined(AO_HAVE_char_store_acquire_read)
    AO_char_store_acquire_read(&b, 13);
# else
    MISSING(AO_char_store);
    b = 13;
# endif
# if defined(AO_HAVE_char_load_acquire_read)
    TA_assert(AO_char_load(&b) == 13);
# else
    MISSING(AO_char_load);
# endif
# if defined(AO_HAVE_char_fetch_and_add_acquire_read)
    TA_assert(AO_char_fetch_and_add_acquire_read(&b, 42) == 13);
    TA_assert(AO_char_fetch_and_add_acquire_read(&b, -42) == 55);
# else
    MISSING(AO_char_fetch_and_add);
# endif
# if defined(AO_HAVE_char_fetch_and_add1_acquire_read)
    TA_assert(AO_char_fetch_and_add1_acquire_read(&b) == 13);
# else
    MISSING(AO_char_fetch_and_add1);
    ++b;
# endif
# if defined(AO_HAVE_char_fetch_and_sub1_acquire_read)
    TA_assert(AO_char_fetch_and_sub1_acquire_read(&b) == 14);
# else
    MISSING(AO_char_fetch_and_sub1);
    --b;
# endif
# if defined(AO_HAVE_int_store_acquire_read)
    AO_int_store_acquire_read(&zz, 13);
# else
    MISSING(AO_int_store);
    zz = 13;
# endif
# if defined(AO_HAVE_int_load_acquire_read)
    TA_assert(AO_int_load(&zz) == 13);
# else
    MISSING(AO_int_load);
# endif
# if defined(AO_HAVE_int_fetch_and_add_acquire_read)
    TA_assert(AO_int_fetch_and_add_acquire_read(&zz, 42) == 13);
    TA_assert(AO_int_fetch_and_add_acquire_read(&zz, -42) == 55);
# else
    MISSING(AO_int_fetch_and_add);
# endif
# if defined(AO_HAVE_int_fetch_and_add1_acquire_read)
    TA_assert(AO_int_fetch_and_add1_acquire_read(&zz) == 13);
# else
    MISSING(AO_int_fetch_and_add1);
    ++zz;
# endif
# if defined(AO_HAVE_int_fetch_and_sub1_acquire_read)
    TA_assert(AO_int_fetch_and_sub1_acquire_read(&zz) == 14);
# else
    MISSING(AO_int_fetch_and_sub1);
    --zz;
# endif
# if defined(AO_HAVE_compare_and_swap_acquire_read)
    TA_assert(!AO_compare_and_swap_acquire_read(&x, 14, 42));
    TA_assert(x == 13);
    TA_assert(AO_compare_and_swap_acquire_read(&x, 13, 42));
    TA_assert(x == 42);
# else
    MISSING(AO_compare_and_swap);
# endif
# if defined(AO_HAVE_or_acquire_read)
    AO_or_acquire_read(&x, 66);
    TA_assert(x == 106);
# else
    MISSING(AO_or);
    x |= 34;
# endif
# if defined(AO_HAVE_compare_double_and_swap_double_acquire_read)
    TA_assert(!AO_compare_double_and_swap_double_acquire_read(&w, 17, 42, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_double_and_swap_double_acquire_read(&w, 0, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_double_and_swap_double_acquire_read(&w, 12, 13, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
    w.AO_val1 = 0; w.AO_val2 = 0;
# else
    MISSING(AO_compare_double_and_swap_double);
# endif
# if defined(AO_HAVE_compare_and_swap_double_acquire_read)
    TA_assert(!AO_compare_and_swap_double_acquire_read(&w, 17, 12, 13));
    TA_assert(w.AO_val1 == 0 && w.AO_val2 == 0);
    TA_assert(AO_compare_and_swap_double_acquire_read(&w, 0, 12, 13));
    TA_assert(w.AO_val1 == 12 && w.AO_val2 == 13);
    TA_assert(AO_compare_and_swap_double_acquire_read(&w, 12, 17, 42));
    TA_assert(w.AO_val1 == 17 && w.AO_val2 == 42);
# else
    MISSING(AO_compare_and_swap_double);
# endif
}


    
