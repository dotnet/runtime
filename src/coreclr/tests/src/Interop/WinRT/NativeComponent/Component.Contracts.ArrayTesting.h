// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/ArrayTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct ArrayTesting : ArrayTestingT<ArrayTesting>
    {
        ArrayTesting() = default;

        int32_t Sum(array_view<int32_t const> array);
        bool Xor(array_view<bool const> array);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct ArrayTesting : ArrayTestingT<ArrayTesting, implementation::ArrayTesting>
    {
    };
}
