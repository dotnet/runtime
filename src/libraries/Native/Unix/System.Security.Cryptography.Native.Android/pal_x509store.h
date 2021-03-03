// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

/*
Enumerate trusted certificates
The certificate passed to the callback will already be a global jobject reference.

Returns 1 on success, 0 otherwise.
*/
typedef void (*EnumCertificatesCallback)(jobject cert, void* context);
PALEXPORT int32_t AndroidCryptoNative_X509StoreEnumerateTrustedCertificates(bool isSystem,
                                                                            EnumCertificatesCallback cb,
                                                                            void* context);
