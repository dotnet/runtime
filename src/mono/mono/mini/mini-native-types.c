#include <config.h>

#if defined(MONO_NATIVE_TYPES)

#include "../../../mono-extensions/mono/mini/mini-native-types.c"

#else

#include "mini.h"

MonoType*
mini_native_type_replace_type (MonoType *type)
{
	return type;
}

MonoInst*
mono_emit_native_types_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

#endif
