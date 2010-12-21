#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>
#include <mono/jit/jit.h>
#include <mono/utils/mono-logger.h>

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
extern void* mono_aot_module_gc_stress_info;
extern void* mono_aot_module_imt_big_iface_test_info;
extern void* mono_aot_module_make_imt_test_info;
/* extern void* mono_aot_module_thread_stress_info; */
extern void* mono_aot_module_iltests_info;

extern void mono_aot_register_module(void *aot_info);
extern void mono_aot_init(void);
extern void mono_jit_set_aot_only(mono_bool aot_only);
extern MonoDomain * mini_init (const char *filename, const char *runtime_version);

int run_all_test_methods(MonoClass *klass) {
  void * iter = NULL;
  MonoMethod *mm = NULL;
  int count = 0;
  int passed = 0;
  printf("Running test methods without reflection\n");
  while (NULL != (mm = mono_class_get_methods(klass, &iter))) {
    long expected_result;
    const char *name = mono_method_get_name(mm);
    char *end = NULL;
    if (strncmp(name, "test_", 5)) continue;
    printf("=== Test %d, method %s\n", count, mono_method_get_name(mm));
    expected_result = strtol(name + 5, &end, 10);
    if (name == end) {
      printf(" warning: could not determine expected return value\n");
      expected_result = 0;
    }
    MonoObject *mo = mono_runtime_invoke(mm, NULL, NULL, NULL);
    int *ret = mono_object_unbox(mo);
    if (ret && *ret == expected_result) {
      printf(" passed!\n");
      passed++;
    } else {
      printf(" FAILED, expected %d, returned %p, %d\n", expected_result, ret,
             ret != NULL ? *ret : 0);
    }
    count++;
  }
  if (count > 0) {
    printf("============================================\n");
    printf("Final count: %d tests, %d pass, %.2f%%\n", count, passed,
           (double)passed / count * 100.0);
  } else {
    printf("no test methods found.\n");
  }
  return count;
}

#if defined(__native_client__)
extern void* mono_aot_module_nacl_info;
extern char* nacl_mono_path;
char *load_corlib_data() {
  FILE *mscorlib;
  static char *corlib_data = NULL;
  if (corlib_data) return corlib_data;

  mscorlib = fopen("mscorlib.dll", "r");
  if (NULL != mscorlib) {
    size_t size;
    struct stat st;
    if (0 == stat("mscorlib.dll", &st)) {
      size = st.st_size;
      printf("reading mscorlib.dll, size %ld\n", size);
      corlib_data = malloc(size);
      if (corlib_data != NULL) {
        while (fread(corlib_data, 1, size, mscorlib) != 0) ;
        if (!ferror(mscorlib)) {
          mono_set_corlib_data(corlib_data, size);
        } else {
          perror("error reading mscorlib.dll");
          free(corlib_data);
          corlib_data = NULL;
        }
      } else {
        perror("Could not allocate memory");
      }
    } else {
      perror("stat error");
    }
    fclose(mscorlib);
  }
  return corlib_data;
}
#endif

/* Initialize Mono. Must run only once per process */
MonoDomain *init_mono(char *mname) {
  MonoDomain *domain = NULL;
#ifdef AOT_VERSION
  mono_jit_set_aot_only(1);
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
  mono_aot_register_module(mono_aot_module_generics_info);
  mono_aot_register_module(mono_aot_module_generics_variant_types_info);
  mono_aot_register_module(mono_aot_module_gc_stress_info);
  mono_aot_register_module(mono_aot_module_imt_big_iface_test_info);
  mono_aot_register_module(mono_aot_module_iltests_info);
#endif
  /* mono_aot_register_module(mono_aot_module_make_imt_test_info); */
  /* mono_aot_register_module(mono_aot_module_thread_stress_info); */
#if defined(__native_client__)
#ifdef AOT_VERSION
  mono_aot_register_module(mono_aot_module_nacl_info);
#endif

  /* Test file-less shortcut for loading mscorlib metadata */
  load_corlib_data();
  nacl_mono_path = strdup(".");
#endif
  /* Uncomment the following if something is going wrong */
  /* mono_trace_set_level_string("info"); */
  domain = mono_jit_init(mname);
  if (NULL == domain) {
    printf("ERROR: mono_jit_init failure\n");
    exit(-1);
  }
  return domain;
}

/* Run all tests from one assembly file */
int try_one(char *mname, MonoDomain *domain) {
  MonoAssembly *ma;
  MonoImage *mi;
  MonoClass *mc;
  MonoMethodDesc *mmd;
  MonoMethod *mm;
  MonoObject *mo;
  MonoString *monostring_arg;
  MonoArray *arg_array;
  int *failures = NULL;
  const int kUseTestDriver = 1;
  int test_count = 0;
  void *args [1];
  char *cstr_arg = "--timing";

  ma = mono_domain_assembly_open(domain, mname);
  if (NULL == ma) {
    printf("ERROR: could not open mono assembly\n");
    exit(-1);
  }

  mi = mono_assembly_get_image(ma);
  if (NULL == mi) {
    printf("ERROR: could not get assembly image\n");
    exit(-1);
  }

  monostring_arg = mono_string_new(domain, cstr_arg);
  mc = mono_class_from_name(mono_get_corlib(), "System", "String");
  if (0 == mc) {
    printf("ERROR: could not mono string class\n");
    exit(-1);
  }

  // to pass a string argument, change the 0 to a 1 and uncomment
  // mono_array_setref below
  arg_array = mono_array_new(domain, mc, 0);
  //mono_array_setref(arg_array, 0, monostring_arg);
  args[0] = arg_array;

  if (!kUseTestDriver) {
    mc = mono_class_from_name(mi, "", "Tests");
    if (NULL == mc) {
      printf("could not open Tests class\n");
      exit(-1);
    }
    test_count = run_all_test_methods(mc);
  }
  /* If run_all_test_methods didn't find any tests, try Main */
  if (kUseTestDriver || test_count == 0) {
    mmd = mono_method_desc_new("Tests:Main()", 1);
    mm = mono_method_desc_search_in_image(mmd, mi);
    if (0 == mm) {
      mmd = mono_method_desc_new("Tests:Main(string[])", 1);
      mm = mono_method_desc_search_in_image(mmd, mi);
      if (0 == mm) {
        printf("Couldn't find Tests:Main() or Tests:Main(string[])\n");
        exit(-1);
      }
    }

    mo = mono_runtime_invoke(mm, NULL, args, NULL);
    failures = mo != NULL ? mono_object_unbox(mo) : NULL;
    if (NULL == failures || *failures != 0) {
      printf("--------------------> Failed");
    }
  }
  return failures != NULL ? failures : 1;
}

int main(int argc, char *argv[]) {
   MonoDomain *domain;
   int failures = 0;

  if (argc < 2) {
    printf("no test specified; running basic.exe\n");
    printf("================================\n");
    domain = init_mono("basic.exe");
    try_one("basic.exe", domain);
  } else {
    domain = init_mono(argv[1]);
    int i;
    for (i = 1; i < argc; i++) {
      printf("\nRunning tests from %s:\n", argv[i]);
      printf("===============================\n\n");
      failures += try_one(argv[i], domain);
    }
  }
  mono_jit_cleanup(domain);
  return failures;
}
