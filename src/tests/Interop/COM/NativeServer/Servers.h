// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <xplatform.h>
#include <cassert>

#include <Server.Contracts.h>

// Forward declare servers so COM clients can reference the CLSIDs
class DECLSPEC_UUID("53169A33-E85D-4E3C-B668-24E438D0929B") NumericTesting;
class DECLSPEC_UUID("B99ABE6A-DFF6-440F-BFB6-55179B8FE18E") ArrayTesting;
class DECLSPEC_UUID("C73C83E8-51A2-47F8-9B5C-4284458E47A6") StringTesting;
class DECLSPEC_UUID("71CF5C45-106C-4B32-B418-43A463C6041F") ErrorMarshalTesting;
class DECLSPEC_UUID("0F8ACD0C-ECE0-4F2A-BD1B-6BFCA93A0726") DispatchTesting;
class DECLSPEC_UUID("4DBD9B61-E372-499F-84DE-EFC70AA8A009") EventTesting;
class DECLSPEC_UUID("4CEFE36D-F377-4B6E-8C34-819A8BB9CB04") AggregationTesting;
class DECLSPEC_UUID("C222F472-DA5A-4FC6-9321-92F4F7053A65") ColorTesting;
class DECLSPEC_UUID("66DB7882-E2B0-471D-92C7-B2B52A0EA535") LicenseTesting;
class DECLSPEC_UUID("FAEF42AE-C1A4-419F-A912-B768AC2679EA") DefaultInterfaceTesting;
class DECLSPEC_UUID("CE137261-6F19-44F5-A449-EF963B3F987E") InspectableTesting;
class DECLSPEC_UUID("4F54231D-9E11-4C0B-8E0B-2EBD8B0E5811") TrackMyLifetimeTesting;

#define CLSID_NumericTesting __uuidof(NumericTesting)
#define CLSID_ArrayTesting __uuidof(ArrayTesting)
#define CLSID_StringTesting __uuidof(StringTesting)
#define CLSID_ErrorMarshalTesting __uuidof(ErrorMarshalTesting)
#define CLSID_DispatchTesting __uuidof(DispatchTesting)
#define CLSID_EventTesting __uuidof(EventTesting)
#define CLSID_AggregationTesting __uuidof(AggregationTesting)
#define CLSID_ColorTesting __uuidof(ColorTesting)
#define CLSID_LicenseTesting __uuidof(LicenseTesting)
#define CLSID_DefaultInterfaceTesting __uuidof(DefaultInterfaceTesting)
#define CLSID_InspectableTesting __uuidof(InspectableTesting)
#define CLSID_TrackMyLifetimeTesting __uuidof(TrackMyLifetimeTesting)

#define IID_INumericTesting __uuidof(INumericTesting)
#define IID_IArrayTesting __uuidof(IArrayTesting)
#define IID_IStringTesting __uuidof(IStringTesting)
#define IID_IErrorMarshalTesting __uuidof(IErrorMarshalTesting)
#define IID_IDispatchTesting __uuidof(IDispatchTesting)
#define IID_TestingEvents __uuidof(TestingEvents)
#define IID_IEventTesting __uuidof(IEventTesting)
#define IID_IAggregationTesting __uuidof(IAggregationTesting)
#define IID_IColorTesting __uuidof(IColorTesting)
#define IID_ILicenseTesting __uuidof(ILicenseTesting)
#define IID_IDefaultInterfaceTesting __uuidof(IDefaultInterfaceTesting)
#define IID_IDefaultInterfaceTesting2 __uuidof(IDefaultInterfaceTesting2)
#define IID_IInspectableTesting __uuidof(IInspectableTesting)
#define IID_IInspectableTesting2 __uuidof(IInspectableTesting2)
#define IID_ITrackMyLifetimeTesting __uuidof(ITrackMyLifetimeTesting)

// Class used for COM activation when using CoreShim
struct CoreShimComActivation
{
    CoreShimComActivation(_In_z_ const WCHAR *assemblyName, _In_z_ const WCHAR *typeName)
    {
        assert(assemblyName && typeName);
        Set(assemblyName, typeName);
    }

    ~CoreShimComActivation()
    {
        Set(nullptr, nullptr);
    }

private:
    void Set(_In_opt_z_ const WCHAR *assemblyName, _In_opt_z_ const WCHAR *typeName)
    {
        // See CoreShim.h for usage of environment variables
        ::SetEnvironmentVariableW(W("CORESHIM_COMACT_ASSEMBLYNAME"), assemblyName);
        ::SetEnvironmentVariableW(W("CORESHIM_COMACT_TYPENAME"), typeName);
    }
};

#include <ComHelpers.h>

#ifndef COM_CLIENT
    #define DEF_FUNC(n) virtual COM_DECLSPEC_NOTHROW HRESULT STDMETHODCALLTYPE n

    #include "NumericTesting.h"
    #include "ArrayTesting.h"
    #include "StringTesting.h"
    #include "ErrorMarshalTesting.h"
    #include "DispatchTesting.h"
    #include "EventTesting.h"
    #include "AggregationTesting.h"
    #include "ColorTesting.h"
    #include "LicenseTesting.h"
    #include "InspectableTesting.h"
    #include "TrackMyLifetimeTesting.h"
#endif
