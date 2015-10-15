// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once
#include "pal_types.h"

#include <openssl/x509.h>

typedef STACK_OF(X509) X509Stack;
typedef STACK_OF(X509_NAME) X509NameStack;
