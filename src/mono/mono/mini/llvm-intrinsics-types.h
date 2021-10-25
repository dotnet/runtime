#ifndef __MONO_MINI_LLVM_INTRINSICS_TYPES_H__
#define __MONO_MINI_LLVM_INTRINSICS_TYPES_H__

/* An intrinsic id. The lower 23 bits are used to store a mono-specific ID. The
 * next 9 bits store overload tag bits. In the configuration of LLVM 9 we use,
 * there are 7017 total intrinsics defined in IntrinsicEnums.inc, so only 13
 * bits are needed to label each intrinsic overload group.
 */
typedef enum {
#define INTRINS(id, llvm_id, arch) INTRINS_ ## id,
#define INTRINS_OVR(id, llvm_id, arch, ty) INTRINS_ ## id,
#define INTRINS_OVR_2_ARG(id, llvm_id, arch, ty1, ty2) INTRINS_ ## id,
#define INTRINS_OVR_3_ARG(id, llvm_id, arch, ty1, ty2, ty3) INTRINS_ ## id,
#define INTRINS_OVR_TAG(id, ...) INTRINS_ ## id,
#define INTRINS_OVR_TAG_KIND(id, ...) INTRINS_ ## id,
#include "llvm-intrinsics.h"
	INTRINS_NUM
} IntrinsicId;

enum {
	XBINOP_FORCEINT_and,
	XBINOP_FORCEINT_or,
	XBINOP_FORCEINT_ornot,
	XBINOP_FORCEINT_xor,
};

#endif /* __MONO_MINI_LLVM_INTRINSICS_TYPES_H__ */
