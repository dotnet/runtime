// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef COREUTILS_H
#define COREUTILS_H

#define INITIAL_CAPACITY 64

template <typename T>
T&& Move(T& value) noexcept
{
    return static_cast<T&&>(value);
}

class OwnedString
{
private:
    char* m_value;
    size_t m_length;
public:
    OwnedString() noexcept :
        m_value(nullptr),
        m_length(0)
    {
    }

    ~OwnedString() noexcept
    {
        free(m_value);
    }

    OwnedString(OwnedString&& other) noexcept :
        m_value(other.m_value),
        m_length(other.m_length)
    {
        other.m_value = nullptr;
        other.m_length = 0;
    }

    OwnedString& operator=(OwnedString&& other) noexcept
    {
        if (this != &other)
        {
            free(m_value);

            m_value = other.m_value;
            m_length = other.m_length;

            other.m_value = nullptr;
            other.m_length = 0;
        }

        return *this;
    }

    OwnedString(const OwnedString&) = delete;
    OwnedString& operator=(const OwnedString&) = delete;

    void TakeOwnership(char* value) noexcept
    {
        free(m_value);

        m_value = value;
        m_length = value != nullptr ? strlen(value) : 0;
    }

    bool Assign(const char* value) noexcept
    {
        size_t length = value != nullptr ? strlen(value) : 0;

        char* copy = static_cast<char*>(malloc(length + 1));
        if (copy == nullptr)
        {
            return false;
        }

        if (length != 0)
        {
            memcpy(copy, value, length);
        }

        copy[length] = '\0';

        free(m_value);
        m_value = copy;
        m_length = length;
        return true;
    }

    const char* CStr() const noexcept
    {
        return m_value != nullptr ? m_value : "";
    }

    size_t Length() const noexcept
    {
        return m_length;
    }

    bool IsEmpty() const noexcept
    {
        return m_length == 0;
    }
};

template <typename T>
class DynamicArray
{
private:
    T* m_items = nullptr;
    size_t m_count = 0;
    size_t m_capacity = 0;

    bool Grow() noexcept
    {
        if (m_capacity > SIZE_MAX / 2)
        {
            return false;
        }

        size_t newCapacity = m_capacity == 0 ? INITIAL_CAPACITY : m_capacity * 2;

        T* items = new (std::nothrow) T[newCapacity];

        if (items == nullptr)
        {
            return false;
        }

        for (size_t index = 0; index < m_count; index++)
        {
            items[index] = Move(m_items[index]);
        }

        delete[] m_items;
        m_items = items;
        m_capacity = newCapacity;
        return true;
    }

public:
    DynamicArray() noexcept = default;

    ~DynamicArray() noexcept
    {
        delete[] m_items;
    }

    DynamicArray(const DynamicArray&) = delete;
    DynamicArray& operator=(const DynamicArray&) = delete;

    bool Add(const T& item) noexcept
    {
        if (m_count == m_capacity && !Grow())
        {
            return false;
        }

        m_items[m_count++] = item;
        return true;
    }

    bool Add(T&& item) noexcept
    {
        if (m_count == m_capacity && !Grow())
        {
            return false;
        }

        m_items[m_count++] = Move(item);
        return true;
    }

    T& operator[](size_t index) noexcept
    {
        assert(index < m_count);
        return m_items[index];
    }

    const T& operator[](size_t index) const noexcept
    {
        assert(index < m_count);
        return m_items[index];
    }

    T* Data() noexcept
    {
        return m_items;
    }

    T* begin() noexcept
    {
        return m_items;
    }

    T* end() noexcept
    {
        return m_items != nullptr ? m_items + m_count : nullptr;
    }

    const T* begin() const noexcept
    {
        return m_items;
    }

    const T* end() const noexcept
    {
        return m_items != nullptr ? m_items + m_count : nullptr;
    }

    size_t Count() const noexcept
    {
        return m_count;
    }
};

#endif // COREUTILS_H