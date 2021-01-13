// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: commonconsts.h
**
**
**============================================================*/

#ifndef _COMMONCONSTS_H_
#define _COMMONCONSTS_H_

#include <pal.h>

const int TIMEOUT = 60 * 5 * 1000;

#define szcHelperProcessStartEvName  "start"
#define szcHelperProcessReadyEvName  "ready"
#define szcHelperProcessFinishEvName  "finish"

/* PEDANTIC and PEDANTIC0 is a helper macro that just grumps about any
 * zero return codes in a generic way. with little typing */
#define PEDANTIC(function, parameters) \
{ \
   if (! (function parameters) ) \
   { \
    Trace("%s: NonFatal failure of %s%s for reasons %u and %u\n", \
          __FILE__, #function, #parameters, GetLastError(), errno); \
   } \
} 
#define PEDANTIC1(function, parameters) \
{ \
   if ( (function parameters) ) \
   { \
    Trace("%s: NonFatal failure of %s%s for reasons %u and %u\n", \
          __FILE__, #function, #parameters, GetLastError(), errno); \
   } \
} 

#endif
