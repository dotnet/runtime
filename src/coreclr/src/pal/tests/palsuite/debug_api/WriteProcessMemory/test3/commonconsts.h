// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Header: commonconsts.h
**
**
==============================================================*/

#ifndef _COMMONCONSTS_H_
#define _COMMONCONSTS_H_

#include <pal.h>

const int TIMEOUT = 40000;

const WCHAR szcToHelperEvName[] =  { 'T', 'o', '\0' };
const WCHAR szcFromHelperEvName[] = { 'F', 'r', 'o', 'm', '\0' };

const char initialValue = '-';
const char nextValue = '|';
const char guardValue = '*';
const char *commsFileName = "AddrNLen.dat";

/* PEDANTIC and PEDANTIC0 is a helper macro that just grumps about any
 * zero return codes in a generic way. with little typing */
#define PEDANTIC(function, parameters) \
{ \
   unsigned int retval = (function parameters); \
   if ( !retval ) \
   { \
    Trace("%s: NonFatal failure of %s%s (returned %u) " \
          "for reasons %u and %u.\n", \
          __FILE__, #function, #parameters, retval, GetLastError(), errno); \
   } \
} 
#define PEDANTIC1(function, parameters) \
{ \
   unsigned int retval = (function parameters); \
   if ( retval ) \
   { \
    Trace("%s: NonFatal failure of %s%s (returned %u) " \
          "for reasons %u and %u\n", \
          __FILE__, #function, #parameters, retval, GetLastError(), errno); \
   } \
} 

#endif
