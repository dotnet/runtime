// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

PALEXPORT jobject AndroidCryptoNative_NewGlobalReference(jobject obj);
PALEXPORT void AndroidCryptoNative_DeleteGlobalReference(jobject obj);
