/***
* ==++==
*
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* ==--==
* =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
*
* Standard macros and definitions.
* This header has minimal dependency on windows headers and is safe for use in the public API
*
* For the latest on this and related APIs, please see: https://github.com/Microsoft/cpprestsdk
*
* =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
****/

#pragma once

#if defined(_WIN32) // Settings specific to Windows

#if _MSC_VER >= 1900
#define CPPREST_NOEXCEPT noexcept
#else
#define CPPREST_NOEXCEPT
#endif

#define CASABLANCA_UNREFERENCED_PARAMETER(x) (x)

#include <sal.h>

#else // End settings specific to Windows

// Settings common to all but Windows

#define __declspec(x) __attribute__ ((x))
#define dllimport
#define novtable /* no novtable equivalent */
#define __assume(x) do { if (!(x)) __builtin_unreachable(); } while (false)
#define CASABLANCA_UNREFERENCED_PARAMETER(x) (void)x
#define CPPREST_NOEXCEPT noexcept

#include <assert.h>
#define _ASSERTE(x) assert(x)

// No SAL on non Windows platforms
#include "cpprest/details/nosal.h"

#if not defined __cdecl
#if defined cdecl
#define __cdecl __attribute__ ((cdecl))
#else
#define __cdecl
#endif

#if defined(__ANDROID__)
// This is needed to disable the use of __thread inside the boost library.
// Android does not support thread local storage -- if boost is included
// without this macro defined, it will create references to __tls_get_addr
// which (while able to link) will not be available at runtime and prevent
// the .so from loading.
#define BOOST_ASIO_DISABLE_THREAD_KEYWORD_EXTENSION
#endif

#ifdef __clang__
#include <cstdio>
#endif

#endif // defined(__APPLE__)

#endif


#ifdef _NO_ASYNCRTIMP
#define _ASYNCRTIMP
#else
#ifdef _ASYNCRT_EXPORT
#define _ASYNCRTIMP __declspec(dllexport)
#else
#define _ASYNCRTIMP __declspec(dllimport)
#endif
#endif

#ifdef CASABLANCA_DEPRECATION_NO_WARNINGS
#define CASABLANCA_DEPRECATED(x)
#else
#define CASABLANCA_DEPRECATED(x) __declspec(deprecated(x))
#endif
