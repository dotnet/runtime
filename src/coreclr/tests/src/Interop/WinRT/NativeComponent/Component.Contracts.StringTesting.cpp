// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "pch.h"
#include "Component.Contracts.StringTesting.h"

namespace winrt::Component::Contracts::implementation
{
    hstring StringTesting::ConcatStrings(hstring const& left, hstring const& right)
    {
        return left + right;
    }
}
