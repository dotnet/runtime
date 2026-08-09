// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(__GNUC__) && !defined(__clang__) && defined(TARGET_SUNOS) && defined(TARGET_AMD64)
char sccsid[] __attribute__((used, weak)) = "@(#)No version information produced";
__asm__(".pushsection .init_array; .reloc ., R_X86_64_NONE, sccsid; .popsection");
#else
static char sccsid[] __attribute__((used, retain)) = "@(#)No version information produced";
#endif
