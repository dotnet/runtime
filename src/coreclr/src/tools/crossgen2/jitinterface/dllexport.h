// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// ***
// Define default C export attributes
// ***
#ifdef _WIN32
#define DLL_EXPORT         extern "C" __declspec(dllexport)
#else
#define DLL_EXPORT         extern "C" __attribute((visibility("default")))
#endif // _WIN32


// ***
// Define default call conventions
// ***
#if defined(_X86_) && !defined(PLATFORM_UNIX)
#define STDMETHODCALLTYPE  __stdcall
#else
#define STDMETHODCALLTYPE
#endif //  defined(_X86_) && !defined(PLATFORM_UNIX)
