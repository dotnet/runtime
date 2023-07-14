// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __MONO_METADATA_UNSAFE_ACCESSOR_H__
#define __MONO_METADATA_UNSAFE_ACCESSOR_H__

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

#endif /* __MONO_METADATA_UNSAFE_ACCESSOR_H__ */
