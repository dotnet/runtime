#include "mono-native-platform.h"

extern MonoNativePlatformType mono_native_platform_type;

int32_t
mono_native_get_platform_type (void)
{
	return mono_native_platform_type;
}

