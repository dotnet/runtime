// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Set optimizations settings for small performance critical methods
//

#ifdef FPO_ON
#error Recursive use of FPO_ON not supported
#endif

#define FPO_ON 1


#if defined(_MSC_VER) && !defined(_DEBUG)
 #pragma optimize("t", on)   // optimize for speed
 #if !defined(HOST_AMD64)   // 'y' isn't an option on amd64
  #pragma optimize("y", on)   // omit frame pointer
 #endif // !defined(TARGET_AMD64)
#endif
