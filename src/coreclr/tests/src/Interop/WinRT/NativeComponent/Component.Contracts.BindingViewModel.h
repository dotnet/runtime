// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#include "Component/Contracts/BindingViewModel.g.h"
#include <vector>

template<typename T>
struct BindableIteratorWrapper : winrt::implements<BindableIteratorWrapper<T>, winrt::Windows::UI::Xaml::Interop::IBindableIterator>
{
    BindableIteratorWrapper(winrt::Windows::Foundation::Collections::IIterator<T>&& iterator)
        :m_iterator(std::move(iterator))
    {}

    winrt::Windows::Foundation::IInspectable Current()
    {
        return winrt::box_value(m_iterator.Current());
    }

    bool HasCurrent()
    {
        return m_iterator.HasCurrent();
    }

    bool MoveNext()
    {
        return m_iterator.MoveNext();
    }

private:
    winrt::Windows::Foundation::Collections::IIterator<T> m_iterator;
};

template<typename T>
struct BindableVectorWrapper : winrt::implements<BindableVectorWrapper<T>, winrt::Windows::UI::Xaml::Interop::IBindableVector>
{
    BindableVectorWrapper(winrt::Windows::Foundation::Collections::IVector<T>&& elements)
        :m_elements(std::move(elements))
    {
    }

    winrt::Windows::UI::Xaml::Interop::IBindableIterator First()
    {
        return winrt::make<BindableIteratorWrapper<int32_t>>(m_elements.First());
    }

    uint32_t Size()
    {
        return m_elements.Size();
    }

    void Append(winrt::Windows::Foundation::IInspectable const& value)
    {
        m_elements.Append(winrt::unbox_value<T>(value));
    }

    void Clear()
    {
        m_elements.Clear();
    }

    winrt::Windows::Foundation::IInspectable GetAt(uint32_t index)
    {
        return winrt::box_value(m_elements.GetAt(index));
    }

    winrt::Windows::UI::Xaml::Interop::IBindableVectorView GetView()
    {
        throw winrt::hresult_not_implemented();
    }

    bool IndexOf(winrt::Windows::Foundation::IInspectable const& value, uint32_t& index)
    {
        return m_elements.IndexOf(winrt::unbox_value<T>(value), index);
    }

    void InsertAt(uint32_t index, winrt::Windows::Foundation::IInspectable const& value)
    {
        m_elements.InsertAt(index, winrt::unbox_value<int32_t>(value));
    }

    void RemoveAt(uint32_t index)
    {
        m_elements.RemoveAt(index);
    }

    void RemoveAtEnd()
    {
        m_elements.RemoveAtEnd();
    }

    void SetAt(uint32_t index, winrt::Windows::Foundation::IInspectable const& value)
    {
        m_elements.SetAt(index, winrt::unbox_value<int32_t>(value));
    }
private:
    winrt::Windows::Foundation::Collections::IVector<T> m_elements;
};

template<typename T, typename Container = std::vector<T>>
struct ObservableCollection : winrt::implements<ObservableCollection<T, Container>, winrt::Windows::UI::Xaml::Interop::INotifyCollectionChanged>
{
    ObservableCollection() = default;

    winrt::event_token CollectionChanged(winrt::Windows::UI::Xaml::Interop::NotifyCollectionChangedEventHandler const& handler)
    {
        return m_CollectionChangedEvent.add(handler);
    }

    void CollectionChanged(winrt::event_token const& token) noexcept
    {
        m_CollectionChangedEvent.remove(token);
    }

    void push_back(const T& value)
    {
        m_elements.push_back(value);

        winrt::Windows::UI::Xaml::Interop::NotifyCollectionChangedEventArgs args(
            winrt::Windows::UI::Xaml::Interop::NotifyCollectionChangedAction::Add,
            winrt::make<BindableVectorWrapper<int32_t>>(winrt::single_threaded_vector(std::vector<T>{value})),
            nullptr,
            (int32_t)(m_elements.size() - 1),
            -1
        );
        m_CollectionChangedEvent(*this, args);
    }

private:
    Container m_elements;
    winrt::event<winrt::Windows::UI::Xaml::Interop::NotifyCollectionChangedEventHandler> m_CollectionChangedEvent;
};


namespace winrt::Component::Contracts::implementation
{
    struct BindingViewModel : BindingViewModelT<BindingViewModel>
    {
        BindingViewModel() = default;

        Windows::UI::Xaml::Interop::INotifyCollectionChanged Collection();
        void AddElement(int32_t i);
        hstring Name();
        void Name(hstring const& value);
        winrt::event_token PropertyChanged(Windows::UI::Xaml::Data::PropertyChangedEventHandler const& handler);
        void PropertyChanged(winrt::event_token const& token) noexcept;

    private:
        hstring m_name;
        winrt::event<Windows::UI::Xaml::Data::PropertyChangedEventHandler> m_propertyChangedEvent;
        ObservableCollection<int> m_collection;
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct BindingViewModel : BindingViewModelT<BindingViewModel, implementation::BindingViewModel>
    {
    };
}
