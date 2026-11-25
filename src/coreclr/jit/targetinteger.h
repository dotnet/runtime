// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef TARGETINTEGER_H
#define TARGETINTEGER_H

// A target integer is not a host address. Keep conversions explicit even when
// the host and target happen to have the same pointer size.
template <bool Signed>
class TargetInteger
{
    uint64_t m_value;

    static uint64_t Normalize(uint64_t value)
    {
        return TargetArchitecture::Is64Bit ? value : static_cast<uint32_t>(value);
    }

    using HostType = typename std::conditional<Signed, int64_t, uint64_t>::type;

    HostType Value() const
    {
        if (Signed && !TargetArchitecture::Is64Bit)
        {
            return static_cast<int32_t>(m_value);
        }
        return static_cast<HostType>(m_value);
    }

    struct Raw
    {
    };

    TargetInteger(uint64_t value, Raw)
        : m_value(Normalize(value))
    {
    }

    template <typename T, typename H>
    friend T TargetFromHost(const H& value);

    template <bool S>
    friend typename std::conditional<S, int64_t, uint64_t>::type TargetToHost(TargetInteger<S> value);

public:
    TargetInteger() = default;

    template <typename T,
              typename std::enable_if<std::is_integral<T>::value && (sizeof(T) <= sizeof(unsigned)) &&
                                          !std::is_same<T, size_t>::value && !std::is_same<T, ssize_t>::value,
                                      int>::type = 0>
    TargetInteger(T value)
        : m_value(Normalize(static_cast<uint64_t>(value)))
    {
    }

    TargetInteger operator-() const
    {
        return TargetInteger(0 - m_value, Raw{});
    }

    TargetInteger operator~() const
    {
        return TargetInteger(~m_value, Raw{});
    }

    explicit operator bool() const
    {
        return m_value != 0;
    }

#define TARGET_INTEGER_BINARY(op)                                                                                      \
    friend TargetInteger operator op(TargetInteger left, TargetInteger right)                                          \
    {                                                                                                                  \
        return TargetInteger(left.m_value op right.m_value, Raw{});                                                    \
    }                                                                                                                  \
    TargetInteger& operator op##=(TargetInteger other)                                                                 \
    {                                                                                                                  \
        return *this = *this op other;                                                                                 \
    }

    TARGET_INTEGER_BINARY(+)
    TARGET_INTEGER_BINARY(-)
    TARGET_INTEGER_BINARY(*)
    TARGET_INTEGER_BINARY(&)
    TARGET_INTEGER_BINARY(|)
    TARGET_INTEGER_BINARY(^)
#undef TARGET_INTEGER_BINARY

    friend TargetInteger operator/(TargetInteger left, TargetInteger right)
    {
        return TargetInteger(static_cast<uint64_t>(left.Value() / right.Value()), Raw{});
    }

    friend TargetInteger operator%(TargetInteger left, TargetInteger right)
    {
        return TargetInteger(static_cast<uint64_t>(left.Value() % right.Value()), Raw{});
    }

    TargetInteger& operator/=(TargetInteger other)
    {
        return *this = *this / other;
    }

    TargetInteger& operator%=(TargetInteger other)
    {
        return *this = *this % other;
    }

    friend TargetInteger operator<<(TargetInteger left, TargetInteger right)
    {
        assert(right.m_value < (TargetArchitecture::Is64Bit ? 64 : 32));
        return TargetInteger(left.m_value << right.m_value, Raw{});
    }

    friend TargetInteger operator>>(TargetInteger left, TargetInteger right)
    {
        assert(right.m_value < (TargetArchitecture::Is64Bit ? 64 : 32));
        return TargetInteger(static_cast<uint64_t>(left.Value() >> right.m_value), Raw{});
    }

    TargetInteger& operator<<=(TargetInteger other)
    {
        return *this = *this << other;
    }

    TargetInteger& operator>>=(TargetInteger other)
    {
        return *this = *this >> other;
    }

#define TARGET_INTEGER_COMPARE(op)                                                                                     \
    friend bool operator op(TargetInteger left, TargetInteger right)                                                   \
    {                                                                                                                  \
        return left.Value() op right.Value();                                                                          \
    }

    TARGET_INTEGER_COMPARE(==)
    TARGET_INTEGER_COMPARE(!=)
    TARGET_INTEGER_COMPARE(<)
    TARGET_INTEGER_COMPARE(<=)
    TARGET_INTEGER_COMPARE(>)
    TARGET_INTEGER_COMPARE(>=)
#undef TARGET_INTEGER_COMPARE
};

template <typename T, typename H>
inline T TargetFromHost(const H& value)
{
    static_assert(std::is_integral<H>::value, "Expected a host integer");
    return T(static_cast<uint64_t>(value), typename T::Raw{});
}

template <bool Signed>
inline typename std::conditional<Signed, int64_t, uint64_t>::type TargetToHost(TargetInteger<Signed> value)
{
    return value.Value();
}

template <typename T, bool Signed>
inline T TargetFromHost(TargetInteger<Signed> value)
{
    return TargetFromHost<T>(TargetToHost(value));
}

#ifdef DEBUG
inline void RunTargetIntegerTests()
{
    using Unsigned = TargetInteger<false>;
    using Signed   = TargetInteger<true>;

    static_assert(!std::is_convertible<size_t, Unsigned>::value);
    static_assert(!std::is_convertible<ssize_t, Signed>::value);
    static_assert(!std::is_convertible<Unsigned, size_t>::value);
    static_assert(!std::is_convertible<Signed, ssize_t>::value);

    unsigned bits      = TargetArchitecture::Is64Bit ? 64 : 32;
    uint64_t mask      = UINT64_MAX >> (64 - bits);
    Unsigned max       = TargetFromHost<Unsigned>(mask);
    Unsigned high      = TargetFromHost<Unsigned>(uint64_t{1} << (bits - 1));
    Signed   min       = TargetFromHost<Signed>(uint64_t{1} << (bits - 1));
    Signed   signedMax = TargetFromHost<Signed>(mask >> 1);

    assert(TargetToHost(max + 1) == 0);
    assert(TargetToHost(Unsigned{0} - 1) == mask);
    assert(TargetToHost(high * 2) == 0);
    assert(signedMax + 1 == min);
    assert(signedMax + 1 < 0);
    assert(min - 1 == signedMax);
    assert(min * 2 == 0);
    assert(!(min * 2));
    assert(-min == min);
    assert(TargetToHost(~Unsigned{0}) == mask);
    assert(TargetToHost(max >> (bits - 1)) == 1);
    assert(TargetToHost(Unsigned{1} << (bits - 1)) == TargetToHost(high));
    assert(TargetToHost(Signed{-8} >> 2) == -2);
    assert(TargetToHost(Signed{-7} / 3) == -2);
    assert(TargetToHost(Signed{-7} % 3) == -1);
    assert(Signed{-1} < Signed{0});
    assert(min < Signed{-1});
    assert(max > Unsigned{0});
    assert(TargetToHost(TargetFromHost<Unsigned>(Signed{-1})) == mask);
    assert(TargetToHost(TargetFromHost<Signed>(max)) == -1);

    Signed value = 5;
    value *= 3;
    value -= 1;
    value >>= 1;
    assert(TargetToHost(value) == 7);
    assert(TargetToHost(TargetFromHost<Unsigned>(uint64_t{1} << 32)) == (bits == 64 ? uint64_t{1} << 32 : 0));
}
#endif // DEBUG

#endif // TARGETINTEGER_H
