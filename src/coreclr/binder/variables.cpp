// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// Variables.cpp
//


//
// Implements the Variables class
//
// ============================================================

#include "variables.hpp"

#include "ex.h"

namespace BINDER_SPACE
{
    Variables *g_BinderVariables = NULL;

    Variables::Variables()
    {
        // Nothing to do here
    }

    Variables::~Variables()
    {
        // Nothing to do here
    }

    HRESULT Variables::Init()
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            // AssemblyBinder string constants
            httpURLPrefix.SetLiteral(W("http://"));

            // AssemblyName string constants
            cultureNeutral.SetLiteral(W("neutral"));
            corelib.SetLiteral(CoreLibName_W);
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }
};
