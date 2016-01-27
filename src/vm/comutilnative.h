// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

/*============================================================
**
** Header:  COMUtilNative
** 
**
** Purpose: A dumping ground for classes which aren't large
** enough to get their own file in the VM.
**
**
===========================================================*/
#ifndef _COMUTILNATIVE_H_
#define _COMUTILNATIVE_H_

#include "object.h"
#include "util.hpp"
#include "cgensys.h"
#include "fcall.h"
#include "qcall.h"
#include "windows.h"
#undef GetCurrentTime

#ifndef FEATURE_CORECLR
#include <winnls.h>
#endif

#ifdef  FEATURE_RANDOMIZED_STRING_HASHING
#pragma warning(push)
#pragma warning(disable:4324)
#if !defined(CROSS_COMPILE) && defined(_TARGET_ARM_) && !defined(PLATFORM_UNIX)
#include "arm_neon.h"
#endif
#include "marvin32.h"
#pragma warning(pop)
#endif

//
//
// PARSE NUMBERS
//
//

#define MinRadix 2
#define MaxRadix 36

class ParseNumbers {

    enum FmtFlags {
      LeftAlign = 0x1,  //Ensure that these conform to the values specified in the managed files.
      CenterAlign = 0x2,
      RightAlign = 0x4,
      PrefixSpace = 0x8,
      PrintSign = 0x10,
      PrintBase = 0x20,
      TreatAsUnsigned = 0x10,
      PrintAsI1 = 0x40,
      PrintAsI2 = 0x80,
      PrintAsI4 = 0x100,
      PrintRadixBase = 0x200,
      AlternateForm = 0x400};

public:

    static INT32 GrabInts(const INT32 radix, __in_ecount(length) WCHAR *buffer, const int length, int *i, BOOL isUnsigned);
    static INT64 GrabLongs(const INT32 radix, __in_ecount(length) WCHAR *buffer, const int length, int *i, BOOL isUnsigned);    

    static FCDECL5(LPVOID, IntToString, INT32 l, INT32 radix, INT32 width, CLR_CHAR paddingChar, INT32 flags);
    static FCDECL5_VII(LPVOID, LongToString, INT64 l, INT32 radix, INT32 width, CLR_CHAR paddingChar, INT32 flags);
    static FCDECL4(INT32, StringToInt, StringObject * s, INT32 radix, INT32 flags, INT32* currPos);
    static FCDECL4(INT64, StringToLong, StringObject * s, INT32 radix, INT32 flags, INT32* currPos);
};

//
//
// EXCEPTION NATIVE
//
//

void FreeExceptionData(ExceptionData *pedata);

class ExceptionNative
{
private:
    enum ExceptionMessageKind {
        ThreadAbort = 1,
        ThreadInterrupted = 2,
        OutOfMemory = 3
    };

public:
    static FCDECL1(FC_BOOL_RET, IsImmutableAgileException, Object* pExceptionUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsTransient, INT32 hresult);
    static FCDECL3(StringObject *, StripFileInfo, Object *orefExcepUNSAFE, StringObject *orefStrUNSAFE, CLR_BOOL isRemoteStackTrace);
    static void QCALLTYPE GetMessageFromNativeResources(ExceptionMessageKind kind, QCall::StringHandleOnStack retMesg);
#if defined(FEATURE_EXCEPTIONDISPATCHINFO)
    static FCDECL0(VOID, PrepareForForeignExceptionRaise);
    static FCDECL1(Object*, CopyStackTrace, Object* pStackTraceUNSAFE);
    static FCDECL1(Object*, CopyDynamicMethods, Object* pDynamicMethodsUNSAFE);
    static FCDECL3(VOID, GetStackTracesDeepCopy, Object* pExceptionObjectUnsafe, Object **pStackTraceUnsafe, Object **pDynamicMethodsUnsafe);
    static FCDECL3(VOID, SaveStackTracesFromDeepCopy, Object* pExceptionObjectUnsafe, Object *pStackTraceUnsafe, Object *pDynamicMethodsUnsafe);
#endif // defined(FEATURE_EXCEPTIONDISPATCHINFO)


    // NOTE: caller cleans up any partially initialized BSTRs in pED
    static void      GetExceptionData(OBJECTREF, ExceptionData *);

    // Note: these are on the PInvoke class to hide these from the user.
    static FCDECL0(EXCEPTION_POINTERS*, GetExceptionPointers);
    static FCDECL0(INT32, GetExceptionCode);
};


//
// Buffer
//
class Buffer {
public:

    // BlockCopy
    // This method from one primitive array to another based
    //      upon an offset into each an a byte count.
    static FCDECL5(VOID, BlockCopy, ArrayBase *src, int srcOffset, ArrayBase *dst, int dstOffset, int count);
    static FCDECL5(VOID, InternalBlockCopy, ArrayBase *src, int srcOffset, ArrayBase *dst, int dstOffset, int count);
    static FCDECL2(FC_UINT8_RET, GetByte, ArrayBase *arrayUNSAFE, INT32 index);
    static FCDECL3(VOID, SetByte, ArrayBase *arrayUNSAFE, INT32 index, UINT8 bData);
    static FCDECL1(FC_BOOL_RET, IsPrimitiveTypeArray, ArrayBase *arrayUNSAFE);
    static FCDECL1(INT32, ByteLength, ArrayBase *arrayUNSAFE);

    static void QCALLTYPE MemMove(void *dst, void *src, size_t length);
};

#define MIN_GC_MEMORYPRESSURE_THRESHOLD 100000
#define RELATIVE_GC_RATIO 8

const UINT NEW_PRESSURE_COUNT = 4;

class GCInterface {
private:

    static MethodDesc *m_pCacheMethod;
    static UINT64   m_ulMemPressure;
    static UINT64   m_ulThreshold;
    static INT32    m_gc_counts[3];

    static UINT64   m_addPressure[NEW_PRESSURE_COUNT];
    static UINT64   m_remPressure[NEW_PRESSURE_COUNT];
    static UINT     m_iteration;
    
public:
    static CrstStatic m_MemoryPressureLock;

    static FORCEINLINE UINT64 InterlockedAdd(UINT64 *pAugend, UINT64 addend);
    static FORCEINLINE UINT64 InterlockedSub(UINT64 *pMinuend, UINT64 subtrahend);

    static FCDECL0(int,     GetGcLatencyMode);
    static FCDECL1(int,     SetGcLatencyMode, int newLatencyMode);
    static FCDECL0(int,     GetLOHCompactionMode);
    static FCDECL1(void,    SetLOHCompactionMode, int newLOHCompactionyMode);
    static FCDECL2(FC_BOOL_RET, RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage);
    static FCDECL0(FC_BOOL_RET, CancelFullGCNotification);
    static FCDECL1(int,     WaitForFullGCApproach, int millisecondsTimeout);
    static FCDECL1(int,     WaitForFullGCComplete, int millisecondsTimeout);
    static FCDECL1(int,     GetGenerationWR, LPVOID handle);
    static FCDECL1(int,     GetGeneration, Object* objUNSAFE);

    static 
    INT64 QCALLTYPE GetTotalMemory();

    static 
    void QCALLTYPE Collect(INT32 generation, INT32 mode);

    static
    void QCALLTYPE WaitForPendingFinalizers();

    static FCDECL0(int,     GetMaxGeneration);
    static FCDECL1(void,    KeepAlive, Object *obj);
    static FCDECL1(void,    SuppressFinalize, Object *obj);
    static FCDECL1(void,    ReRegisterForFinalize, Object *obj);
    static FCDECL2(int,     CollectionCount, INT32 generation, INT32 getSpecialGCCount);
    
    static 
    int QCALLTYPE StartNoGCRegion(INT64 totalSize, BOOL lohSizeKnown, INT64 lohSize, BOOL disallowFullBlockingGC);

    static 
    int QCALLTYPE EndNoGCRegion();

    static
    void QCALLTYPE _AddMemoryPressure(UINT64 bytesAllocated);
    
    static
    void QCALLTYPE _RemoveMemoryPressure(UINT64 bytesAllocated);

    static void RemoveMemoryPressure(UINT64 bytesAllocated);
    static void AddMemoryPressure(UINT64 bytesAllocated);
    NOINLINE static void SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated);
    static void SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated);

    // New less sensitive implementation of Add/RemoveMemoryPressure:
    static void CheckCollectionCount();
    static void NewRemoveMemoryPressure(UINT64 bytesAllocated);
    static void NewAddMemoryPressure(UINT64 bytesAllocated);

private:
    // Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
    NOINLINE static void GarbageCollectModeAny(int generation);
};

class COMInterlocked
{
public:
        static FCDECL2(INT32, Exchange, INT32 *location, INT32 value);
        static FCDECL2_IV(INT64,   Exchange64, INT64 *location, INT64 value);
        static FCDECL2(LPVOID, ExchangePointer, LPVOID* location, LPVOID value);
        static FCDECL3(INT32, CompareExchange,        INT32* location, INT32 value, INT32 comparand);
        static FCDECL4(INT32, CompareExchangeReliableResult,        INT32* location, INT32 value, INT32 comparand, CLR_BOOL* succeeded);
        static FCDECL3_IVV(INT64, CompareExchange64,        INT64* location, INT64 value, INT64 comparand);
        static FCDECL3(LPVOID, CompareExchangePointer, LPVOID* location, LPVOID value, LPVOID comparand);
        static FCDECL2_IV(float, ExchangeFloat, float *location, float value);
        static FCDECL2_IV(double, ExchangeDouble, double *location, double value);
        static FCDECL3_IVV(float, CompareExchangeFloat, float *location, float value, float comparand);
        static FCDECL3_IVV(double, CompareExchangeDouble, double *location, double value, double comparand);
        static FCDECL2(LPVOID, ExchangeObject, LPVOID* location, LPVOID value);
        static FCDECL3(LPVOID, CompareExchangeObject, LPVOID* location, LPVOID value, LPVOID comparand);
        static FCDECL2(INT32, ExchangeAdd32, INT32 *location, INT32 value);
        static FCDECL2_IV(INT64, ExchangeAdd64, INT64 *location, INT64 value);
        static FCDECL2_VV(void, ExchangeGeneric, FC_TypedByRef location, FC_TypedByRef value);
        static FCDECL3_VVI(void, CompareExchangeGeneric, FC_TypedByRef location, FC_TypedByRef value, LPVOID comparand);
};

class ManagedLoggingHelper {

public:
    static FCDECL6(INT32, GetRegistryLoggingValues, CLR_BOOL* bLoggingEnabled, CLR_BOOL* bLogToConsole, INT32 *bLogLevel, CLR_BOOL* bPerfWarnings, CLR_BOOL* bCorrectnessWarnings, CLR_BOOL* bSafeHandleStackTraces);
};

class ValueTypeHelper {
public:
    static FCDECL1(FC_BOOL_RET, CanCompareBits, Object* obj);
    static FCDECL2(FC_BOOL_RET, FastEqualsCheck, Object* obj1, Object* obj2);
    static FCDECL1(INT32, GetHashCode, Object* objRef);
    static FCDECL1(INT32, GetHashCodeOfPtr, LPVOID ptr);
};

#ifndef FEATURE_CORECLR
class SizedRefHandle
{
public:
    static FCDECL1(OBJECTHANDLE,    Initialize, Object* _obj);
    static FCDECL1(VOID,            Free, OBJECTHANDLE handle);
    static FCDECL1(LPVOID,          GetTarget, OBJECTHANDLE handle);
    static FCDECL1(INT64,           GetApproximateSize, OBJECTHANDLE handle);
};

typedef BOOL (*PFN_IS_NLS_DEFINED_STRING)(NLS_FUNCTION, DWORD, LPNLSVERSIONINFO, LPCWSTR, INT);
typedef INT (*PFN_COMPARE_STRING_EX)(LPCWSTR, DWORD, LPCWSTR, INT, LPCWSTR, INT, LPNLSVERSIONINFO, LPVOID, LPARAM);
typedef INT (*PFN_LC_MAP_STRING_EX)(LPCWSTR, DWORD, LPCWSTR, INT, LPWSTR, INT, LPNLSVERSIONINFO, LPVOID, LPARAM);
typedef INT (*PFN_FIND_NLS_STRING_EX)(LPCWSTR, DWORD, LPCWSTR, INT, LPCWSTR, INT, LPINT, LPNLSVERSIONINFO, LPVOID, LPARAM);
typedef INT (*PFN_COMPARE_STRING_ORDINAL)(LPCWSTR, INT, LPCWSTR, INT, BOOL);
typedef BOOL (*PFN_GET_NLS_VERSION_EX)(NLS_FUNCTION, LPCWSTR, LPNLSVERSIONINFOEX);
typedef INT (*PFN_FIND_STRING_ORDINAL)(DWORD, LPCWSTR, INT, LPCWSTR, INT, BOOL);

class COMNlsCustomSortLibrary {
public:
    PFN_IS_NLS_DEFINED_STRING pIsNLSDefinedString;
    PFN_COMPARE_STRING_EX pCompareStringEx;
    PFN_LC_MAP_STRING_EX pLCMapStringEx;
    PFN_FIND_NLS_STRING_EX pFindNLSStringEx;
    PFN_COMPARE_STRING_ORDINAL pCompareStringOrdinal;
    PFN_GET_NLS_VERSION_EX pGetNLSVersionEx;
    PFN_FIND_STRING_ORDINAL pFindStringOrdinal;
};
#endif //!FEATURE_CORECLR

typedef const BYTE  * PCBYTE;

class COMNlsHashProvider {
public:
    COMNlsHashProvider();

    INT32 HashString(LPCWSTR szStr, SIZE_T strLen, BOOL forceRandomHashing, INT64 additionalEntropy);
    INT32 HashSortKey(PCBYTE pSrc, SIZE_T cbSrc, BOOL forceRandomHashing, INT64 additionalEntropy);
    INT32 HashiStringKnownLower80(LPCWSTR lpszStr, INT32 strLen, BOOL forceRandomHashing, INT64 additionalEntropy);

#ifdef FEATURE_CORECLR
    static COMNlsHashProvider s_NlsHashProvider;
#endif // FEATURE_CORECLR

#ifdef  FEATURE_RANDOMIZED_STRING_HASHING
    void SetUseRandomHashing(BOOL useRandomHashing) { LIMITED_METHOD_CONTRACT; bUseRandomHashing = useRandomHashing; }
    BOOL GetUseRandomHashing() { LIMITED_METHOD_CONTRACT; return bUseRandomHashing; }


private:
    BOOL bUseRandomHashing;
    PBYTE pEntropy;
    PCSYMCRYPT_MARVIN32_EXPANDED_SEED pDefaultSeed;

    PCBYTE GetEntropy();
    PCSYMCRYPT_MARVIN32_EXPANDED_SEED GetDefaultSeed();
    void InitializeDefaultSeed();
    void CreateMarvin32Seed(INT64 additionalEntropy, PSYMCRYPT_MARVIN32_EXPANDED_SEED pExpandedMarvinSeed);
#endif // FEATURE_RANDOMIZED_STRING_HASHING
};

#ifdef FEATURE_COREFX_GLOBALIZATION
class CoreFxGlobalization {
public:
  static INT32 QCALLTYPE HashSortKey(PCBYTE pSortKey, INT32 cbSortKey, BOOL forceRandomizedHashing, INT64 additionalEntropy);
};
#endif // FEATURE_COREFX_GLOBALIZATION

class StreamNative {
public:
    static FCDECL1(FC_BOOL_RET, HasOverriddenBeginEndRead, Object *stream);
    static FCDECL1(FC_BOOL_RET, HasOverriddenBeginEndWrite, Object *stream);
};

#endif // _COMUTILNATIVE_H_
