// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_lifetime.h"

jobject AndroidCryptoNative_NewGlobalReference(jobject obj)
{
    return AddGRef(GetJNIEnv(), obj);
}

void AndroidCryptoNative_DeleteGlobalReference(jobject obj)
{
    ReleaseGRef(GetJNIEnv(), obj);
}
