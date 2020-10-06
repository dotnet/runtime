// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if defined(__GNUC__)
#define EXPORT_API extern "C" __attribute__((visibility("default")))
#elif defined(_MSC_VER)
#define EXPORT_API extern "C" __declspec(dllexport)
#endif 

EXPORT_API bool NativeFunc()
{
    return true;
}
