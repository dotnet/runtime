// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  Utils.h                                  XX
XX                                                                           XX
XX   Has miscellaneous utility functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifndef _UTILS_H_
#define _UTILS_H_

#include "safemath.h"
#include "clr_std/type_traits"
#include "iallocator.h"
#include "hostallocator.h"
#include "cycletimer.h"
#include "vartypesdef.h"

// Needed for unreached()
#include "error.h"

#ifdef TARGET_64BIT
#define BitScanForwardPtr BitScanForward64
#else
#define BitScanForwardPtr BitScanForward
#endif

template <typename T, int size>
inline constexpr unsigned ArrLen(T (&)[size])
{
    return size;
}

// return true if arg is a power of 2
template <typename T>
inline bool isPow2(T i)
{
    return (i > 0 && ((i - 1) & i) == 0);
}

template <typename T>
constexpr bool AreContiguous(T val1, T val2)
{
    return (val1 + 1) == val2;
}

template <typename T, typename... Ts>
constexpr bool AreContiguous(T val1, T val2, Ts... rest)
{
    return ((val1 + 1) == val2) && AreContiguous(val2, rest...);
}

// Adapter for iterators to a type that is compatible with C++11
// range-based for loops.
template <typename TIterator>
class IteratorPair
{
    TIterator m_begin;
    TIterator m_end;

public:
    IteratorPair(TIterator begin, TIterator end) : m_begin(begin), m_end(end)
    {
    }

    inline TIterator begin()
    {
        return m_begin;
    }

    inline TIterator end()
    {
        return m_end;
    }
};

template <typename TIterator>
inline IteratorPair<TIterator> MakeIteratorPair(TIterator begin, TIterator end)
{
    return IteratorPair<TIterator>(begin, end);
}

// Recursive template definition to calculate the base-2 logarithm
// of a constant value.
template <unsigned val, unsigned acc = 0>
struct ConstLog2
{
    enum
    {
        value = ConstLog2<val / 2, acc + 1>::value
    };
};

template <unsigned acc>
struct ConstLog2<0, acc>
{
    enum
    {
        value = acc
    };
};

template <unsigned acc>
struct ConstLog2<1, acc>
{
    enum
    {
        value = acc
    };
};

inline const char* dspBool(bool b)
{
    return (b) ? "true" : "false";
}

template <typename T>
int signum(T val)
{
    if (val < T(0))
    {
        return -1;
    }
    else if (val > T(0))
    {
        return 1;
    }
    else
    {
        return 0;
    }
}

#if defined(DEBUG) || defined(INLINE_DATA)

// ConfigMethodRange describes a set of methods, specified via their
// hash codes. This can be used for binary search and/or specifying an
// explicit method set.
//
// Note method hash codes are not necessarily unique. For instance
// many IL stubs may have the same hash.
//
// If range string is null or just whitespace, range includes all
// methods.
//
// Parses values as decimal numbers.
//
// Examples:
//
//  [string with just spaces] : all methods
//                   12345678 : a single method
//          12345678-23456789 : a range of methods
// 99998888 12345678-23456789 : a range of methods plus a single method

class ConfigMethodRange
{

public:
    // Default capacity
    enum
    {
        DEFAULT_CAPACITY = 50
    };

    // Does the range include this hash?
    bool Contains(unsigned hash);

    // Ensure the range string has been parsed.
    void EnsureInit(const WCHAR* rangeStr, unsigned capacity = DEFAULT_CAPACITY)
    {
        // Make sure that the memory was zero initialized
        assert(m_inited == 0 || m_inited == 1);

        if (!m_inited)
        {
            InitRanges(rangeStr, capacity);
            assert(m_inited == 1);
        }
    }

    bool IsEmpty() const
    {
        return m_lastRange == 0;
    }

    // Error checks
    bool Error() const
    {
        return m_badChar != 0;
    }

    size_t BadCharIndex() const
    {
        return m_badChar - 1;
    }

    void Dump();

private:
    struct Range
    {
        unsigned m_low;
        unsigned m_high;
    };

    void InitRanges(const WCHAR* rangeStr, unsigned capacity);

    unsigned m_entries;   // number of entries in the range array
    unsigned m_lastRange; // count of low-high pairs
    unsigned m_inited;    // 1 if range string has been parsed
    size_t   m_badChar;   // index + 1 of any bad character in range string
    Range*   m_ranges;    // ranges of functions to include
};

#endif // defined(DEBUG) || defined(INLINE_DATA)

class Compiler;

/*****************************************************************************
 * Fixed bit vector class
 */
class FixedBitVect
{
private:
    UINT bitVectSize;
    UINT bitVect[];

    // bitChunkSize() - Returns number of bits in a bitVect chunk
    static UINT bitChunkSize();

    // bitNumToBit() - Returns a bit mask of the given bit number
    static UINT bitNumToBit(UINT bitNum);

public:
    // bitVectInit() - Initializes a bit vector of a given size
    static FixedBitVect* bitVectInit(UINT size, Compiler* comp);

    // bitVectSet() - Sets the given bit
    void bitVectSet(UINT bitNum);

    // bitVectTest() - Tests the given bit
    bool bitVectTest(UINT bitNum);

    // bitVectOr() - Or in the given bit vector
    void bitVectOr(FixedBitVect* bv);

    // bitVectAnd() - And with passed in bit vector
    void bitVectAnd(FixedBitVect& bv);

    // bitVectGetFirst() - Find the first bit on and return the bit num.
    //                    Return -1 if no bits found.
    UINT bitVectGetFirst();

    // bitVectGetNext() - Find the next bit on given previous bit and return bit num.
    //                    Return -1 if no bits found.
    UINT bitVectGetNext(UINT bitNumPrev);

    // bitVectGetNextAndClear() - Find the first bit on, clear it and return it.
    //                            Return -1 if no bits found.
    UINT bitVectGetNextAndClear();
};

/******************************************************************************
 * A specialized version of sprintf_s to simplify conversion to SecureCRT
 *
 * pWriteStart -> A pointer to the first byte to which data is written.
 * pBufStart -> the start of the buffer into which the data is written.  If
 *              composing a complex string with multiple calls to sprintf, this
 *              should not change.
 * cbBufSize -> The size of the overall buffer (i.e. the size of the buffer
 *              pointed to by pBufStart).  For subsequent calls, this does not
 *              change.
 * fmt -> The format string
 * ... -> Arguments.
 *
 * returns -> number of bytes successfully written, not including the null
 *            terminator.  Calls NO_WAY on error.
 */
int SimpleSprintf_s(_In_reads_(cbBufSize - (pWriteStart - pBufStart)) char* pWriteStart,
                    _In_reads_(cbBufSize) char*                             pBufStart,
                    size_t                                                  cbBufSize,
                    _In_z_ const char*                                      fmt,
                    ...);

#ifdef DEBUG
void hexDump(FILE* dmpf, const char* name, BYTE* addr, size_t size);
#endif // DEBUG

/******************************************************************************
 * ScopedSetVariable: A simple class to set and restore a variable within a scope.
 * For example, it can be used to set a 'bool' flag to 'true' at the beginning of a
 * function and automatically back to 'false' either at the end the function, or at
 * any other return location. The variable should not be changed during the scope:
 * the destructor asserts that the value at destruction time is the same one we set.
 * Usage: ScopedSetVariable<bool> _unused_name(&variable, true);
 */
template <typename T>
class ScopedSetVariable
{
public:
    ScopedSetVariable(T* pVariable, T value) : m_pVariable(pVariable)
    {
        m_oldValue   = *m_pVariable;
        *m_pVariable = value;
        INDEBUG(m_value = value;)
    }

    ~ScopedSetVariable()
    {
        assert(*m_pVariable == m_value); // Assert that the value didn't change between ctor and dtor
        *m_pVariable = m_oldValue;
    }

private:
#ifdef DEBUG
    T m_value;      // The value we set the variable to (used for assert).
#endif              // DEBUG
    T  m_oldValue;  // The old value, to restore the variable to.
    T* m_pVariable; // Address of the variable to change
};

/******************************************************************************
 * PhasedVar: A class to represent a variable that has phases, in particular,
 * a write phase where the variable is computed, and a read phase where the
 * variable is used. Once the variable has been read, it can no longer be changed.
 * Reading the variable essentially commits everyone to using that value forever,
 * and it is assumed that subsequent changes to the variable would invalidate
 * whatever assumptions were made by the previous readers, leading to bad generated code.
 * These assumptions are asserted in DEBUG builds.
 * The phase ordering is clean for AMD64, but not for x86/ARM. So don't do the phase
 * ordering asserts for those platforms.
 */
template <typename T>
class PhasedVar
{
public:
    PhasedVar()
#ifdef DEBUG
        : m_initialized(false), m_writePhase(true)
#endif // DEBUG
    {
    }

    PhasedVar(T value)
        : m_value(value)
#ifdef DEBUG
        , m_initialized(true)
        , m_writePhase(true)
#endif // DEBUG
    {
    }

    ~PhasedVar()
    {
#ifdef DEBUG
        m_initialized = false;
        m_writePhase  = true;
#endif // DEBUG
    }

    // Read the value. Change to the read phase.
    // Marked 'const' because we don't change the encapsulated value, even though
    // we do change the write phase, which is only for debugging asserts.

    operator T() const
    {
#ifdef DEBUG
        assert(m_initialized);
        (const_cast<PhasedVar*>(this))->m_writePhase = false;
#endif // DEBUG
        return m_value;
    }

    // Mark the value as read only; explicitly change the variable to the "read" phase.
    void MarkAsReadOnly() const
    {
#ifdef DEBUG
        assert(m_initialized);
        (const_cast<PhasedVar*>(this))->m_writePhase = false;
#endif // DEBUG
    }

    // When dumping stuff we could try to read a PhasedVariable
    // This method tells us whether we should read the PhasedVariable
    bool HasFinalValue() const
    {
#ifdef DEBUG
        return (const_cast<PhasedVar*>(this))->m_writePhase == false;
#else
        return true;
#endif // DEBUG
    }

    // Functions/operators to write the value. Must be in the write phase.

    PhasedVar& operator=(const T& value)
    {
#ifdef DEBUG
        assert(m_writePhase);
        m_initialized = true;
#endif // DEBUG
        m_value = value;
        return *this;
    }

    PhasedVar& operator&=(const T& value)
    {
#ifdef DEBUG
        assert(m_writePhase);
        m_initialized = true;
#endif // DEBUG
        m_value &= value;
        return *this;
    }

    // Note: if you need more <op>= functions, you can define them here, like operator&=

    // Assign a value, but don't assert if we're not in the write phase, and
    // don't change the phase (if we're actually in the read phase, we'll stay
    // in the read phase). This is a dangerous function, and overrides the main
    // benefit of this class. Use it wisely!
    void OverrideAssign(const T& value)
    {
#ifdef DEBUG
        m_initialized = true;
#endif // DEBUG
        m_value = value;
    }

    // We've decided that this variable can go back to write phase, even if it has been
    // written. This can be used, for example, for variables set and read during frame
    // layout calculation, as long as it is before final layout, such that anything
    // being calculated is just an estimate anyway. Obviously, it must be used carefully,
    // since it overrides the main benefit of this class.
    void ResetWritePhase()
    {
#ifdef DEBUG
        m_writePhase = true;
#endif // DEBUG
    }

private:
    // Don't allow a copy constructor. (This could be allowed, but only add it once it is actually needed.)

    PhasedVar(const PhasedVar& o)
    {
        unreached();
    }

    T m_value;
#ifdef DEBUG
    bool m_initialized; // true once the variable has been initialized, that is, written once.
    bool m_writePhase;  // true if we are in the (initial) "write" phase. Once the value is read, this changes to false,
                        // and can't be changed back.
#endif                  // DEBUG
};

class HelperCallProperties
{
private:
    bool m_isPure[CORINFO_HELP_COUNT];
    bool m_noThrow[CORINFO_HELP_COUNT];
    bool m_alwaysThrow[CORINFO_HELP_COUNT];
    bool m_nonNullReturn[CORINFO_HELP_COUNT];
    bool m_isAllocator[CORINFO_HELP_COUNT];
    bool m_mutatesHeap[CORINFO_HELP_COUNT];
    bool m_mayRunCctor[CORINFO_HELP_COUNT];

    void init();

public:
    HelperCallProperties()
    {
        init();
    }

    bool IsPure(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_isPure[helperId];
    }

    bool NoThrow(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_noThrow[helperId];
    }

    bool AlwaysThrow(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_alwaysThrow[helperId];
    }

    bool NonNullReturn(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_nonNullReturn[helperId];
    }

    bool IsAllocator(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_isAllocator[helperId];
    }

    bool MutatesHeap(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_mutatesHeap[helperId];
    }

    bool MayRunCctor(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_mayRunCctor[helperId];
    }
};

//*****************************************************************************
// AssemblyNamesList2: Parses and stores a list of Assembly names, and provides
// a function for determining whether a given assembly name is part of the list.
//
// This is a clone of the AssemblyNamesList class that exists in the VM's utilcode,
// modified to use the JIT's memory allocator and throw on out of memory behavior.
// It is named AssemblyNamesList2 to avoid a name conflict with the VM version.
// It might be preferable to adapt the VM's code to be more flexible (for example,
// by using an IAllocator), but the string handling code there is heavily macroized,
// and for the small usage we have of this class, investing in genericizing the VM
// implementation didn't seem worth it.
//*****************************************************************************

class AssemblyNamesList2
{
    struct AssemblyName
    {
        char*         m_assemblyName;
        AssemblyName* m_next;
    };

    AssemblyName* m_pNames; // List of names
    HostAllocator m_alloc;  // HostAllocator to use in this class

public:
    // Take a Unicode string list of assembly names, parse it, and store it.
    AssemblyNamesList2(const WCHAR* list, HostAllocator alloc);

    ~AssemblyNamesList2();

    // Return 'true' if 'assemblyName' (in UTF-8 format) is in the stored list of assembly names.
    bool IsInList(const char* assemblyName);

    // Return 'true' if the assembly name list is empty.
    bool IsEmpty()
    {
        return m_pNames == nullptr;
    }
};

// MethodSet: Manage a list of methods that is read from a file.
//
// Methods are approximately in the format output by JitFunctionTrace, e.g.:
//
//     System.CLRConfig:GetBoolValue(ref,byref):bool (MethodHash=3c54d35e)
//       -- use the MethodHash, not the method name
//
//     System.CLRConfig:GetBoolValue(ref,byref):bool
//       -- use just the name
//
// Method names should not have any leading whitespace.
//
// TODO: Should this be more related to JitConfigValues::MethodSet?
//
class MethodSet
{
    // TODO: use a hash table? or two: one on hash value, one on function name
    struct MethodInfo
    {
        char*       m_MethodName;
        int         m_MethodHash;
        MethodInfo* m_next;

        MethodInfo(char* methodName, int methodHash)
            : m_MethodName(methodName), m_MethodHash(methodHash), m_next(nullptr)
        {
        }
    };

    MethodInfo*   m_pInfos; // List of function info
    HostAllocator m_alloc;  // HostAllocator to use in this class

public:
    // Take a Unicode string with the filename containing a list of function names, parse it, and store it.
    MethodSet(const WCHAR* filename, HostAllocator alloc);

    ~MethodSet();

    // Return 'true' if 'functionName' (in UTF-8 format) is in the stored set of assembly names.
    bool IsInSet(const char* functionName);

    // Return 'true' if 'functionHash' (in UTF-8 format) is in the stored set of assembly names.
    bool IsInSet(int functionHash);

    // Return 'true' if this method is active. Prefer non-zero methodHash for check over (non-null) methodName.
    bool IsActiveMethod(const char* methodName, int methodHash);

    // Return 'true' if the assembly name set is empty.
    bool IsEmpty()
    {
        return m_pInfos == nullptr;
    }
};

#ifdef FEATURE_JIT_METHOD_PERF
// When Start() is called time is noted and when ElapsedTime
// is called we know how much time was spent in msecs.
//
class CycleCount
{
private:
    double           cps;         // cycles per second
    unsigned __int64 beginCycles; // cycles at stop watch construction
public:
    CycleCount();

    // Kick off the counter, and if re-entrant will use the latest cycles as starting point.
    // If the method returns false, any other query yield unpredictable results.
    bool Start();

    // Return time elapsed in msecs, if Start returned true.
    double ElapsedTime();

private:
    // Return true if successful.
    bool GetCycles(unsigned __int64* time);
};

// Uses win API QueryPerformanceCounter/QueryPerformanceFrequency.
class PerfCounter
{
    LARGE_INTEGER beg;
    double        freq;

public:
    // If the method returns false, any other query yield unpredictable results.
    bool Start();

    // Return time elapsed from start in millis, if Start returned true.
    double ElapsedTime();
};

#endif // FEATURE_JIT_METHOD_PERF

#ifdef DEBUG

/*****************************************************************************
 * Return the number of digits in a number of the given base (default base 10).
 * Used when outputting strings.
 */
unsigned CountDigits(unsigned num, unsigned base = 10);
unsigned CountDigits(double num, unsigned base = 10);

#endif // DEBUG

/*****************************************************************************
* Floating point utility class
*/
class FloatingPointUtils
{
public:
    static double convertUInt64ToDouble(unsigned __int64 u64);

    static float convertUInt64ToFloat(unsigned __int64 u64);

    static unsigned __int64 convertDoubleToUInt64(double d);

    static double round(double x);

    static float round(float x);

    static bool isNormal(double x);

    static bool isNormal(float x);

    static bool hasPreciseReciprocal(double x);

    static bool hasPreciseReciprocal(float x);

    static double infinite_double();

    static float infinite_float();

    static bool isNegative(float val);

    static bool isNegative(double val);

    static bool isNaN(float val);

    static bool isNaN(double val);

    static double maximum(double val1, double val2);

    static float maximum(float val1, float val2);

    static double minimum(double val1, double val2);

    static float minimum(float val1, float val2);
};

// The CLR requires that critical section locks be initialized via its ClrCreateCriticalSection API...but
// that can't be called until the CLR is initialized. If we have static data that we'd like to protect by a
// lock, and we have a statically allocated lock to protect that data, there's an issue in how to initialize
// that lock. We could insert an initialize call in the startup path, but one might prefer to keep the code
// more local. For such situations, CritSecObject solves the initialization problem, via a level of
// indirection. A pointer to the lock is initially null, and when we query for the lock pointer via "Val()".
// If the lock has not yet been allocated, this allocates one (here a leaf lock), and uses a
// CompareAndExchange-based lazy-initialization to update the field. If this fails, the allocated lock is
// destroyed. This will work as long as the first locking attempt occurs after enough CLR initialization has
// happened to make ClrCreateCriticalSection calls legal.

class CritSecObject
{
public:
    CritSecObject()
    {
        m_pCs = nullptr;
    }

    CRITSEC_COOKIE Val()
    {
        if (m_pCs == nullptr)
        {
            // CompareExchange-based lazy init.
            CRITSEC_COOKIE newCs    = ClrCreateCriticalSection(CrstLeafLock, CRST_DEFAULT);
            CRITSEC_COOKIE observed = InterlockedCompareExchangeT(&m_pCs, newCs, NULL);
            if (observed != nullptr)
            {
                ClrDeleteCriticalSection(newCs);
            }
        }
        return m_pCs;
    }

private:
    // CRITSEC_COOKIE is an opaque pointer type.
    CRITSEC_COOKIE m_pCs;

    // No copying or assignment allowed.
    CritSecObject(const CritSecObject&) = delete;
    CritSecObject& operator=(const CritSecObject&) = delete;
};

// Stack-based holder for a critial section lock.
// Ensures lock is released.

class CritSecHolder
{
public:
    CritSecHolder(CritSecObject& critSec) : m_CritSec(critSec)
    {
        ClrEnterCriticalSection(m_CritSec.Val());
    }

    ~CritSecHolder()
    {
        ClrLeaveCriticalSection(m_CritSec.Val());
    }

private:
    CritSecObject& m_CritSec;

    // No copying or assignment allowed.
    CritSecHolder(const CritSecHolder&) = delete;
    CritSecHolder& operator=(const CritSecHolder&) = delete;
};

namespace MagicDivide
{
uint32_t GetUnsigned32Magic(
    uint32_t d, bool* increment /*out*/, int* preShift /*out*/, int* postShift /*out*/, unsigned bits);
#ifdef TARGET_64BIT
uint64_t GetUnsigned64Magic(
    uint64_t d, bool* increment /*out*/, int* preShift /*out*/, int* postShift /*out*/, unsigned bits);
#endif
int32_t GetSigned32Magic(int32_t d, int* shift /*out*/);
#ifdef TARGET_64BIT
int64_t GetSigned64Magic(int64_t d, int* shift /*out*/);
#endif
}

//
// Profiling helpers
//

double CachedCyclesPerSecond();

template <typename T>
bool FitsIn(var_types type, T value)
{
    static_assert_no_msg((std::is_same<T, int32_t>::value || std::is_same<T, int64_t>::value ||
                          std::is_same<T, uint32_t>::value || std::is_same<T, uint64_t>::value));

    switch (type)
    {
        case TYP_BYTE:
            return FitsIn<int8_t>(value);
        case TYP_BOOL:
        case TYP_UBYTE:
            return FitsIn<uint8_t>(value);
        case TYP_SHORT:
            return FitsIn<int16_t>(value);
        case TYP_USHORT:
            return FitsIn<uint16_t>(value);
        case TYP_INT:
            return FitsIn<int32_t>(value);
        case TYP_UINT:
            return FitsIn<uint32_t>(value);
        case TYP_LONG:
            return FitsIn<int64_t>(value);
        case TYP_ULONG:
            return FitsIn<uint64_t>(value);
        default:
            unreached();
    }
}

namespace CheckedOps
{
const bool Unsigned = true;
const bool Signed   = false;

// Important note: templated functions below must use dynamic "assert"s instead of "static_assert"s
// because they can be instantiated on code paths that are not reachable at runtime, but visible
// to the compiler. One example is VN's EvalOp<T> function, which can be instantiated with "size_t"
// for some operators, and that's legal, but its callee EvalOpSpecialized<T> uses "assert(!AddOverflows(v1, v2))"
// for VNF_ADD_OVF/UN, and would like to continue doing so without casts.

template <class T>
bool AddOverflows(T x, T y, bool unsignedAdd)
{
    typedef typename std::make_unsigned<T>::type UT;
    assert((std::is_same<T, int32_t>::value || std::is_same<T, int64_t>::value));

    if (unsignedAdd)
    {
        return (ClrSafeInt<UT>(static_cast<UT>(x)) + ClrSafeInt<UT>(static_cast<UT>(y))).IsOverflow();
    }
    else
    {
        return (ClrSafeInt<T>(x) + ClrSafeInt<T>(y)).IsOverflow();
    }
}

template <class T>
bool SubOverflows(T x, T y, bool unsignedSub)
{
    typedef typename std::make_unsigned<T>::type UT;
    assert((std::is_same<T, int32_t>::value || std::is_same<T, int64_t>::value));

    if (unsignedSub)
    {
        return (ClrSafeInt<UT>(static_cast<UT>(x)) - ClrSafeInt<UT>(static_cast<UT>(y))).IsOverflow();
    }
    else
    {
        return (ClrSafeInt<T>(x) - ClrSafeInt<T>(y)).IsOverflow();
    }
}

template <class T>
bool MulOverflows(T x, T y, bool unsignedMul)
{
    typedef typename std::make_unsigned<T>::type UT;
    assert((std::is_same<T, int32_t>::value || std::is_same<T, int64_t>::value));

    if (unsignedMul)
    {
        return (ClrSafeInt<UT>(static_cast<UT>(x)) * ClrSafeInt<UT>(static_cast<UT>(y))).IsOverflow();
    }
    else
    {
        return (ClrSafeInt<T>(x) * ClrSafeInt<T>(y)).IsOverflow();
    }
}

bool CastFromIntOverflows(int32_t fromValue, var_types toType, bool fromUnsigned);
bool CastFromLongOverflows(int64_t fromValue, var_types toType, bool fromUnsigned);
bool CastFromFloatOverflows(float fromValue, var_types toType);
bool CastFromDoubleOverflows(double fromValue, var_types toType);
}

#endif // _UTILS_H_
