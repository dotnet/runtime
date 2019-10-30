// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/StringTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct StringTesting : StringTestingT<StringTesting>
    {
        StringTesting() = default;

        hstring ConcatStrings(hstring const& left, hstring const& right);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct StringTesting : StringTestingT<StringTesting, implementation::StringTesting>
    {
    };
}
