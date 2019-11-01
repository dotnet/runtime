// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "pch.h"
#include "Component.Contracts.ExceptionTesting.h"

namespace winrt::Component::Contracts::implementation
{
    void ExceptionTesting::ThrowException(winrt::hresult const& hr)
    {
        winrt::throw_hresult(hr);
    }

    winrt::hresult ExceptionTesting::GetException(int32_t hr)
    {
        return {hr};
    }
}
