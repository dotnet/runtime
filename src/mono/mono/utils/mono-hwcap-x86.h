#ifndef __MONO_UTILS_HWCAP_X86_H__
#define __MONO_UTILS_HWCAP_X86_H__

#include "mono/utils/mono-hwcap.h"

extern gboolean mono_hwcap_x86_is_xen;
extern gboolean mono_hwcap_x86_has_cmov;
extern gboolean mono_hwcap_x86_has_fcmov;
extern gboolean mono_hwcap_x86_has_sse1;
extern gboolean mono_hwcap_x86_has_sse2;
extern gboolean mono_hwcap_x86_has_sse3;
extern gboolean mono_hwcap_x86_has_ssse3;
extern gboolean mono_hwcap_x86_has_sse41;
extern gboolean mono_hwcap_x86_has_sse42;
extern gboolean mono_hwcap_x86_has_sse4a;

#endif /* __MONO_UTILS_HWCAP_X86_H__ */
