// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Interface between the VM and Interop library.
//

#ifdef FEATURE_COMINTEROP

// Native calls for the managed ComWrappers API
class ComWrappersNative
{
public:
    static void QCALLTYPE GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease);
};

#endif // FEATURE_COMINTEROP