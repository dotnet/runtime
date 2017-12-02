// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_crypto_types.h"
#include "opensslshim.h"

/*
Function:
GetX509NameStackFieldCount

Direct shim to sk_X509_NAME_num
*/
extern "C" int32_t CryptoNative_GetX509NameStackFieldCount(X509NameStack* sk);

/*
Direct shim to sk_X509_NAME_value
*/
extern "C" X509_NAME* CryptoNative_GetX509NameStackField(X509NameStack* sk, int32_t loc);

/*
Shims the d2i_X509_NAME method and makes it easier to invoke from managed code.
*/
extern "C" X509_NAME* CryptoNative_DecodeX509Name(const uint8_t* buf, int32_t len);

/*
Cleans up and deletes an X509_NAME instance.

Implemented by calling X509_NAME_free.

No-op if a is null.
The given X509_NAME pointer is invalid after this call.
Always succeeds.
*/
extern "C" void CryptoNative_X509NameDestroy(X509_NAME* a);

/*
Function:
NewX509NameStack

Direct shim to sk_X509_NAME_new_null
*/
extern "C" STACK_OF(X509_NAME) * CryptoNative_NewX509NameStack();

/*
Function:
PushX509NameStackField

Direct shim to sk_X509_NAME_push
Return values:
1 on success
0 on a NULL stack, or an error within sk_X509_NAME_push
*/
extern "C" int32_t CryptoNative_PushX509NameStackField(STACK_OF(X509_NAME) * stack, X509_NAME* x509Name);

/*
Function:
RecursiveFreeX509NameStack

Direct shim to sk_X509_NAME_pop_free
*/
extern "C" void CryptoNative_RecursiveFreeX509NameStack(STACK_OF(X509_NAME) * stack);

/*
Direct shim to X509_NAME_entry_count
*/
extern "C" int32_t CryptoNative_GetX509NameEntryCount(X509_NAME* x509Name);

/*
Direct shim to X509_NAME_get_entry
*/
extern "C" X509_NAME_ENTRY* CryptoNative_GetX509NameEntry(X509_NAME* x509Name, int32_t loc);

/*
Direct shim to X509_NAME_ENTRY_get_object
*/
extern "C" ASN1_OBJECT* CryptoNative_GetX509NameEntryOid(X509_NAME_ENTRY* nameEntry);

/*
Direct shim to X509_NAME_ENTRY_get_data
*/
extern "C" ASN1_STRING* CryptoNative_GetX509NameEntryData(X509_NAME_ENTRY* nameEntry);
