// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "pch.h"
#include "Component.Contracts.ArrayTesting.h"
#include <numeric>

namespace winrt::Component::Contracts::implementation
{
    int32_t ArrayTesting::Sum(array_view<int32_t const> array)
    {
        return std::accumulate(array.begin(), array.end(), 0);
    }

    bool ArrayTesting::Xor(array_view<bool const> array)
    {
        return std::accumulate(array.begin(), array.end(), false, [](bool left, bool right) { return left ^ right; });
    }
}
