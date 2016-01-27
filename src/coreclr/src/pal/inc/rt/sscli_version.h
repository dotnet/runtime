// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File: sscli_version.h
// 
// =========================================================================== 


#ifndef __SSCLI_VERSION_H__
#define __SSCLI_VERSION_H__

#ifndef __RC_STRINGIZE__
#define __RC_STRINGIZE__AUX(x)      #x
#define __RC_STRINGIZE__(x)         __RC_STRINGIZE__AUX(x)
#endif

#ifndef __RC_STRINGIZE_WSZ__
#define __RC_STRINGIZE_WSZ__AUX(x)  L###x
#define __RC_STRINGIZE_WSZ__(x)     __RC_STRINGIZE_WSZ__AUX(x)
#endif

#define SSCLI_VERSION_MAJOR 2
#define SSCLI_VERSION_MINOR 0
#define SSCLI_VERSION_RELEASE 0001

#define SSCLI_VERSION_STR __RC_STRINGIZE__(SSCLI_VERSION_MAJOR) "." __RC_STRINGIZE__(SSCLI_VERSION_MINOR) "." __RC_STRINGIZE__(SSCLI_VERSION_RELEASE)

#define SSCLI_VERSION_STRW __RC_STRINGIZE_WSZ__(SSCLI_VERSION_MAJOR) L"." __RC_STRINGIZE_WSZ__(SSCLI_VERSION_MINOR) L"." __RC_STRINGIZE_WSZ__(SSCLI_VERSION_RELEASE)
#endif // __SSCLI_VERSION_H__
