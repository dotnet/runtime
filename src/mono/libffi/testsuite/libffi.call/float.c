#include "ffitest.h"

static int floating(int a, float b, double c, long double d, int e)
{
  int i;

  i = (int) ((float)a/b + ((float)c/(float)d));

  return i;
}

int 
main ()
{
  ffi_cif cif;
  ffi_type *args[5];
  void *values[5];
  int si1, si2;
  float f;
  double d;
  long double ld;
  int rint __attribute__((aligned(8)));

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
  
  exit (0);
}
  
