// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_seckey.h"
#include "pal_compiler.h"

#include <Security/Security.h>

/*
Perform the EC Diffie-Hellman key agreement between the provided keys.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t
AppleCryptoNative_EcdhKeyAgree(SecKeyRef privateKey, SecKeyRef publicKey, CFDataRef* pAgreeOut, CFErrorRef* pErrorOut);
