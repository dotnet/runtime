#ifndef __MONO_NAMED_INTRINSIC_H__
#define __MONO_NAMED_INTRINSIC_H__

#define MONO_RUNTIME

#if defined (MONO_ARCH_SIMD_INTRINSICS)
#define FEATURE_HW_INTRINSICS
#endif

#if defined (TARGET_AMD64) || defined (TARGET_X86)
#define TARGET_XARCH
#endif

// TARGET_ARM64 already defined correctly

#include <../../../coreclr/jit/namedinitrinsiclist.h>
// HKTN-TODO: include any intrinsics that might be Mono-specific

static NamedIntrinsic lookup_named_intrinsic (const char* class_ns, const char* class_name, MonoMethod* method);

#endif