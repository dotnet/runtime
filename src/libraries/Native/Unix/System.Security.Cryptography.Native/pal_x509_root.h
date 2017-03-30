// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "opensslshim.h"

/*
Look up the directory in which all certificate files therein are considered
trusted (root or trusted intermediate).
*/
extern "C" const char* CryptoNative_GetX509RootStorePath();

/*
Look up the file in which all certificates are considered trusted
(root or trusted intermediate), in addition to those files in
the root store path.
*/
extern "C" const char* CryptoNative_GetX509RootStoreFile();
