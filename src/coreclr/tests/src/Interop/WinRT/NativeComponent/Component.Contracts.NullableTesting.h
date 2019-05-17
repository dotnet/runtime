// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/NullableTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct NullableTesting : NullableTestingT<NullableTesting>
    {
        NullableTesting() = default;

        bool IsNull(Windows::Foundation::IReference<int32_t> const& value);
        int32_t GetIntValue(Windows::Foundation::IReference<int32_t> const& value);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct NullableTesting : NullableTestingT<NullableTesting, implementation::NullableTesting>
    {
    };
}
