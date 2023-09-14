#ifndef __MONO_CROSS_PLATFORM_INTRINSICS_H__
#define __MONO_CROSS_PLATFORM_INTRINSICS_H__

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

MonoInst* emit_cross_platform_intrinsics_for_vector_classes (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, const char* class_ns, const char* class_name);

MonoInst* emit_hw_intrinsics_for_vector_classes (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args);

#endif
