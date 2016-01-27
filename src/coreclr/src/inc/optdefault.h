// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Revert optimizations back to default
//
#undef FPO_ON

#ifdef _MSC_VER
#pragma optimize("",on)
#endif
