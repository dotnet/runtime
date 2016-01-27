// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Set optimizations settings for small performance critical methods
//

#ifdef FPO_ON
#error Recursive use of FPO_ON not supported
#endif

#define FPO_ON 1


#if defined(_MSC_VER) && !defined(_DEBUG)
 #pragma optimize("t", on)   // optimize for speed
 #if !defined(_AMD64_)   // 'y' isn't an option on amd64
  #pragma optimize("y", on)   // omit frame pointer
 #endif // !defined(_TARGET_AMD64_)
#endif 
