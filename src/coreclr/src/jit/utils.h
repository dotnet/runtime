// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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

#include "iallocator.h"
#include "cycletimer.h"

// Needed for unreached()
#include "error.h"

#ifdef _TARGET_64BIT_
#define BitScanForwardPtr BitScanForward64
#else
#define BitScanForwardPtr BitScanForward
#endif

template<typename T, int size>
unsigned ArrLen(T(&)[size]){return size;}

// return true if arg is a power of 2
template<typename T>
inline bool isPow2(T i)
{
    return (i > 0 && ((i-1)&i) == 0);
}


inline const char* dspBool(bool b)
{
    return (b) ? "true" : "false";
}

#ifdef FEATURE_CORECLR
#ifdef _CRT_ABS_DEFINED
// we don't have the full standard library
inline int64_t abs(int64_t t)
{
    return t > 0 ? t : -t;
}
#endif
#endif // FEATURE_CORECLR

template <typename T> int signum(T val) 
{
    if (val < T(0))
        return -1;
    else if (val > T(0))
        return 1;
    else 
        return 0;
}

#ifdef DEBUG
/**************************************************************************/

/* to be used as static variables - no constructors/destructors, assumes zero 
   initialized memory */

class ConfigMethodRange
{

public:
    bool contains(class ICorJitInfo* info, CORINFO_METHOD_HANDLE method);

    inline void ensureInit(const wchar_t* rangeStr)
    {
        // make sure that the memory was zero initialized
        _ASSERTE(m_inited == 0 || m_inited == 1);

        if (!m_inited)
        {
            initRanges(rangeStr);
            _ASSERTE(m_inited == 1);
        }
    }

private:
    void initRanges(__in_z LPCWSTR rangeStr);

private:
    unsigned char m_lastRange;                   // count of low-high pairs
    unsigned char m_inited;
    unsigned m_ranges[100];                      // ranges of functions to Jit (low, high pairs).  
};

#endif // DEBUG

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
    static FixedBitVect *bitVectInit(UINT size, Compiler *comp);

    // bitVectSet() - Sets the given bit
    void bitVectSet(UINT bitNum);

    // bitVectTest() - Tests the given bit
    bool bitVectTest(UINT bitNum);

    // bitVectOr() - Or in the given bit vector
    void bitVectOr(FixedBitVect *bv);

    // bitVectAnd() - And with passed in bit vector
    void bitVectAnd(FixedBitVect &bv);

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
int SimpleSprintf_s(__in_ecount(cbBufSize-(pWriteStart - pBufStart)) char * pWriteStart,
                    __in_ecount(cbBufSize) char * pBufStart, size_t cbBufSize,
                    __in_z const char * fmt, ...);

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
    ScopedSetVariable(T* pVariable, T value)
        : m_pVariable(pVariable)
    {
        m_oldValue = *m_pVariable;
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
#endif // DEBUG
    T m_oldValue;   // The old value, to restore the variable to.
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
        : m_initialized(false)
        , m_writePhase(true)
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
        m_writePhase = true;
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

    // Functions/operators to write the value. Must be in the write phase.

    PhasedVar& operator=(const T& value)
    {
#ifdef DEBUG
#ifndef LEGACY_BACKEND
        assert(m_writePhase);
#endif // !LEGACY_BACKEND
        m_initialized = true;
#endif // DEBUG
        m_value = value;
        return *this;
    }

    PhasedVar& operator&=(const T& value)
    {
#ifdef DEBUG
#ifndef LEGACY_BACKEND
        assert(m_writePhase);
#endif // !LEGACY_BACKEND
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
    bool m_writePhase;  // true if we are in the (initial) "write" phase. Once the value is read, this changes to false, and can't be changed back.
#endif // DEBUG
};

class HelperCallProperties
{
private:
    bool m_isPure       [CORINFO_HELP_COUNT];
    bool m_noThrow      [CORINFO_HELP_COUNT];
    bool m_nonNullReturn[CORINFO_HELP_COUNT];
    bool m_isAllocator  [CORINFO_HELP_COUNT];
    bool m_mutatesHeap  [CORINFO_HELP_COUNT];
    bool m_mayRunCctor  [CORINFO_HELP_COUNT];
    bool m_mayFinalize  [CORINFO_HELP_COUNT];

    void init(); 

public:
    HelperCallProperties()  { init(); }

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

    bool MayFinalize(CorInfoHelpFunc helperId)
    {
        assert(helperId > CORINFO_HELP_UNDEF);
        assert(helperId < CORINFO_HELP_COUNT);
        return m_mayFinalize[helperId];
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
        LPUTF8          m_assemblyName;
        AssemblyName*   m_next;
    };

    AssemblyName*       m_pNames;       // List of names
    IAllocator*         m_alloc;        // IAllocator to use in this class

public:

    // Take a Unicode string list of assembly names, parse it, and store it.
    AssemblyNamesList2(const wchar_t* list, __in IAllocator* alloc);

    ~AssemblyNamesList2();

    // Return 'true' if 'assemblyName' (in UTF-8 format) is in the stored list of assembly names.
    bool IsInList(LPCUTF8 assemblyName);

    // Return 'true' if the assembly name list is empty.
    bool IsEmpty()
    {
        return m_pNames == nullptr;
    }
};

#ifdef FEATURE_JIT_METHOD_PERF
// When Start() is called time is noted and when ElapsedTime
// is called we know how much time was spent in msecs.
//
class CycleCount
{
private:
    double           cps;             // cycles per second
    unsigned __int64 beginCycles;     // cycles at stop watch construction
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
    double freq;

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
unsigned            CountDigits(unsigned num, unsigned base = 10);

#endif // DEBUG

// Utility class for lists.
template<typename T>
struct ListNode 
{
    T data;
    ListNode<T>* next;

    // Create the class without using constructors.
    static ListNode<T>* Create(T value, IAllocator* alloc)
    {
        ListNode<T>* node = new (alloc) ListNode<T>;
        node->data = value;
        node->next = nullptr;
        return node;
    }
};

/*****************************************************************************
* Floating point utility class 
*/
class FloatingPointUtils {
public:

    static double convertUInt64ToDouble(unsigned __int64 u64);

    static float convertUInt64ToFloat(unsigned __int64 u64);

    static unsigned __int64 convertDoubleToUInt64(double d);
};

#endif // _UTILS_H_
