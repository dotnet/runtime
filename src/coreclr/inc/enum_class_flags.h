// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef ENUM_CLASS_FLAGS_OPERATORS
#define ENUM_CLASS_FLAGS_OPERATORS

template <typename T>
inline auto operator& (T left, T right) -> decltype(T::support_use_as_flags)
{
    return static_cast<T>(static_cast<int>(left) & static_cast<int>(right));
}

template <typename T>
inline auto operator| (T left, T right) -> decltype(T::support_use_as_flags)
{
    return static_cast<T>(static_cast<int>(left) | static_cast<int>(right));
}

template <typename T>
inline auto operator^ (T left, T right) -> decltype(T::support_use_as_flags)
{
    return static_cast<T>(static_cast<int>(left) ^ static_cast<int>(right));
}

template <typename T>
inline auto operator~ (T value) -> decltype(T::support_use_as_flags)
{
    return static_cast<T>(~static_cast<int>(value));
}

template <typename T>
inline auto operator |= (T& left, T right) -> const decltype(T::support_use_as_flags)&
{
    left = left | right;
    return left;
}

template <typename T>
inline auto operator &= (T& left, T right) -> const decltype(T::support_use_as_flags)&
{
    left = left & right;
    return left;
}

template <typename T>
inline auto operator ^= (T& left, T right) -> const decltype(T::support_use_as_flags)&
{
    left = left ^ right;
    return left;
}

template <typename T>
inline bool HasFlag(T value, decltype(T::support_use_as_flags) flag)
{
    return (value & flag) == flag;
}

#endif /* ENUM_CLASS_FLAGS_OPERATORS */