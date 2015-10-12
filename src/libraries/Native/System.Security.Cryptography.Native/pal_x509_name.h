// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdint.h>
#include <openssl/x509.h>

/*
Function:
GetX509NameStackFieldCount

Direct shim to sk_X509_NAME_num
*/
extern "C" int32_t GetX509NameStackFieldCount(STACK_OF(X509_NAME) * sk);

/*
Function:
GetX509NameStackField

Direct shim to sk_X509_NAME_value
*/
extern "C" X509_NAME* GetX509NameStackField(STACK_OF(X509_NAME) * sk, int32_t loc);

/*
Shims the d2i_X509_NAME method and makes it easier to invoke from managed code.
*/
extern "C" X509_NAME* DecodeX509Name(const unsigned char* buf, int32_t len);
