// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_crypto_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Function:
GetX509NameStackFieldCount

Direct shim to sk_X509_NAME_num
*/
PALEXPORT int32_t CryptoNative_GetX509NameStackFieldCount(X509NameStack* sk);

/*
Direct shim to sk_X509_NAME_value
*/
PALEXPORT X509_NAME* CryptoNative_GetX509NameStackField(X509NameStack* sk, int32_t loc);
