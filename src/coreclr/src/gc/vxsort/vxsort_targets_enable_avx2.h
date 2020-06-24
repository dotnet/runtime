#ifdef __GNUC__
#ifdef __clang__
#pragma clang attribute push (__attribute__((target("avx2"))), apply_to = any(function))
#else
#pragma GCC push_options
#pragma GCC target("avx512f")
#endif
#endif
