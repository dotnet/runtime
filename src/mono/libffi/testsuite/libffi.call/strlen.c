#include "ffitest.h"

static size_t my_strlen(char *s)
{
  return (strlen(s));
}

int 
main ()
{
  ffi_cif cif;
  ffi_type *args[1];
  void *values[1];
  int rint __attribute__((aligned(8)));
  char *s;

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
  
  exit (0);
}
  
