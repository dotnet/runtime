// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/ExceptionTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct ExceptionTesting : ExceptionTestingT<ExceptionTesting>
    {
        ExceptionTesting() = default;

        void ThrowException(winrt::hresult const& hr);
        winrt::hresult GetException(int32_t hr);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct ExceptionTesting : ExceptionTestingT<ExceptionTesting, implementation::ExceptionTesting>
    {
    };
}
