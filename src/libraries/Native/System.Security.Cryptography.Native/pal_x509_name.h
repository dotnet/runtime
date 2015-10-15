// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_crypto_types.h"

#include <openssl/x509.h>

/*
These values should be kept in sync with System.Security.Cryptography.X509Certificates.X500DistinguishedNameFlags
*/
enum X500DistinguishedNameFlags : int32_t
{
    None = 0x0000,
    Reversed = 0x0001,

    UseSemicolons = 0x0010,
    DoNotUsePlusSign = 0x0020,
    DoNotUseQuotes = 0x0040,
    UseCommas = 0x0080,
    UseNewLines = 0x0100,

    UseUTF8Encoding = 0x1000,
    UseT61Encoding = 0x2000,
    ForceUTF8Encoding = 0x4000,
};

/*
Function:
GetX509NameStackFieldCount

Direct shim to sk_X509_NAME_num
*/
extern "C" int32_t GetX509NameStackFieldCount(X509NameStack* sk);

/*
Function:
GetX509NameStackField

Direct shim to sk_X509_NAME_value
*/
extern "C" X509_NAME* GetX509NameStackField(X509NameStack* sk, int32_t loc);

/*
Shims the d2i_X509_NAME method and makes it easier to invoke from managed code.
*/
extern "C" X509_NAME* DecodeX509Name(const unsigned char* buf, int32_t len);

/*
Cleans up and deletes an X509_NAME instance.

Implemented by calling X509_NAME_free.

No-op if a is null.
The given X509_NAME pointer is invalid after this call.
Always succeeds.
*/
extern "C" void X509NameDestroy(X509_NAME* a);

/*
Prints the X509_NAME into the BIO using a format that is acceptable
for use in "Find*" functions.

Returns the number of bytes written to the BIO.
*/
extern "C" int32_t X509NamePrintForFind(BIO* out, X509_NAME* nm);

/*
Prints the X509_NAME into the BIO using the specified format.

Returns the number of bytes written to the BIO.
*/
extern "C" int32_t X509NamePrintEx(BIO* out, X509_NAME* nm, X500DistinguishedNameFlags flags);

/*
Function:
NewX509NameStack

Direct shim to sk_X509_NAME_new_null
*/
extern "C" STACK_OF(X509_NAME)* NewX509NameStack();

/*
Function:
PushX509NameStackField

Direct shim to sk_X509_NAME_push
Return values:
1 on success
0 on a NULL stack, or an error within sk_X509_NAME_push
*/
extern "C" int32_t PushX509NameStackField(STACK_OF(X509_NAME)* stack, X509_NAME* x509Name);

/*
Function:
RecursiveFreeX509NameStack

Direct shim to sk_X509_NAME_pop_free
*/
extern "C" void RecursiveFreeX509NameStack(STACK_OF(X509_NAME)* stack);

/*
Function:
DuplicateX509Name

Direct shim to X509_NAME_dup
*/
extern "C" X509_NAME* DuplicateX509Name(X509_NAME* x509Name);

