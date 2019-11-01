// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_x509_name.h"

int32_t CryptoNative_GetX509NameStackFieldCount(X509NameStack* sk)
{
    return sk_X509_NAME_num(sk);
}

X509_NAME* CryptoNative_GetX509NameStackField(X509NameStack* sk, int32_t loc)
{
    return sk_X509_NAME_value(sk, loc);
}
