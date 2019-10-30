// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "pch.h"
#include "Component.Contracts.UriTesting.h"

namespace winrt::Component::Contracts::implementation
{
    hstring UriTesting::GetFromUri(Windows::Foundation::Uri const& uri)
    {
        return uri.ToString();
    }

    Windows::Foundation::Uri UriTesting::CreateUriFromString(hstring const& uri)
    {
        return {uri};
    }
}
