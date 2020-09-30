// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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
#if defined(HOST_X86) && !defined(HOST_UNIX)
#define STDMETHODCALLTYPE  __stdcall
#else
#define STDMETHODCALLTYPE
#endif //  defined(HOST_X86) && !defined(HOST_UNIX)
