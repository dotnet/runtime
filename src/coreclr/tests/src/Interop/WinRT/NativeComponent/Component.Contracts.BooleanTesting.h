// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/BooleanTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct BooleanTesting : BooleanTestingT<BooleanTesting>
    {
        BooleanTesting() = default;

        bool And(bool left, bool right);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct BooleanTesting : BooleanTestingT<BooleanTesting, implementation::BooleanTesting>
    {
    };
}
