// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/BindingProjectionsTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct BindingProjectionsTesting : BindingProjectionsTestingT<BindingProjectionsTesting>
    {
        BindingProjectionsTesting() = default;

        Component::Contracts::IBindingViewModel CreateViewModel();

        Windows::Foundation::IClosable InitializeXamlFrameworkForCurrentThread();
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct BindingProjectionsTesting : BindingProjectionsTestingT<BindingProjectionsTesting, implementation::BindingProjectionsTesting>
    {
    };
}
