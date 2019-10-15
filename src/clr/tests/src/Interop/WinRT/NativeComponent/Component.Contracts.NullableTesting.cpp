// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "pch.h"
#include "Component.Contracts.NullableTesting.h"

namespace winrt::Component::Contracts::implementation
{
    bool NullableTesting::IsNull(Windows::Foundation::IReference<int32_t> const& value)
    {
        return value == nullptr;
    }

    int32_t NullableTesting::GetIntValue(Windows::Foundation::IReference<int32_t> const& value)
    {
        return winrt::unbox_value<int32_t>(value);
    }
}
