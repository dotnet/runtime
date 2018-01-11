// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_x509_name.h"

extern "C" int32_t CryptoNative_GetX509NameStackFieldCount(X509NameStack* sk)
{
    return sk_X509_NAME_num(sk);
}

extern "C" X509_NAME* CryptoNative_GetX509NameStackField(X509NameStack* sk, int32_t loc)
{
    return sk_X509_NAME_value(sk, loc);
}

extern "C" X509_NAME* CryptoNative_DecodeX509Name(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_X509_NAME(nullptr, &buf, len);
}

extern "C" void CryptoNative_X509NameDestroy(X509_NAME* a)
{
    if (a != nullptr)
    {
        X509_NAME_free(a);
    }
}

extern "C" STACK_OF(X509_NAME) * CryptoNative_NewX509NameStack()
{
    return sk_X509_NAME_new_null();
}

extern "C" int32_t CryptoNative_PushX509NameStackField(STACK_OF(X509_NAME) * stack, X509_NAME* x509Name)
{
    if (!stack)
    {
        return 0;
    }

    return sk_X509_NAME_push(stack, x509Name);
}

extern "C" void CryptoNative_RecursiveFreeX509NameStack(STACK_OF(X509_NAME) * stack)
{
    sk_X509_NAME_pop_free(stack, X509_NAME_free);
}

extern "C" int32_t CryptoNative_GetX509NameEntryCount(X509_NAME* x509Name)
{
    return X509_NAME_entry_count(x509Name);
}

extern "C" X509_NAME_ENTRY* CryptoNative_GetX509NameEntry(X509_NAME* x509Name, int32_t loc)
{
    return X509_NAME_get_entry(x509Name, loc);
}

extern "C" ASN1_OBJECT* CryptoNative_GetX509NameEntryOid(X509_NAME_ENTRY* nameEntry)
{
    return X509_NAME_ENTRY_get_object(nameEntry);
}

extern "C" ASN1_STRING* CryptoNative_GetX509NameEntryData(X509_NAME_ENTRY* nameEntry)
{
    return X509_NAME_ENTRY_get_data(nameEntry);
}
