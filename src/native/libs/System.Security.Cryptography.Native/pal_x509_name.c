// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509_name.h"

int32_t CryptoNative_GetX509NameStackFieldCount(X509NameStack* sk)
{
    // No error queie impact.
    return sk_X509_NAME_num(sk);
}

X509_NAME* CryptoNative_GetX509NameStackField(X509NameStack* sk, int32_t loc)
{
    // No error queue impact.
    return sk_X509_NAME_value(sk, loc);
}
