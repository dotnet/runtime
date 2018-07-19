// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "ComHelpers.h"

//#import "Server.Contract.tlb" no_namespace
#include <Server.Contracts.tlh>

#define DEF_RAWFUNC(n) virtual COM_DECLSPEC_NOTHROW HRESULT STDMETHODCALLTYPE raw_ ## n
#define DEF_FUNC(n) virtual COM_DECLSPEC_NOTHROW HRESULT STDMETHODCALLTYPE ## n

#include "NumericTesting.h"
#include "ArrayTesting.h"
#include "StringTesting.h"
#include "ErrorMarshalTesting.h"
