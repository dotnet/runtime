// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_x509_name.h"

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t GetX509NameStackFieldCount(X509NameStack* sk)
{
    return CryptoNative_GetX509NameStackFieldCount(sk);
}

extern "C" int32_t CryptoNative_GetX509NameStackFieldCount(X509NameStack* sk)
{
    return sk_X509_NAME_num(sk);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" X509_NAME* GetX509NameStackField(X509NameStack* sk, int32_t loc)
{
    return CryptoNative_GetX509NameStackField(sk, loc);
}

extern "C" X509_NAME* CryptoNative_GetX509NameStackField(X509NameStack* sk, int32_t loc)
{
    return sk_X509_NAME_value(sk, loc);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" X509_NAME* DecodeX509Name(const uint8_t* buf, int32_t len)
{
    return CryptoNative_DecodeX509Name(buf, len);
}

extern "C" X509_NAME* CryptoNative_DecodeX509Name(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_X509_NAME(nullptr, &buf, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void X509NameDestroy(X509_NAME* a)
{
    return CryptoNative_X509NameDestroy(a);
}

extern "C" void CryptoNative_X509NameDestroy(X509_NAME* a)
{
    if (a != nullptr)
    {
        X509_NAME_free(a);
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" STACK_OF(X509_NAME) * NewX509NameStack()
{
    return CryptoNative_NewX509NameStack();
}

extern "C" STACK_OF(X509_NAME) * CryptoNative_NewX509NameStack()
{
    return sk_X509_NAME_new_null();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t PushX509NameStackField(STACK_OF(X509_NAME) * stack, X509_NAME* x509Name)
{
    return CryptoNative_PushX509NameStackField(stack, x509Name);
}

extern "C" int32_t CryptoNative_PushX509NameStackField(STACK_OF(X509_NAME) * stack, X509_NAME* x509Name)
{
    if (!stack)
    {
        return 0;
    }

    return sk_X509_NAME_push(stack, x509Name);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void RecursiveFreeX509NameStack(STACK_OF(X509_NAME) * stack)
{
    return CryptoNative_RecursiveFreeX509NameStack(stack);
}

extern "C" void CryptoNative_RecursiveFreeX509NameStack(STACK_OF(X509_NAME) * stack)
{
    sk_X509_NAME_pop_free(stack, X509_NAME_free);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" X509_NAME* DuplicateX509Name(X509_NAME* x509Name)
{
    return CryptoNative_DuplicateX509Name(x509Name);
}

extern "C" X509_NAME* CryptoNative_DuplicateX509Name(X509_NAME* x509Name)
{
    return X509_NAME_dup(x509Name);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t GetX509NameEntryCount(X509_NAME* x509Name)
{
    return CryptoNative_GetX509NameEntryCount(x509Name);
}

extern "C" int32_t CryptoNative_GetX509NameEntryCount(X509_NAME* x509Name)
{
    return X509_NAME_entry_count(x509Name);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" X509_NAME_ENTRY* GetX509NameEntry(X509_NAME* x509Name, int32_t loc)
{
    return CryptoNative_GetX509NameEntry(x509Name, loc);
}

extern "C" X509_NAME_ENTRY* CryptoNative_GetX509NameEntry(X509_NAME* x509Name, int32_t loc)
{
    return X509_NAME_get_entry(x509Name, loc);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" ASN1_OBJECT* GetX509NameEntryOid(X509_NAME_ENTRY* nameEntry)
{
    return CryptoNative_GetX509NameEntryOid(nameEntry);
}

extern "C" ASN1_OBJECT* CryptoNative_GetX509NameEntryOid(X509_NAME_ENTRY* nameEntry)
{
    return X509_NAME_ENTRY_get_object(nameEntry);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" ASN1_STRING* GetX509NameEntryData(X509_NAME_ENTRY* nameEntry)
{
    return CryptoNative_GetX509NameEntryData(nameEntry);
}

extern "C" ASN1_STRING* CryptoNative_GetX509NameEntryData(X509_NAME_ENTRY* nameEntry)
{
    return X509_NAME_ENTRY_get_data(nameEntry);
}
