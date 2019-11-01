// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/TypeTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct TypeTesting : TypeTestingT<TypeTesting>
    {
        TypeTesting() = default;

        hstring GetTypeName(Windows::UI::Xaml::Interop::TypeName const& typeName);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct TypeTesting : TypeTestingT<TypeTesting, implementation::TypeTesting>
    {
    };
}
