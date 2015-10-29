// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_crypto_types.h"

#include <openssl/x509.h>

/*
Function:
GetX509NameStackFieldCount

Direct shim to sk_X509_NAME_num
*/
extern "C" int32_t GetX509NameStackFieldCount(X509NameStack* sk);

/*
Direct shim to sk_X509_NAME_value
*/
extern "C" X509_NAME* GetX509NameStackField(X509NameStack* sk, int32_t loc);

/*
Shims the d2i_X509_NAME method and makes it easier to invoke from managed code.
*/
extern "C" X509_NAME* DecodeX509Name(const uint8_t* buf, int32_t len);

/*
Cleans up and deletes an X509_NAME instance.

Implemented by calling X509_NAME_free.

No-op if a is null.
The given X509_NAME pointer is invalid after this call.
Always succeeds.
*/
extern "C" void X509NameDestroy(X509_NAME* a);

/*
Function:
NewX509NameStack

Direct shim to sk_X509_NAME_new_null
*/
extern "C" STACK_OF(X509_NAME) * NewX509NameStack();

/*
Function:
PushX509NameStackField

Direct shim to sk_X509_NAME_push
Return values:
1 on success
0 on a NULL stack, or an error within sk_X509_NAME_push
*/
extern "C" int32_t PushX509NameStackField(STACK_OF(X509_NAME) * stack, X509_NAME * x509Name);

/*
Function:
RecursiveFreeX509NameStack

Direct shim to sk_X509_NAME_pop_free
*/
extern "C" void RecursiveFreeX509NameStack(STACK_OF(X509_NAME) * stack);

/*
Function:
DuplicateX509Name

Direct shim to X509_NAME_dup
*/
extern "C" X509_NAME* DuplicateX509Name(X509_NAME* x509Name);

/*
Direct shim to X509_NAME_entry_count
*/
extern "C" int32_t GetX509NameEntryCount(X509_NAME* x509Name);

/*
Direct shim to X509_NAME_get_entry
*/
extern "C" X509_NAME_ENTRY* GetX509NameEntry(X509_NAME* x509Name, int32_t loc);

/*
Direct shim to X509_NAME_ENTRY_get_object
*/
extern "C" ASN1_OBJECT* GetX509NameEntryOid(X509_NAME_ENTRY* nameEntry);

/*
Direct shim to X509_NAME_ENTRY_get_data
*/
extern "C" ASN1_STRING* GetX509NameEntryData(X509_NAME_ENTRY* nameEntry);
