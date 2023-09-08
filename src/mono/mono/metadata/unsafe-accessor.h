// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __MONO_METADATA_UNSAFE_ACCESSOR_H__
#define __MONO_METADATA_UNSAFE_ACCESSOR_H__

#include <mono/metadata/class.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/mono-error.h>

/* keep in sync with System.Runtime.CompilerServices.UnsafeAccessorKind
 * https://github.com/dotnet/runtime/blob/a2c19cd005a1130ba7f921e0264287cfbfa8513c/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/UnsafeAccessorAttribute.cs#L9-L35
 */
typedef enum {
	MONO_UNSAFE_ACCESSOR_CTOR,
	MONO_UNSAFE_ACCESSOR_METHOD,
	MONO_UNSAFE_ACCESSOR_STATIC_METHOD,
	MONO_UNSAFE_ACCESSOR_FIELD,
	MONO_UNSAFE_ACCESSOR_STATIC_FIELD,
} MonoUnsafeAccessorKind;

MonoMethod*
mono_unsafe_accessor_find_ctor (MonoClass *in_class, MonoMethodSignature *sig, MonoClass *from_class, MonoError *error);

MonoMethod*
mono_unsafe_accessor_find_method (MonoClass *in_class, const char *name, MonoMethodSignature *sig, MonoClass *from_class, MonoError *error);

#endif /* __MONO_METADATA_UNSAFE_ACCESSOR_H__ */
