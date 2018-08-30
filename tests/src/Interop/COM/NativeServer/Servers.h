// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <xplatform.h>

//#import "Server.Contract.tlb" no_namespace
#include <Server.Contracts.tlh>

// Forward declare servers so COM clients can reference the CLSIDs
class DECLSPEC_UUID("53169A33-E85D-4E3C-B668-24E438D0929B") NumericTesting;
class DECLSPEC_UUID("B99ABE6A-DFF6-440F-BFB6-55179B8FE18E") ArrayTesting;
class DECLSPEC_UUID("C73C83E8-51A2-47F8-9B5C-4284458E47A6") StringTesting;
class DECLSPEC_UUID("71CF5C45-106C-4B32-B418-43A463C6041F") ErrorMarshalTesting;

#define CLSID_NumericTesting __uuidof(NumericTesting)
#define CLSID_ArrayTesting __uuidof(ArrayTesting)
#define CLSID_StringTesting __uuidof(StringTesting)
#define CLSID_ErrorMarshalTesting __uuidof(ErrorMarshalTesting)

#define IID_INumericTesting __uuidof(INumericTesting)
#define IID_IArrayTesting __uuidof(IArrayTesting)
#define IID_IStringTesting __uuidof(IStringTesting)
#define IID_IErrorMarshalTesting __uuidof(IErrorMarshalTesting)

#ifndef COM_CLIENT
    #include "ComHelpers.h"

    #define DEF_RAWFUNC(n) virtual COM_DECLSPEC_NOTHROW HRESULT STDMETHODCALLTYPE raw_ ## n
    #define DEF_FUNC(n) virtual COM_DECLSPEC_NOTHROW HRESULT STDMETHODCALLTYPE ## n

    #include "NumericTesting.h"
    #include "ArrayTesting.h"
    #include "StringTesting.h"
    #include "ErrorMarshalTesting.h"
#endif
