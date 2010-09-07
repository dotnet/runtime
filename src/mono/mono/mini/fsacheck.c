#include <stdio.h>
#include <stdlib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>
#include <mono/jit/jit.h>

extern void* mono_aot_module_mscorlib_info;
extern void* mono_aot_module_System_Core_info;
extern void* mono_aot_module_System_info;
extern void* mono_aot_module_Mono_Posix_info;
extern void* mono_aot_module_System_Configuration_info;
extern void* mono_aot_module_System_Security_info;
extern void* mono_aot_module_System_Xml_info;
/* extern void* mono_aot_module_System_Threading_info; */
extern void* mono_aot_module_Mono_Security_info;
extern void* mono_aot_module_Mono_Simd_info;
extern void* mono_aot_module_TestDriver_info;

extern void* mono_aot_module_basic_info;
extern void* mono_aot_module_basic_float_info;
extern void* mono_aot_module_basic_long_info;
extern void* mono_aot_module_basic_calls_info;
extern void* mono_aot_module_basic_simd_info;
extern void* mono_aot_module_objects_info;
extern void* mono_aot_module_arrays_info;
extern void* mono_aot_module_basic_math_info;
extern void* mono_aot_module_exceptions_info;
extern void* mono_aot_module_devirtualization_info;
extern void* mono_aot_module_generics_info;
extern void* mono_aot_module_generics_variant_types_info;
extern void* mono_aot_module_basic_simd_info;
/* extern void* mono_aot_module_thread_stress_info; */


extern void mono_aot_register_module(void *aot_info);
extern void mono_aot_init(void);
extern void mono_jit_set_aot_only(mono_bool aot_only);
extern MonoDomain * mini_init (const char *filename, const char *runtime_version);


void try_one(char *mname) {
  MonoDomain *domain;
  MonoAssembly *ma;
  MonoImage *mi;
  MonoClass *mc;
  MonoMethodDesc *mmd;
  MonoMethod *mm;
  MonoObject *mo;
  MonoArray *arg_array;
  void *args [1];
  char *cstr_arg = "20";

  mono_jit_set_aot_only(1);
  domain = mono_jit_init(mname);
  printf("mono domain: %p\n", domain);

  ma = mono_domain_assembly_open(domain, mname);
  if (0 == ma) {
    printf("ERROR: could not open mono assembly\n");
    exit(-1);
  }
  printf("opened mono assembly: %p\n", ma);

  mi = mono_assembly_get_image(ma);
  printf("mono image: %p\n", mi);

  mo = mono_string_new(domain, cstr_arg);
  mc = mono_class_from_name(mono_get_corlib(), "System", "String");
  printf("string class: %p\n", mc);
  arg_array = mono_array_new(domain, mc, 1);
  mono_array_setref(arg_array, 0, mo);
  args[0] = arg_array;

  mmd = mono_method_desc_new("Tests:Main()", 1);
  mm = mono_method_desc_search_in_image(mmd, mi);
  if (0 == mm) {
    mmd = mono_method_desc_new("Tests:Main(string[])", 1);
    mm = mono_method_desc_search_in_image(mmd, mi);
    if (0 == mm) {
      mmd = mono_method_desc_new("SimdTests:Main(string[])", 1);
      mm = mono_method_desc_search_in_image(mmd, mi);
      if (0 == mm) {
        printf("Couldn't find Tests:Main(), Tests:Main(string[]) or SimdTests:Main(string[])\n");
        exit(-1);
      }
    }
  }
  printf("mono desc method: %p\n", mmd);
  printf("mono method: %p\n", mm);

  mo = mono_runtime_invoke(mm, NULL, args, NULL);
  printf("mono object: %p\n", mo);

  mono_jit_cleanup(domain);
}

int main(int argc, char *argv[]) {
  mono_aot_register_module(mono_aot_module_mscorlib_info);
  mono_aot_register_module(mono_aot_module_TestDriver_info);
  mono_aot_register_module(mono_aot_module_System_Core_info);
  mono_aot_register_module(mono_aot_module_System_info);
  mono_aot_register_module(mono_aot_module_Mono_Posix_info);
  mono_aot_register_module(mono_aot_module_System_Configuration_info);
  mono_aot_register_module(mono_aot_module_System_Security_info);
  mono_aot_register_module(mono_aot_module_System_Xml_info);
  mono_aot_register_module(mono_aot_module_Mono_Security_info);
  /*  mono_aot_register_module(mono_aot_module_System_Threading_info); */
  mono_aot_register_module(mono_aot_module_Mono_Simd_info);

  mono_aot_register_module(mono_aot_module_basic_info);
  mono_aot_register_module(mono_aot_module_basic_float_info);
  mono_aot_register_module(mono_aot_module_basic_long_info);
  mono_aot_register_module(mono_aot_module_basic_calls_info);
  mono_aot_register_module(mono_aot_module_basic_simd_info);
  mono_aot_register_module(mono_aot_module_objects_info);
  mono_aot_register_module(mono_aot_module_arrays_info);
  mono_aot_register_module(mono_aot_module_basic_math_info);
  mono_aot_register_module(mono_aot_module_exceptions_info);
  mono_aot_register_module(mono_aot_module_devirtualization_info);
  /*
  mono_aot_register_module(mono_aot_module_generics_info);
  mono_aot_register_module(mono_aot_module_generics_variant_types_info);
  */

  /*  mono_aot_register_module(mono_aot_module_thread_stress_info); */
  if (argc < 2) {
    printf("no test specified; running basic.exe\n");
    printf("==========================\n");
    try_one("basic.exe");
    printf("==========================\n");
  } else {
    printf("\nProgram %s %s output:\n", argv[0], argv[1]);
    printf("==========================\n\n");
    try_one(argv[1]);
  }

  return 0;
}
#include <stdio.h>
#include <stdlib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>
#include <mono/jit/jit.h>

extern void* mono_aot_module_mscorlib_info;
extern void* mono_aot_module_System_Core_info;
extern void* mono_aot_module_System_info;
extern void* mono_aot_module_Mono_Posix_info;
extern void* mono_aot_module_System_Configuration_info;
extern void* mono_aot_module_System_Security_info;
extern void* mono_aot_module_System_Xml_info;
/* extern void* mono_aot_module_System_Threading_info; */
extern void* mono_aot_module_Mono_Security_info;
extern void* mono_aot_module_Mono_Simd_info;
extern void* mono_aot_module_TestDriver_info;

extern void* mono_aot_module_basic_info;
extern void* mono_aot_module_basic_float_info;
extern void* mono_aot_module_basic_long_info;
extern void* mono_aot_module_basic_calls_info;
extern void* mono_aot_module_basic_simd_info;
extern void* mono_aot_module_objects_info;
extern void* mono_aot_module_arrays_info;
extern void* mono_aot_module_basic_math_info;
extern void* mono_aot_module_exceptions_info;
extern void* mono_aot_module_devirtualization_info;
extern void* mono_aot_module_generics_info;
extern void* mono_aot_module_generics_variant_types_info;
extern void* mono_aot_module_basic_simd_info;
/* extern void* mono_aot_module_thread_stress_info; */


extern void mono_aot_register_module(void *aot_info);
extern void mono_aot_init(void);
extern void mono_jit_set_aot_only(mono_bool aot_only);
extern MonoDomain * mini_init (const char *filename, const char *runtime_version);


void try_one(char *mname) {
  MonoDomain *domain;
  MonoAssembly *ma;
  MonoImage *mi;
  MonoClass *mc;
  MonoMethodDesc *mmd;
  MonoMethod *mm;
  MonoObject *mo;
  MonoArray *arg_array;
  void *args [1];
  char *cstr_arg = "20";

  mono_jit_set_aot_only(1);
  domain = mono_jit_init(mname);
  printf("mono domain: %p\n", domain);

  ma = mono_domain_assembly_open(domain, mname);
  if (0 == ma) {
    printf("ERROR: could not open mono assembly\n");
    exit(-1);
  }
  printf("opened mono assembly: %p\n", ma);

  mi = mono_assembly_get_image(ma);
  printf("mono image: %p\n", mi);

  mo = mono_string_new(domain, cstr_arg);
  mc = mono_class_from_name(mono_get_corlib(), "System", "String");
  printf("string class: %p\n", mc);
  arg_array = mono_array_new(domain, mc, 1);
  mono_array_setref(arg_array, 0, mo);
  args[0] = arg_array;

  mmd = mono_method_desc_new("Tests:Main()", 1);
  mm = mono_method_desc_search_in_image(mmd, mi);
  if (0 == mm) {
    mmd = mono_method_desc_new("Tests:Main(string[])", 1);
    mm = mono_method_desc_search_in_image(mmd, mi);
    if (0 == mm) {
      mmd = mono_method_desc_new("SimdTests:Main(string[])", 1);
      mm = mono_method_desc_search_in_image(mmd, mi);
      if (0 == mm) {
        printf("Couldn't find Tests:Main(), Tests:Main(string[]) or SimdTests:Main(string[])\n");
        exit(-1);
      }
    }
  }
  printf("mono desc method: %p\n", mmd);
  printf("mono method: %p\n", mm);

  mo = mono_runtime_invoke(mm, NULL, args, NULL);
  printf("mono object: %p\n", mo);

  mono_jit_cleanup(domain);
}

int main(int argc, char *argv[]) {
  mono_aot_register_module(mono_aot_module_mscorlib_info);
  mono_aot_register_module(mono_aot_module_TestDriver_info);
  mono_aot_register_module(mono_aot_module_System_Core_info);
  mono_aot_register_module(mono_aot_module_System_info);
  mono_aot_register_module(mono_aot_module_Mono_Posix_info);
  mono_aot_register_module(mono_aot_module_System_Configuration_info);
  mono_aot_register_module(mono_aot_module_System_Security_info);
  mono_aot_register_module(mono_aot_module_System_Xml_info);
  mono_aot_register_module(mono_aot_module_Mono_Security_info);
  /*  mono_aot_register_module(mono_aot_module_System_Threading_info); */
  mono_aot_register_module(mono_aot_module_Mono_Simd_info);

  mono_aot_register_module(mono_aot_module_basic_info);
  mono_aot_register_module(mono_aot_module_basic_float_info);
  mono_aot_register_module(mono_aot_module_basic_long_info);
  mono_aot_register_module(mono_aot_module_basic_calls_info);
  mono_aot_register_module(mono_aot_module_basic_simd_info);
  mono_aot_register_module(mono_aot_module_objects_info);
  mono_aot_register_module(mono_aot_module_arrays_info);
  mono_aot_register_module(mono_aot_module_basic_math_info);
  mono_aot_register_module(mono_aot_module_exceptions_info);
  mono_aot_register_module(mono_aot_module_devirtualization_info);
  /*
  mono_aot_register_module(mono_aot_module_generics_info);
  mono_aot_register_module(mono_aot_module_generics_variant_types_info);
  */

  /*  mono_aot_register_module(mono_aot_module_thread_stress_info); */
  if (argc < 2) {
    printf("no test specified; running basic.exe\n");
    printf("==========================\n");
    try_one("basic.exe");
    printf("==========================\n");
  } else {
    printf("\nProgram %s %s output:\n", argv[0], argv[1]);
    printf("==========================\n\n");
    try_one(argv[1]);
  }

  return 0;
}
