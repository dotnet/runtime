// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef __GNUC__
#ifdef __clang__
#pragma clang attribute push (__attribute__((target("avx512f,avx512dq"))), apply_to = any(function))
#else
#pragma GCC push_options
#pragma GCC target("avx512f,avx512dq")
#endif
#endif
