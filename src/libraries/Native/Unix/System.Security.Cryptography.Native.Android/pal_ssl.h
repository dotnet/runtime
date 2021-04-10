// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"
#include <pal_ssl_types.h>

/*
Get the supported protocols
*/
PALEXPORT PAL_SslProtocol AndroidCryptoNative_SSLGetSupportedProtocols(void);

/*
Returns whether or not configuration of application protocols is supported
*/
PALEXPORT bool AndroidCryptoNative_SSLSupportsApplicationProtocolsConfiguration(void);
