// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "pch.h"
#include "Component.Contracts.BindingViewModel.h"
#include <xplatform.h>

namespace winrt::Component::Contracts::implementation
{
    Windows::UI::Xaml::Interop::INotifyCollectionChanged BindingViewModel::Collection()
    {
        return m_collection;
    }

    void BindingViewModel::AddElement(int32_t i)
    {
        m_collection.push_back(i);
    }

    hstring BindingViewModel::Name()
    {
        return m_name;
    }

    void BindingViewModel::Name(hstring const& value)
    {
        m_name = value;
        m_propertyChangedEvent(*this, Windows::UI::Xaml::Data::PropertyChangedEventArgs(hstring(W("Name"))));
    }

    winrt::event_token BindingViewModel::PropertyChanged(Windows::UI::Xaml::Data::PropertyChangedEventHandler const& handler)
    {
        return m_propertyChangedEvent.add(handler);
    }

    void BindingViewModel::PropertyChanged(winrt::event_token const& token) noexcept
    {
        m_propertyChangedEvent.remove(token);
    }
}
