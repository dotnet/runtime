/* -----------------------------------------------------------------------
   ffitest.c - Copyright (c) 1996, 1997, 1998  Cygnus Solutions

   Permission is hereby granted, free of charge, to any person obtaining
   a copy of this software and associated documentation files (the
   ``Software''), to deal in the Software without restriction, including
   without limitation the rights to use, copy, modify, merge, publish,
   distribute, sublicense, and/or sell copies of the Software, and to
   permit persons to whom the Software is furnished to do so, subject to
   the following conditions:

   The above copyright notice and this permission notice shall be included
   in all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
   IN NO EVENT SHALL RED HAT BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */

#include <ffi.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <float.h>

/* This is lame. Long double support is barely there under SunOS 4.x  */
#if defined(__sparc__) && (SIZEOF_LONG_DOUBLE != 16)
#define BROKEN_LONG_DOUBLE
#endif

#define CHECK(x) !(x) ? fail(__FILE__, __LINE__) : 0 

static int fail(char *file, int line)
{
  fprintf(stderr, "Test failure: %s line %d\n", file, line);
  exit(EXIT_FAILURE);
  /*@notreached@*/
  return 0;
}

#define MAX_ARGS 256

static size_t my_strlen(char *s)
{
  return (strlen(s));
}

static int promotion(signed char sc, signed short ss, 
		     unsigned char uc, unsigned short us)
{
  int r = (int) sc + (int) ss + (int) uc + (int) us;

  return r;
}

static signed char return_sc(signed char sc)
{
  return sc;
}

static unsigned char return_uc(unsigned char uc)
{
  return uc;
}

static long long return_ll(long long ll)
{
  return ll;
}

static int floating(int a, float b, double c, long double d, int e)
{
  int i;

#if 0
  /* This is ifdef'd out for now. long double support under SunOS/gcc
     is pretty much non-existent.  You'll get the odd bus error in library
     routines like printf().  */
  printf("%d %f %f %Lf %d\n", a, (double)b, c, d, e);
#endif

  i = (int) ((float)a/b + ((float)c/(float)d));

  return i;
}

static float many(float f1,
		  float f2,
		  float f3,
		  float f4,
		  float f5,
		  float f6,
		  float f7,
		  float f8,
		  float f9,
		  float f10,
		  float f11,
		  float f12,
		  float f13)
{
#if 0
  printf("%f %f %f %f %f %f %f %f %f %f %f %f %f\n",
	 (double) f1, (double) f2, (double) f3, (double) f4, (double) f5, 
	 (double) f6, (double) f7, (double) f8, (double) f9, (double) f10,
	 (double) f11, (double) f12, (double) f13);
#endif

  return ((f1/f2+f3/f4+f5/f6+f7/f8+f9/f10+f11/f12) * f13);
}

static double dblit(float f)
{
  return f/3.0;
}

static long double ldblit(float f)
{
  return (long double) (((long double) f)/ (long double) 3.0);
}

typedef struct
{
  unsigned char uc;
  double d;
  unsigned int ui;
} test_structure_1;

typedef struct
{
  double d1;
  double d2;
} test_structure_2;

typedef struct
{
  int si;
} test_structure_3;

typedef struct
{
  unsigned ui1;
  unsigned ui2;
  unsigned ui3;
} test_structure_4;

typedef struct
{
  char c1;
  char c2;
} test_structure_5;

static test_structure_1 struct1(test_structure_1 ts)
{
  /*@-type@*/
  ts.uc++;
  /*@=type@*/
  ts.d--;
  ts.ui++;

  return ts;
}

static test_structure_2 struct2(test_structure_2 ts)
{
  ts.d1--;
  ts.d2--;

  return ts;
}

static test_structure_3 struct3(test_structure_3 ts)
{
  ts.si = -(ts.si*2);

  return ts;
}

static test_structure_4 struct4(test_structure_4 ts)
{
  ts.ui3 = ts.ui1 * ts.ui2 * ts.ui3;

  return ts;
}

static test_structure_5 struct5(test_structure_5 ts1, test_structure_5 ts2)
{
  ts1.c1 += ts2.c1;
  ts1.c2 -= ts2.c2;

  return ts1;
}

/* Take an int and a float argument, together with int userdata, and 	*/
/* return the sum.							*/
static void closure_test_fn(ffi_cif* cif,void* resp,void** args, void* userdata)
{
    *(int*)resp =
	 *(int *)args[0] + (int)(*(float *)args[1]) + (int)(long)userdata;
}

typedef int (*closure_test_type)(int, float);

int main(/*@unused@*/ int argc, /*@unused@*/ char *argv[])
{
  ffi_cif cif;
  ffi_type *args[MAX_ARGS];
  void *values[MAX_ARGS];
  char *s;
  signed char sc;
  unsigned char uc;
  signed short ss;
  unsigned short us;
  unsigned long ul;
  long long ll;
  float f;
  double d;
  long double ld;
  signed int si1;
  signed int si2;

#if defined(__alpha__) || defined(IA64) || defined(SPARC64) || (defined(__mips__) && (_MIPS_SIM == _ABIN32))
  long long rint;
#else
  int rint;
#endif
  long long rlonglong;

  ffi_type ts1_type;
  ffi_type ts2_type;
  ffi_type ts3_type;
  ffi_type ts4_type;  
  ffi_type ts5_type;
  ffi_type *ts1_type_elements[4];
  ffi_type *ts2_type_elements[3];
  ffi_type *ts3_type_elements[2];
  ffi_type *ts4_type_elements[4];
  ffi_type *ts5_type_elements[3];

  ts1_type.size = 0;
  ts1_type.alignment = 0;
  ts1_type.type = FFI_TYPE_STRUCT;

  ts2_type.size = 0;
  ts2_type.alignment = 0;
  ts2_type.type = FFI_TYPE_STRUCT;

  ts3_type.size = 0;
  ts3_type.alignment = 0;
  ts3_type.type = FFI_TYPE_STRUCT;

  ts4_type.size = 0;
  ts4_type.alignment = 0;
  ts4_type.type = FFI_TYPE_STRUCT;

  ts5_type.size = 0;
  ts5_type.alignment = 0;
  ts5_type.type = FFI_TYPE_STRUCT;

  /*@-immediatetrans@*/
  ts1_type.elements = ts1_type_elements;
  ts2_type.elements = ts2_type_elements;
  ts3_type.elements = ts3_type_elements;
  ts4_type.elements = ts4_type_elements;
  ts5_type.elements = ts5_type_elements;
  /*@=immediatetrans@*/
  
  ts1_type_elements[0] = &ffi_type_uchar;
  ts1_type_elements[1] = &ffi_type_double;
  ts1_type_elements[2] = &ffi_type_uint;
  ts1_type_elements[3] = NULL;
  
  ts2_type_elements[0] = &ffi_type_double;
  ts2_type_elements[1] = &ffi_type_double;
  ts2_type_elements[2] = NULL;

  ts3_type_elements[0] = &ffi_type_sint;
  ts3_type_elements[1] = NULL;

  ts4_type_elements[0] = &ffi_type_uint;
  ts4_type_elements[1] = &ffi_type_uint;
  ts4_type_elements[2] = &ffi_type_uint;
  ts4_type_elements[3] = NULL;

  ts5_type_elements[0] = &ffi_type_schar;
  ts5_type_elements[1] = &ffi_type_schar;
  ts5_type_elements[2] = NULL;

  ul = 0;

  /* return value tests */
  {
#if defined(__mips__) /* || defined(ARM) */
    puts ("long long tests not run. This is a known bug on this architecture.");
#else
    args[0] = &ffi_type_sint64;
    values[0] = &ll;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ffi_type_sint64, args) == FFI_OK);

    for (ll = 0LL; ll < 100LL; ll++)
      {
	ul++;
	ffi_call(&cif, FFI_FN(return_ll), &rlonglong, values);
	CHECK(rlonglong == ll);
      }

    for (ll = 55555555555000LL; ll < 55555555555100LL; ll++)
      {
	ul++;
	ffi_call(&cif, FFI_FN(return_ll), &rlonglong, values);
	CHECK(rlonglong == ll);
      }
#endif

    args[0] = &ffi_type_schar;
    values[0] = &sc;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ffi_type_schar, args) == FFI_OK);

    for (sc = (signed char) -127; 
	 sc < (signed char) 127; /*@-type@*/ sc++ /*@=type@*/)
      {
	ul++;
	ffi_call(&cif, FFI_FN(return_sc), &rint, values);
	CHECK(rint == (int) sc);
      }

    args[0] = &ffi_type_uchar;
    values[0] = &uc;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ffi_type_uchar, args) == FFI_OK);

    for (uc = (unsigned char) '\x00'; 
	 uc < (unsigned char) '\xff'; /*@-type@*/ uc++ /*@=type@*/)
      {
	ul++;
	ffi_call(&cif, FFI_FN(return_uc), &rint, values);
	CHECK(rint == (signed int) uc);
      }

    printf("%lu return value tests run\n", ul);
  }

#ifdef BROKEN_LONG_DOUBLE
  printf ("This architecture has broken `long double' support. No floating point\ntests have been run.\n");
#else
  /* float arg tests */
  {
    args[0] = &ffi_type_float;
    values[0] = &f;

    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ffi_type_longdouble, args) == FFI_OK);

    f = 3.14159;

#if 0
  /* This is ifdef'd out for now. long double support under SunOS/gcc
     is pretty much non-existent.  You'll get the odd bus error in library
     routines like printf().  */
    printf ("%Lf\n", ldblit(f));
#endif
    ld = 666;
    ffi_call(&cif, FFI_FN(ldblit), &ld, values);

#if 0
  /* This is ifdef'd out for now. long double support under SunOS/gcc
     is pretty much non-existent.  You'll get the odd bus error in library
     routines like printf().  */
    printf ("%Lf, %Lf, %Lf, %Lf\n", ld, ldblit(f), ld - ldblit(f), LDBL_EPSILON);
#endif

    /* These are not always the same!! Check for a reasonable delta */
    /*@-realcompare@*/
    if (ld - ldblit(f) < LDBL_EPSILON)
    /*@=realcompare@*/
	puts("long double return value tests ok!");
    else
        CHECK(0);
  }

  /* float arg tests */
  {
    args[0] = &ffi_type_sint;
    values[0] = &si1;
    args[1] = &ffi_type_float;
    values[1] = &f;
    args[2] = &ffi_type_double;
    values[2] = &d;
    args[3] = &ffi_type_longdouble;
    values[3] = &ld;
    args[4] = &ffi_type_sint;
    values[4] = &si2;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 5,
		       &ffi_type_sint, args) == FFI_OK);

    si1 = 6;
    f = 3.14159;
    d = (double)1.0/(double)3.0;
    ld = 2.71828182846L;
    si2 = 10;

    floating (si1, f, d, ld, si2);

    ffi_call(&cif, FFI_FN(floating), &rint, values);

    printf ("%d vs %d\n", rint, floating (si1, f, d, ld, si2));

    CHECK(rint == floating(si1, f, d, ld, si2));

    printf("float arg tests ok!\n");
  }
#endif

  /* strlen tests */
  {
    args[0] = &ffi_type_pointer;
    values[0] = (void*) &s;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ffi_type_sint, args) == FFI_OK);

    s = "a";
    ffi_call(&cif, FFI_FN(my_strlen), &rint, values);
    CHECK(rint == 1);

    s = "1234567";
    ffi_call(&cif, FFI_FN(my_strlen), &rint, values);
    CHECK(rint == 7);

    s = "1234567890123456789012345";
    ffi_call(&cif, FFI_FN(my_strlen), &rint, values);
    CHECK(rint == 25);

    printf("strlen tests passed\n");
  }

  /* float arg tests */
  {
    args[0] = &ffi_type_float;
    values[0] = &f;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ffi_type_double, args) == FFI_OK);

    f = 3.14159;

    ffi_call(&cif, FFI_FN(dblit), &d, values);

    /* These are not always the same!! Check for a reasonable delta */
    /*@-realcompare@*/
    CHECK(d - dblit(f) < DBL_EPSILON);
    /*@=realcompare@*/

    printf("double return value tests ok!\n");
  }

  /* many arg tests */
  {
    float ff;
    float fa[13];
    
    for (ul = 0; ul < 13; ul++)
      {
	args[ul] = &ffi_type_float;
	values[ul] = &fa[ul];
	fa[ul] = (float) ul;
      }

    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 13, 
		       &ffi_type_float, args) == FFI_OK);

    /*@-usedef@*/
    ff =  many(fa[0], fa[1],
	       fa[2], fa[3],
	       fa[4], fa[5],
	       fa[6], fa[7],
	       fa[8], fa[9],
	       fa[10],fa[11],fa[12]);
    /*@=usedef@*/

    ffi_call(&cif, FFI_FN(many), &f, values);

    /*@-realcompare@*/
    if (f - ff < FLT_EPSILON)
    /*@=realcompare@*/
	printf("many arg tests ok!\n");
    else
#ifdef POWERPC
	printf("many arg tests failed!  This is a gcc bug.\n");
#else
        CHECK(0);
#endif
  }

  /* promotion tests */
  {
    args[0] = &ffi_type_schar;
    args[1] = &ffi_type_sshort;
    args[2] = &ffi_type_uchar;
    args[3] = &ffi_type_ushort;
    values[0] = &sc;
    values[1] = &ss;
    values[2] = &uc;
    values[3] = &us;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 4, 
		       &ffi_type_sint, args) == FFI_OK);

    us = 0;
    ul = 0;

    for (sc = (signed char) -127; 
	 sc <= (signed char) 120; /*@-type@*/ sc += 1 /*@=type@*/)
      for (ss = -30000; ss <= 30000; ss += 10000)
	for (uc = (unsigned char) 0; 
	     uc <= (unsigned char) 200; /*@-type@*/ uc += 20 /*@=type@*/)
	  for (us = 0; us <= 60000; us += 10000)
	    {
	      ul++;
	      ffi_call(&cif, FFI_FN(promotion), &rint, values);
	      CHECK(rint == (int) sc + (int) ss + (int) uc + (int) us);
	    }
    printf("%lu promotion tests run\n", ul);
  }

#ifndef X86_WIN32 /* Structures dont work on Win32 */

  /* struct tests */
  {
    test_structure_1 ts1_arg;
    /* This is a hack to get a properly aligned result buffer */
    test_structure_1 *ts1_result = 
      (test_structure_1 *) malloc (sizeof(test_structure_1));

    args[0] = &ts1_type;
    values[0] = &ts1_arg;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ts1_type, args) == FFI_OK);

    ts1_arg.uc = '\x01';
    ts1_arg.d = 3.14159;
    ts1_arg.ui = 555;

    ffi_call(&cif, FFI_FN(struct1), ts1_result, values);

    CHECK(ts1_result->ui == 556);
    CHECK(ts1_result->d == 3.14159 - 1);

    puts ("structure test 1 ok!\n");

    free (ts1_result);
  }

  /* struct tests */
  {
    test_structure_2 ts2_arg;

    /* This is a hack to get a properly aligned result buffer */
    test_structure_2 *ts2_result = 
      (test_structure_2 *) malloc (sizeof(test_structure_2));

    args[0] = &ts2_type;
    values[0] = &ts2_arg;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ts2_type, args) == FFI_OK);

    ts2_arg.d1 = 5.55;
    ts2_arg.d2 = 6.66;

    printf ("%g\n", ts2_result->d1);
    printf ("%g\n", ts2_result->d2);

    ffi_call(&cif, FFI_FN(struct2), ts2_result, values);

    printf ("%g\n", ts2_result->d1);
    printf ("%g\n", ts2_result->d2);
    
    CHECK(ts2_result->d1 == 5.55 - 1);
    CHECK(ts2_result->d2 == 6.66 - 1);

    printf("structure test 2 ok!\n");

    free (ts2_result);
  }

  /* struct tests */
  {
    int compare_value;
    test_structure_3 ts3_arg;
    test_structure_3 *ts3_result = 
      (test_structure_3 *) malloc (sizeof(test_structure_3));

    args[0] = &ts3_type;
    values[0] = &ts3_arg;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ts3_type, args) == FFI_OK);

    ts3_arg.si = -123;
    compare_value = ts3_arg.si;

    ffi_call(&cif, FFI_FN(struct3), ts3_result, values);

    printf ("%d %d\n", ts3_result->si, -(compare_value*2));

    if (ts3_result->si == -(ts3_arg.si*2))
	puts ("structure test 3 ok!");
    else
      {
	puts ("Structure test 3 found structure passing bug.");
	puts ("  Current versions of GCC are not 100% compliant with the");
	puts ("  n32 ABI.  There is a known problem related to passing");
	puts ("  small structures.  Send a bug report to the gcc maintainers.");
      }

    free (ts3_result);
  }

  /* struct tests */
  {
    test_structure_4 ts4_arg;

    /* This is a hack to get a properly aligned result buffer */
    test_structure_4 *ts4_result = 
      (test_structure_4 *) malloc (sizeof(test_structure_4));

    args[0] = &ts4_type;
    values[0] = &ts4_arg;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 1, 
		       &ts4_type, args) == FFI_OK);

    ts4_arg.ui1 = 2;
    ts4_arg.ui2 = 3;
    ts4_arg.ui3 = 4;

    ffi_call (&cif, FFI_FN(struct4), ts4_result, values);
    
    if (ts4_result->ui3 == 2U * 3U * 4U)
      puts ("structure test 4 ok!");
    else
      puts ("Structure test 4 found GCC's structure passing bug.");

    free (ts4_result);
  }

  /* struct tests */
  {
    test_structure_5 ts5_arg1, ts5_arg2;

    /* This is a hack to get a properly aligned result buffer */
    test_structure_5 *ts5_result = 
      (test_structure_5 *) malloc (sizeof(test_structure_5));

    args[0] = &ts5_type;
    args[1] = &ts5_type;
    values[0] = &ts5_arg1;
    values[1] = &ts5_arg2;
    
    /* Initialize the cif */
    CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 2, 
		       &ts5_type, args) == FFI_OK);

    ts5_arg1.c1 = 2;
    ts5_arg1.c2 = 6;
    ts5_arg2.c1 = 5;
    ts5_arg2.c2 = 3;

    ffi_call (&cif, FFI_FN(struct5), ts5_result, values);
    
    if (ts5_result->c1 == 7 
	&& ts5_result->c2 == 3)
      puts ("structure test 5 ok!");
    else
      puts ("Structure test 5 found GCC's structure passing bug.");

    free (ts5_result);
  }

#else
  printf("Structure passing doesn't work on Win32.\n");
#endif /* X86_WIN32 */

# if FFI_CLOSURES
  /* A simple closure test */
    {
      ffi_closure cl;
      ffi_type * cl_arg_types[3];

      cl_arg_types[0] = &ffi_type_sint;
      cl_arg_types[1] = &ffi_type_float;
      cl_arg_types[2] = NULL;
      
      /* Initialize the cif */
      CHECK(ffi_prep_cif(&cif, FFI_DEFAULT_ABI, 2, 
	    	         &ffi_type_sint, cl_arg_types) == FFI_OK);

      CHECK(ffi_prep_closure(&cl, &cif, closure_test_fn,
			     (void *) 3 /* userdata */)
	    == FFI_OK);
      CHECK((*((closure_test_type)(&cl)))(1, 2.0) == 6);
    }
# endif

  /* If we arrived here, all is good */
  (void) puts("\nLooks good. No surprises.\n");

  /*@-compdestroy@*/

  return 0;
}

