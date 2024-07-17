// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <zconf.h> // voidpf

voidpf z_custom_calloc(voidpf opaque, unsigned items, unsigned size);

void z_custom_cfree(voidpf opaque, voidpf ptr);
