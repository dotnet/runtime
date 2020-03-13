#include <config.h>
#include <glib.h>
#include "mono/utils/mono-threads-api.h"
#include "mono/utils/atomic.h"
#include "mono/metadata/icall-internals.h"

#include "mono-native-platform.h"

extern MonoNativePlatformType mono_native_platform_type;
volatile static gboolean module_initialized;
volatile static gint32 module_counter;

int32_t
mono_native_get_platform_type (void)
{
	return mono_native_platform_type;
}

static int32_t
ves_icall_MonoNativePlatform_IncrementInternalCounter (void)
{
	return mono_atomic_inc_i32 (&module_counter);
}

int32_t
mono_native_is_initialized (void)
{
	return module_initialized;
}

void
mono_native_initialize (void)
{
	if (mono_atomic_cas_i32 (&module_initialized, TRUE, FALSE) != FALSE)
		return;

	mono_add_internal_call_with_flags ("Mono.MonoNativePlatform::IncrementInternalCounter", ves_icall_MonoNativePlatform_IncrementInternalCounter, TRUE);
}
