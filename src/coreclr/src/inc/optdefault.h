// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Revert optimizations back to default
//
#undef FPO_ON

#ifdef _MSC_VER
#pragma optimize("",on)
#endif
