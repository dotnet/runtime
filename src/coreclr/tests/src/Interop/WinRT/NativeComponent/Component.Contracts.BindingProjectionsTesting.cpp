// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "pch.h"
#include "Component.Contracts.BindingProjectionsTesting.h"
#include "Component.Contracts.BindingViewModel.h"
#include <winrt/Windows.UI.Xaml.Hosting.h>

namespace winrt::Component::Contracts::implementation
{
    Component::Contracts::IBindingViewModel BindingProjectionsTesting::CreateViewModel()
    {
        return make<BindingViewModel>();
    }

    Windows::Foundation::IClosable BindingProjectionsTesting::InitializeXamlFrameworkForCurrentThread()
    {
        return Windows::UI::Xaml::Hosting::WindowsXamlManager::InitializeForCurrentThread();
    }
}
