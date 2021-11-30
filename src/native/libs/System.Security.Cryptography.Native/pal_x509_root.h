// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include "opensslshim.h"

/*
Look up the directory in which all certificate files therein are considered
trusted (root or trusted intermediate).
*/
PALEXPORT const char* CryptoNative_GetX509RootStorePath(uint8_t* defaultPath);

/*
Look up the file in which all certificates are considered trusted
(root or trusted intermediate), in addition to those files in
the root store path.
*/
PALEXPORT const char* CryptoNative_GetX509RootStoreFile(uint8_t* defaultPath);
