// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/UriTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct UriTesting : UriTestingT<UriTesting>
    {
        UriTesting() = default;

        hstring GetFromUri(Windows::Foundation::Uri const& uri);
        Windows::Foundation::Uri CreateUriFromString(hstring const& uri);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct UriTesting : UriTestingT<UriTesting, implementation::UriTesting>
    {
    };
}
