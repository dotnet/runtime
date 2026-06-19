// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Function prototypes unique to OpenSSL 4.0

#pragma once
#include "pal_types.h"

typedef void (*OPENSSL_sk_freefunc)(void *);
typedef void (*OPENSSL_sk_freefunc_thunk)(OPENSSL_sk_freefunc, void *);

OPENSSL_STACK *OPENSSL_sk_set_thunks(OPENSSL_STACK *st, OPENSSL_sk_freefunc_thunk f_thunk);
