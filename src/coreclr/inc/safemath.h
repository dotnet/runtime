// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// safemath.h
//
// overflow checking infrastructure
// ---------------------------------------------------------------------------


#ifndef SAFEMATH_H_
#define SAFEMATH_H_

// This file is included from several places outside the CLR, so we can't
// pull in files like DebugMacros.h.  However, we assume that the standard
// clrtypes (UINT32 etc.) are defined.
#include "debugmacrosext.h"

#ifndef _ASSERTE_SAFEMATH
#ifdef _ASSERTE
// Use _ASSERTE if we have it (should always be the case in the CLR)
#define _ASSERTE_SAFEMATH _ASSERTE
#else
// Otherwise (eg. we're being used from a tool like SOS) there isn't much
// we can rely on that is available everywhere.  In
// several other tools we just take the recourse of disabling asserts,
// we'll do the same here.
// Ideally we'd have a collection of common utilities available everywhere.
#define _ASSERTE_SAFEMATH(a)
#endif
#endif

#include "static_assert.h"

#ifdef PAL_STDCPP_COMPAT
#include <type_traits>
#else
#include "clr_std/type_traits"
#endif

//==================================================================
// Semantics: if val can be represented as the exact same value
// when cast to Dst type, then FitsIn<Dst>(val) will return true;
// otherwise FitsIn returns false.
//
// Dst and Src must both be integral types.
//
// It's important to note that most of the conditionals in this
// function are based on static type information and as such will
// be optimized away. In particular, the case where the signs are
// identical will result in no code branches.

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6326) // PREfast warning: Potential comparison of a constant with another constant
#endif // _PREFAST_

template <typename Dst, typename Src>
inline bool FitsIn(Src val)
{
#ifdef _MSC_VER
    static_assert_no_msg(!__is_class(Dst));
    static_assert_no_msg(!__is_class(Src));
#endif

    if (std::is_signed<Src>::value == std::is_signed<Dst>::value)
    {   // Src and Dst are equally signed
        if (sizeof(Src) <= sizeof(Dst))
        {   // No truncation is possible
            return true;
        }
        else
        {   // Truncation is possible, requiring runtime check
            return val == (Src)((Dst)val);
        }
    }
    else if (std::is_signed<Src>::value)
    {   // Src is signed, Dst is unsigned
#ifdef __GNUC__
        // Workaround for GCC warning: "comparison is always
        // false due to limited range of data type."
        if (!(val == 0 || val > 0))
#else
        if (val < 0)
#endif
        {   // A negative number cannot be represented by an unsigned type
            return false;
        }
        else
        {
            if (sizeof(Src) <= sizeof(Dst))
            {   // No truncation is possible
                return true;
            }
            else
            {   // Truncation is possible, requiring runtime check
                return val == (Src)((Dst)val);
            }
        }
    }
    else
    {   // Src is unsigned, Dst is signed
        if (sizeof(Src) < sizeof(Dst))
        {   // No truncation is possible. Note that Src is strictly
            // smaller than Dst.
            return true;
        }
        else
        {   // Truncation is possible, requiring runtime check
#ifdef __GNUC__
            // Workaround for GCC warning: "comparison is always
            // true due to limited range of data type." If in fact
            // Dst were unsigned we'd never execute this code
            // anyway.
            return ((Dst)val > 0 || (Dst)val == 0) &&
#else
            return ((Dst)val >= 0) &&
#endif
                   (val == (Src)((Dst)val));
        }
    }
}

// Requires that Dst is an integral type, and that DstMin and DstMax are the
// minimum and maximum values of that type, respectively.  Returns "true" iff
// "val" can be represented in the range [DstMin..DstMax] (allowing loss of precision, but
// not truncation).
template <INT64 DstMin, UINT64 DstMax>
inline bool FloatFitsInIntType(float val)
{
    float DstMinF = static_cast<float>(DstMin);
    float DstMaxF = static_cast<float>(DstMax);
    return DstMinF <= val && val <= DstMaxF;
}

template <INT64 DstMin, UINT64 DstMax>
inline bool DoubleFitsInIntType(double val)
{
    double DstMinD = static_cast<double>(DstMin);
    double DstMaxD = static_cast<double>(DstMax);
    return DstMinD <= val && val <= DstMaxD;
}

#ifdef _PREFAST_
#pragma warning(pop)
#endif //_PREFAST_

#define ovadd_lt(a, b, rhs) (((a) + (b) <  (rhs) ) && ((a) + (b) >= (a)))
#define ovadd_le(a, b, rhs) (((a) + (b) <= (rhs) ) && ((a) + (b) >= (a)))
#define ovadd_gt(a, b, rhs) (((a) + (b) >  (rhs) ) || ((a) + (b) < (a)))
#define ovadd_ge(a, b, rhs) (((a) + (b) >= (rhs) ) || ((a) + (b) < (a)))

#define ovadd3_gt(a, b, c, rhs) (((a) + (b) + (c) > (rhs)) || ((a) + (b) < (a)) || ((a) + (b) + (c) < (c)))


 //-----------------------------------------------------------------------------
//
// Liberally lifted from https://github.com/dcleblanc/SafeInt and modified.
//
// Modified to track an overflow bit instead of throwing exceptions.  In most
// cases the Visual C++ optimizer (Whidbey beta1 - v14.00.40607) is able to
// optimize the bool away completely.
// Note that using a sentinal value (IntMax for example) to represent overflow
// actually results in poorer code-gen.
//
// This has also been simplified significantly to remove functionality we
// don't currently want (division, implicit conversions, many additional operators etc.)
//
// Example:
//   unsafe: UINT32 bufSize = headerSize + elementCount * sizeof(void*);
//   becomes:
//      S_UINT32 bufSize = S_UINT32(headerSize) + S_UINT32(elementCount) *
//                                          S_UINT32( sizeof(void*) );
//      if( bufSize.IsOverflow() ) { <overflow-error> }
//      else { use bufSize.Value() }
//   or:
//      UINT32 tmp, bufSize;
//      if( !ClrSafeInt<UINT32>::multiply( elementCount, sizeof(void*), tmp ) ||
//              !ClrSafeInt<UINT32>::addition( tmp, headerSize, bufSize ) )
//      { <overflow-error> }
//      else { use bufSize }
//
//-----------------------------------------------------------------------------
// TODO: Any way to prevent unintended instantiations?  This is only designed to
//  work with unsigned integral types (signed types will work but we probably
//  don't need signed support).
template<typename T> class ClrSafeInt
{
public:
    // Default constructor - 0 value by default
    ClrSafeInt() :
        m_value(0),
        m_overflow(false)
        COMMA_INDEBUG( m_checkedOverflow( false ) )
    {
    }

    // Value constructor
    // This is explicit because otherwise it would be harder to
    // differentiate between checked and unchecked usage of an operator.
    // I.e. si + x + y  vs. si + ( x + y )
    //
    // Set the m_checkedOverflow bit to true since this is being initialized
    // with a constant value and we know that it is valid. A scenario in
    // which this is useful is when an overflow causes a fallback value to
    // be used:
    //      if (val.IsOverflow())
    //          val = ClrSafeInt<T>(some_value);
    explicit ClrSafeInt( T v ) :
        m_value(v),
        m_overflow(false)
        COMMA_INDEBUG( m_checkedOverflow( true ) )
    {
    }

    template <typename U>
    explicit ClrSafeInt(U u) :
        m_value(0),
        m_overflow(false)
        COMMA_INDEBUG( m_checkedOverflow( false ) )
    {
        if (!FitsIn<T>(u))
        {
            m_overflow = true;
        }
        else
        {
            m_value = (T)u;
        }
    }

    template <typename U>
    ClrSafeInt(ClrSafeInt<U> u) :
        m_value(0),
        m_overflow(false)
        COMMA_INDEBUG( m_checkedOverflow( false ) )
    {
        if (u.IsOverflow() || !FitsIn<T>(u.Value()))
        {
            m_overflow = true;
        }
        else
        {
            m_value = (T)u.Value();
    }
    }

    // Note: compiler-generated copy constructor and assignment operator
    // are correct for our purposes.

    // Note: The MS compiler will sometimes silently perform value-destroying
    // conversions when calling the operators below.
    // Eg. "ClrSafeInt<unsigned> s(0); s += int(-1);" will result in s
    // having the value 0xffffffff without generating a compile-time warning.
    // Narrowing conversions are generally level 4 warnings so may or may not
    // be visible.
    //
    // In the original SafeInt class, all operators have an
    // additional overload that takes an arbitrary type U and then safe
    // conversions are performed (resulting in overflow whenever the value
    // cannot be preserved).
    // We could do the same thing, but currently don't because:
    //  - we don't believe there are common cases where this would result in a
    //    security hole.
    //  - the extra complexity isn't worth the benefits
    //  - it would prevent compiler warnings in the cases we do get warnings for.


    // true if there has been an overflow leading up to the creation of this
    // value, false otherwise.
    // Note that in debug builds we track whether our client called this,
    // so we should not be calling this method ourselves from within this class.
    inline bool IsOverflow() const
    {
        INDEBUG( m_checkedOverflow = true; )
        return m_overflow;
    }

    // Get the value of this integer.
    // Must only be called when IsOverflow()==false.  If this is called
    // on overflow we'll assert in Debug and return 0 in release.
    inline T Value() const
    {
        _ASSERTE_SAFEMATH( m_checkedOverflow );  // Ensure our caller first checked the overflow bit
        _ASSERTE_SAFEMATH( !m_overflow );
        return m_value;
    }

    // force the value into the overflow state.
    inline void SetOverflow()
    {
        INDEBUG( this->m_checkedOverflow = false; )
        this->m_overflow = true;
        // incase someone manages to call Value in release mode - should be optimized out
        this->m_value = 0;
    }


    //
    // OPERATORS
    //

    // Addition and multiplication.  Only permitted when both sides are explicitly
    // wrapped inside of a ClrSafeInt and when the types match exactly.
    // If we permitted a RHS of type 'T', then there would be differences
    // in correctness between mathematically equivalent expressions such as
    // "si + x + y" and "si + ( x + y )".  Unfortunately, not permitting this
    // makes expressions involving constants tedius and ugly since the constants
    // must be wrapped in ClrSafeInt instances.  If we become confident that
    // our tools (PreFast) will catch all integer overflows, then we can probably
    // safely add this.
    inline ClrSafeInt<T> operator +(ClrSafeInt<T> rhs) const
    {
        ClrSafeInt<T> result;       // value is initialized to 0
        if( this->m_overflow ||
            rhs.m_overflow ||
            !addition( this->m_value, rhs.m_value, result.m_value ) )
        {
            result.m_overflow = true;
        }

        return result;
    }

    inline ClrSafeInt<T> operator -(ClrSafeInt<T> rhs) const
    {
        ClrSafeInt<T> result;       // value is initialized to 0
        if( this->m_overflow ||
            rhs.m_overflow ||
            !subtraction( this->m_value, rhs.m_value, result.m_value ) )
        {
            result.m_overflow = true;
        }

        return result;
    }

    inline ClrSafeInt<T> operator *(ClrSafeInt<T> rhs) const
    {
        ClrSafeInt<T> result;       // value is initialized to 0
        if( this->m_overflow ||
            rhs.m_overflow ||
            !multiply( this->m_value, rhs.m_value, result.m_value ) )
        {
            result.m_overflow = true;
        }

        return result;
    }

    // Accumulation operators
    // Here it's ok to have versions that take a value of type 'T', however we still
    // don't allow any mixed-type operations.
    inline ClrSafeInt<T>& operator +=(ClrSafeInt<T> rhs)
    {
        INDEBUG( this->m_checkedOverflow = false; )
        if( this->m_overflow ||
            rhs.m_overflow ||
            !ClrSafeInt<T>::addition( this->m_value, rhs.m_value, this->m_value ) )
        {
            this->SetOverflow();
        }
        return *this;
    }

    inline ClrSafeInt<T>& operator +=(T rhs)
    {
        INDEBUG( this->m_checkedOverflow = false; )
        if( this->m_overflow ||
            !ClrSafeInt<T>::addition( this->m_value, rhs, this->m_value ) )
        {
            this->SetOverflow();
        }
        return *this;
    }

    inline ClrSafeInt<T>& operator *=(ClrSafeInt<T> rhs)
    {
        INDEBUG( this->m_checkedOverflow = false; )
        if( this->m_overflow ||
            rhs.m_overflow ||
            !ClrSafeInt<T>::multiply( this->m_value, rhs.m_value, this->m_value ) )
        {
            this->SetOverflow();
        }
        return *this;
    }

    inline ClrSafeInt<T>& operator *=(T rhs)
    {
        INDEBUG( this->m_checkedOverflow = false; )
        if( this->m_overflow ||
            !ClrSafeInt<T>::multiply( this->m_value, rhs, this->m_value ) )
        {
            this->SetOverflow();
        }

        return *this;
    }

    //
    // STATIC HELPER METHODS
    //these compile down to something as efficient as macros and allow run-time testing
    //of type by the developer
    //

    template <typename U> static bool IsSigned(U)
    {
        return std::is_signed<U>::value;
    }

    static bool IsSigned()
    {
        return std::is_signed<T>::value;
    }

    static bool IsMixedSign(T lhs, T rhs)
    {
        return ((lhs ^ rhs) < 0);
    }

    static unsigned char BitCount(){return (sizeof(T)*8);}

    static bool Is64Bit(){return sizeof(T) == 8;}
    static bool Is32Bit(){return sizeof(T) == 4;}
    static bool Is16Bit(){return sizeof(T) == 2;}
    static bool Is8Bit(){return sizeof(T) == 1;}

    //both of the following should optimize away
    static T MaxInt()
    {
        if(IsSigned())
        {
            return (T)~((T)1 << (BitCount()-1));
        }
        //else
        return (T)(~(T)0);
    }

    static T MinInt()
    {
        if(IsSigned())
        {
            return (T)((T)1 << (BitCount()-1));
        }
        else
        {
            return ((T)0);
        }
    }

    // Align a value up to the nearest boundary, which must be a power of 2
    inline void AlignUp( T alignment )
    {
        _ASSERTE_SAFEMATH( IsPowerOf2( alignment ) );
        *this += (alignment - 1);
        if( !this->m_overflow )
        {
            m_value &= ~(alignment - 1);
        }
    }

    //
    // Arithmetic implementation functions
    //

    //note - this looks complex, but most of the conditionals
    //are constant and optimize away
    //for example, a signed 64-bit check collapses to:
/*
    if(lhs == 0 || rhs == 0)
        return 0;

    if(MaxInt()/+lhs < +rhs)
    {
        //overflow
        throw SafeIntException(ERROR_ARITHMETIC_OVERFLOW);
    }
    //ok
    return lhs * rhs;

    Which ought to inline nicely
*/
    // Returns true if safe, false for overflow.
    static bool multiply(T lhs, T rhs, T &result)
    {
        if(Is64Bit())
        {
            //fast track this one - and avoid DIV_0 below
            if(lhs == 0 || rhs == 0)
            {
                result = 0;
                return true;
            }

            //we're 64 bit - slow, but the only way to do it
            if(IsSigned())
            {
                if(!IsMixedSign(lhs, rhs))
                {
                    //both positive or both negative
                    //result will be positive, check for lhs * rhs > MaxInt
                    if(lhs > 0)
                    {
                        //both positive
                        if(MaxInt()/lhs < rhs)
                        {
                            //overflow
                            return false;
                        }
                    }
                    else
                    {
                        //both negative

                        //comparison gets tricky unless we force it to positive
                        //EXCEPT that -MinInt is undefined - can't be done
                        //And MinInt always has a greater magnitude than MaxInt
                        if(lhs == MinInt() || rhs == MinInt())
                        {
                            //overflow
                            return false;
                        }

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning( disable : 4146 )   // unary minus applied to unsigned is still unsigned
#endif
                        if(MaxInt()/(-lhs) < (-rhs) )
                        {
                            //overflow
                            return false;
                        }
#ifdef _MSC_VER
#pragma warning(pop)
#endif
                    }
                }
                else
                {
                    //mixed sign - this case is difficult
                    //test case is lhs * rhs < MinInt => overflow
                    //if lhs < 0 (implies rhs > 0),
                    //lhs < MinInt/rhs is the correct test
                    //else if lhs > 0
                    //rhs < MinInt/lhs is the correct test
                    //avoid dividing MinInt by a negative number,
                    //because MinInt/-1 is a corner case

                    if(lhs < 0)
                    {
                        if(lhs < MinInt()/rhs)
                        {
                            //overflow
                            return false;
                        }
                    }
                    else
                    {
                        if(rhs < MinInt()/lhs)
                        {
                            //overflow
                            return false;
                        }
                    }
                }

                //ok
                result = lhs * rhs;
                return true;
            }
            else
            {
                //unsigned, easy case
                if(MaxInt()/lhs < rhs)
                {
                    //overflow
                    return false;
                }
                //ok
                result = lhs * rhs;
                return true;
            }
        }
        else if(Is32Bit())
        {
            //we're 32-bit
            if(IsSigned())
            {
                INT64 tmp = (INT64)lhs * (INT64)rhs;

                //upper 33 bits must be the same
                //most common case is likely that both are positive - test first
                if( (tmp & 0xffffffff80000000LL) == 0 ||
                    (tmp & 0xffffffff80000000LL) == 0xffffffff80000000LL)
                {
                    //this is OK
                    result = (T)tmp;
                    return true;
                }

                //overflow
                return false;

            }
            else
            {
                UINT64 tmp = (UINT64)lhs * (UINT64)rhs;
                if (tmp & 0xffffffff00000000ULL) //overflow
                {
                    //overflow
                    return false;
                }
                result = (T)tmp;
                return true;
            }
        }
        else if(Is16Bit())
        {
            //16-bit
            if(IsSigned())
            {
                INT32 tmp = (INT32)lhs * (INT32)rhs;
                //upper 17 bits must be the same
                //most common case is likely that both are positive - test first
                if( (tmp & 0xffff8000) == 0 || (tmp & 0xffff8000) == 0xffff8000)
                {
                    //this is OK
                    result = (T)tmp;
                    return true;
                }

                //overflow
                return false;
            }
            else
            {
                UINT32 tmp = (UINT32)lhs * (UINT32)rhs;
                if (tmp & 0xffff0000) //overflow
                {
                    return false;
                }
                result = (T)tmp;
                return true;
            }
        }
        else //8-bit
        {
            _ASSERTE_SAFEMATH(Is8Bit());

            if(IsSigned())
            {
                INT16 tmp = (INT16)lhs * (INT16)rhs;
                //upper 9 bits must be the same
                //most common case is likely that both are positive - test first
                if( (tmp & 0xff80) == 0 || (tmp & 0xff80) == 0xff80)
                {
                    //this is OK
                    result = (T)tmp;
                    return true;
                }

                //overflow
                return false;
            }
            else
            {
                UINT16 tmp = ((UINT16)lhs) * ((UINT16)rhs);

                if (tmp & 0xff00) //overflow
                {
                    return false;
                }
                result = (T)tmp;
                return true;
            }
        }
    }

    // Returns true if safe, false on overflow
    static inline bool addition(T lhs, T rhs, T &result)
    {
        if(IsSigned())
        {
            //test for +/- combo
            if(!IsMixedSign(lhs, rhs))
            {
                //either two negatives, or 2 positives
#ifdef __GNUC__
                // Workaround for GCC warning: "comparison is always
                // false due to limited range of data type."
                if (!(rhs == 0 || rhs > 0))
#else
                if(rhs < 0)
#endif // __GNUC__ else
                {
                    //two negatives
                    if(lhs < (T)(MinInt() - rhs)) //remember rhs < 0
                    {
                        return false;
                    }
                    //ok
                }
                else
                {
                    //two positives
                    if((T)(MaxInt() - lhs) < rhs)
                    {
                        return false;
                    }
                    //OK
                }
            }
            //else overflow not possible
            result = lhs + rhs;
            return true;
        }
        else //unsigned
        {
            if((T)(MaxInt() - lhs) < rhs)
            {
                return false;

            }
            result = lhs + rhs;
            return true;
        }
    }

    // Returns true if safe, false on overflow
    static inline bool subtraction(T lhs, T rhs, T& result)
    {
        T tmp = lhs - rhs;

        if(IsSigned())
        {
            if(IsMixedSign(lhs, rhs)) //test for +/- combo
            {
                //mixed positive and negative
                //two cases - +X - -Y => X + Y - check for overflow against MaxInt()
                //            -X - +Y - check for overflow against MinInt()

                if(lhs >= 0) //first case
                {
                    //test is X - -Y > MaxInt()
                    //equivalent to X > MaxInt() - |Y|
                    //Y == MinInt() creates special case
                    //Even 0 - MinInt() can't be done
                    //note that the special case collapses into the general case, due to the fact
                    //MaxInt() - MinInt() == -1, and lhs is non-negative
                    //OR tmp should be GTE lhs

                    // old test - leave in for clarity
                    //if(lhs > (T)(MaxInt() + rhs)) //remember that rhs is negative
                    if(tmp < lhs)
                    {
                        return false;
                    }
                    //fall through to return value
                }
                else
                {
                    //second case
                    //test is -X - Y < MinInt()
                    //or      -X < MinInt() + Y
                    //we do not have the same issues because abs(MinInt()) > MaxInt()
                    //tmp should be LTE lhs

                    //if(lhs < (T)(MinInt() + rhs)) // old test - leave in for clarity
                    if(tmp > lhs)
                    {
                        return false;
                    }
                    //fall through to return value
                }
            }
            // else
            //both negative, or both positive
            //no possible overflow
            result = tmp;
            return true;
        }
        else
        {
            //easy unsigned case
            if(lhs < rhs)
            {
                return false;
            }
            result = tmp;
            return true;
        }
    }

private:
    // Private helper functions
    // Note that's it occasionally handy to call the arithmetic implementation
    // functions above so we leave them public, even though we almost always use
    // the operators instead.

    // True if the specified value is a power of two.
    static inline bool IsPowerOf2( T x )
    {
        // find the smallest power of 2 >= x
        T testPow = 1;
        while( testPow < x )
        {
            testPow = testPow << 1;           // advance to next power of 2
            if( testPow <= 0 )
            {
                return false;       // overflow
            }
        }

        return( testPow == x );
    }

    //
    // Instance data
    //

    // The integer value this instance represents, or 0 if overflow.
     T m_value;

    // True if overflow has been reached.  Once this is set, it cannot be cleared.
    bool m_overflow;

    // In debug builds we verify that our caller checked the overflow bit before
    // accessing the value.  This flag is cleared on initialization, and whenever
    // m_value or m_overflow changes, and set only when IsOverflow
    // is called.
    INDEBUG( mutable bool m_checkedOverflow; )
};

// Allows creation of a ClrSafeInt corresponding to the type of the argument.
template <typename T>
ClrSafeInt<T> AsClrSafeInt(T t)
{
    return ClrSafeInt<T>(t);
}

template <typename T>
ClrSafeInt<T> AsClrSafeInt(ClrSafeInt<T> t)
{
    return t;
}

// Convenience safe-integer types.  Currently these are the only types
// we are using ClrSafeInt with.  We may want to add others.
// These type names are based on our standardized names in clrtypes.h
typedef ClrSafeInt<UINT8> S_UINT8;
typedef ClrSafeInt<UINT16> S_UINT16;
//typedef ClrSafeInt<UINT32> S_UINT32;
#define S_UINT32 ClrSafeInt<UINT32>
typedef ClrSafeInt<UINT64> S_UINT64;
typedef ClrSafeInt<SIZE_T> S_SIZE_T;

 #endif // SAFEMATH_H_
