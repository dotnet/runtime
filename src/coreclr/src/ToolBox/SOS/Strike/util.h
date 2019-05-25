// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#ifndef __util_h__
#define __util_h__

#define LIMITED_METHOD_CONTRACT

// So we can use the PAL_TRY_NAKED family of macros without dependencies on utilcode.
inline void RestoreSOToleranceState() {}

#include <cor.h>
#include <corsym.h>
#include <clrdata.h>
#include <palclr.h>
#include <metahost.h>
#include <new>

#if !defined(FEATURE_PAL)
#include <dia2.h>
#endif

#ifdef STRIKE
#if defined(_MSC_VER)
#pragma warning(disable:4200)
#pragma warning(default:4200)
#endif
#include "data.h"
#endif //STRIKE

#include "cordebug.h"
#include "static_assert.h"

typedef LPCSTR  LPCUTF8;
typedef LPSTR   LPUTF8;

DECLARE_HANDLE(OBJECTHANDLE);

struct IMDInternalImport;

#if defined(_TARGET_WIN64_)
#define WIN64_8SPACES ""
#define WIN86_8SPACES "        "
#define POINTERSIZE "16"
#define POINTERSIZE_HEX 16
#define POINTERSIZE_BYTES 8
#define POINTERSIZE_TYPE "I64"
#else
#define WIN64_8SPACES "        "
#define WIN86_8SPACES ""
#define POINTERSIZE "8"
#define POINTERSIZE_HEX 8
#define POINTERSIZE_BYTES 4
#define POINTERSIZE_TYPE "I32"
#endif

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE POINTERSIZE_BYTES
#endif // TARGET_POINTER_SIZE

#if defined(_MSC_VER)
#pragma warning(disable:4510 4512 4610)
#endif

#ifndef _ASSERTE
#ifdef _DEBUG
#define _ASSERTE(expr)         \
    do { if (!(expr) ) { ExtErr("_ASSERTE fired:\n\t%s\n", #expr); if (IsDebuggerPresent()) DebugBreak(); } } while (0)
#else
#define _ASSERTE(x)
#endif
#endif // ASSERTE

#ifdef _DEBUG
#define ASSERT_CHECK(expr, msg, reason)         \
        do { if (!(expr) ) { ExtOut(reason); ExtOut(msg); ExtOut(#expr); DebugBreak(); } } while (0)
#endif

// The native symbol reader dll name
#if defined(_AMD64_)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.amd64.dll")
#elif defined(_X86_)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.x86.dll")
#elif defined(_ARM_)
#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.arm.dll")
#elif defined(_ARM64_)
// Use diasymreader until the package has an arm64 version - issue #7360
//#define NATIVE_SYMBOL_READER_DLL W("Microsoft.DiaSymReader.Native.arm64.dll")
#define NATIVE_SYMBOL_READER_DLL W("diasymreader.dll")
#endif

// PREFIX macros - Begin

// SOS does not have support for Contracts.  Therefore we needed to duplicate
// some of the PREFIX infrastructure from inc\check.h in here.

// Issue - PREFast_:510  v4.51 does not support __assume(0)
#if (defined(_MSC_VER) && !defined(_PREFAST_)) || defined(_PREFIX_)
#if defined(_AMD64_)
// Empty methods that consist of UNREACHABLE() result in a zero-sized declspec(noreturn) method
// which causes the pdb file to make the next method declspec(noreturn) as well, thus breaking BBT
// Remove when we get a VC compiler that fixes VSW 449170
# define __UNREACHABLE() DebugBreak(); __assume(0);
#else
# define __UNREACHABLE() __assume(0)
#endif
#else
#define __UNREACHABLE()  do { } while(true)
#endif


#if defined(_PREFAST_) || defined(_PREFIX_) 
#define COMPILER_ASSUME_MSG(_condition, _message) if (!(_condition)) __UNREACHABLE();
#else

#if defined(DACCESS_COMPILE)
#define COMPILER_ASSUME_MSG(_condition, _message) do { } while (0)
#else

#if defined(_DEBUG)
#define COMPILER_ASSUME_MSG(_condition, _message) \
    ASSERT_CHECK(_condition, _message, "Compiler optimization assumption invalid")
#else
#define COMPILER_ASSUME_MSG(_condition, _message) __assume(_condition)
#endif // _DEBUG

#endif // DACCESS_COMPILE

#endif // _PREFAST_ || _PREFIX_

#define PREFIX_ASSUME(_condition) \
    COMPILER_ASSUME_MSG(_condition, "")

// PREFIX macros - End

class MethodTable;

#define MD_NOT_YET_LOADED ((DWORD_PTR)-1)
/*
 * HANDLES
 *
 * The default type of handle is a strong handle.
 *
 */
#define HNDTYPE_DEFAULT                         HNDTYPE_STRONG
#define HNDTYPE_WEAK_DEFAULT                    HNDTYPE_WEAK_LONG
#define HNDTYPE_WEAK_SHORT                      (0)
#define HNDTYPE_WEAK_LONG                       (1)
#define HNDTYPE_STRONG                          (2)
#define HNDTYPE_PINNED                          (3)
#define HNDTYPE_VARIABLE                        (4)
#define HNDTYPE_REFCOUNTED                      (5)
#define HNDTYPE_DEPENDENT                       (6)
#define HNDTYPE_ASYNCPINNED                     (7)
#define HNDTYPE_SIZEDREF                        (8)
#define HNDTYPE_WEAK_WINRT                      (9)

// Anything above this we consider abnormal and stop processing heap information
const int nMaxHeapSegmentCount = 1000;

class BaseObject
{
    MethodTable    *m_pMethTab;
};


const BYTE gElementTypeInfo[] = {
#define TYPEINFO(e,ns,c,s,g,ia,ip,if,im,gv)    s,
#include "cortypeinfo.h"
#undef TYPEINFO
};

typedef struct tagLockEntry
{
    tagLockEntry *pNext;    // next entry
    tagLockEntry *pPrev;    // prev entry
    DWORD dwULockID;
    DWORD dwLLockID;        // owning lock
    WORD wReaderLevel;      // reader nesting level    
} LockEntry;

#define MAX_CLASSNAME_LENGTH    1024

enum EEFLAVOR {UNKNOWNEE, MSCOREE, MSCORWKS, MSCOREND};

#include "sospriv.h"
extern IXCLRDataProcess *g_clrData;
extern ISOSDacInterface *g_sos;

#include "dacprivate.h"

interface ICorDebugProcess;
extern ICorDebugProcess * g_pCorDebugProcess;

// This class is templated for easy modification.  We may need to update the CachedString
// or related classes to use WCHAR instead of char in the future.
template <class T, int count, int size>
class StaticData
{
public:
    StaticData()
    {
        for (int i = 0; i < count; ++i)
            InUse[i] = false;
    }

    // Whether the individual data pointers in the cache are in use.
    bool InUse[count];

    // The actual data itself.
    T Data[count][size];

    // The number of arrays in the cache.
    static const int Count;

    // The size of each individual array.
    static const int Size;
};

class CachedString
{
public:
    CachedString();
    CachedString(const CachedString &str);
    ~CachedString();

    const CachedString &operator=(const CachedString &str);

    // Returns the capacity of this string.
    size_t GetStrLen() const
    {
        return mSize;
    }

    // Returns a mutable character pointer.  Be sure not to write past the
    // length of this string.
    inline operator char *()
    {
        return mPtr;
    }

    // Returns a const char representation of this string.
    inline operator const char *() const
    {
        return GetPtr();
    }

    // To ensure no AV's, any time a constant pointer is requested, we will
    // return an empty string "" if we hit an OOM.  This will only happen
    // if we hit an OOM and do not check for it before using the string.
    // If you request a non-const char pointer out of this class, it may be
    // null (see operator char *).
    inline const char *GetPtr() const
    {
        if (!mPtr || IsOOM())
            return "";

        return mPtr;
    }

    // Returns true if we ran out of memory trying to allocate the string
    // or the refcount.
    bool IsOOM() const
    {
        return mIndex == -2;
    }
    
    // allocate a string of the specified size.  this will Clear() any
    // previously allocated string.  call IsOOM() to check for failure.
    void Allocate(int size);

private:
    // Copies rhs into this string.
    void Copy(const CachedString &rhs);

    // Clears this string, releasing any underlying memory.
    void Clear();

    // Creates a new string.
    void Create();

    // Sets an out of memory state.
    void SetOOM();

private:
    char *mPtr;

    // The reference count.  This may be null if there is only one copy
    // of this string.
    mutable unsigned int *mRefCount;

    // mIndex contains the index of the cached pointer we are using, or:
    // ~0 - poison value we initialize it to for debugging purposes
    // -1 - mPtr points to a pointer we have new'ed
    // -2 - We hit an oom trying to allocate either mCount or mPtr
    int mIndex;
    
    // contains the size of current string
    int mSize;

private:
    static StaticData<char, 4, 1024> cache;
};

// Things in this namespace should not be directly accessed/called outside of
// the output-related functions.
namespace Output
{
    extern unsigned int g_bSuppressOutput;
    extern unsigned int g_Indent;
    extern unsigned int g_DMLEnable;
    extern bool g_bDbgOutput;
    extern bool g_bDMLExposed;
    
    inline bool IsOutputSuppressed()
    { return g_bSuppressOutput > 0; }
    
    inline void ResetIndent()
    { g_Indent = 0; }
    
    inline void SetDebugOutputEnabled(bool enabled)
    { g_bDbgOutput = enabled; }
    
    inline bool IsDebugOutputEnabled()
    { return g_bDbgOutput; }

    inline void SetDMLExposed(bool exposed)
    { g_bDMLExposed = exposed; }
    
    inline bool IsDMLExposed()
    { return g_bDMLExposed; }

    enum FormatType
    {
        DML_None,
        DML_MethodTable,
        DML_MethodDesc,
        DML_EEClass,
        DML_Module,
        DML_IP,
        DML_Object,
        DML_Domain,
        DML_Assembly,
        DML_ThreadID,
        DML_ValueClass,
        DML_DumpHeapMT,
        DML_ListNearObj,
        DML_ThreadState,
        DML_PrintException,
        DML_RCWrapper,
        DML_CCWrapper,
        DML_ManagedVar,
        DML_Async,
    };

    /**********************************************************************\
    * This function builds a DML string for a ValueClass.  If DML is       *
    * enabled, this function returns a DML string based on the format      *
    * type.  Otherwise this returns a string containing only the hex value *
    * of addr.                                                             *
    *                                                                      *
    * Params:                                                              *
    *   mt - the method table of the ValueClass                            *
    *   addr - the address of the ValueClass                               *
    *   type - the format type to use to output this object                *
    *   fill - whether or not to pad the hex value with zeros              *
    *                                                                      *
    \**********************************************************************/
    CachedString BuildVCValue(CLRDATA_ADDRESS mt, CLRDATA_ADDRESS addr, FormatType type, bool fill = true);
    

    /**********************************************************************\
    * This function builds a DML string for an object.  If DML is enabled, *
    * this function returns a DML string based on the format type.         *
    * Otherwise this returns a string containing only the hex value of     *
    * addr.                                                                *
    *                                                                      *
    * Params:                                                              *
    *   addr - the address of the object                                   *
    *   type - the format type to use to output this object                *
    *   fill - whether or not to pad the hex value with zeros              *
    *                                                                      *
    \**********************************************************************/
    CachedString BuildHexValue(CLRDATA_ADDRESS addr, FormatType type, bool fill = true);

    /**********************************************************************\
    * This function builds a DML string for an managed variable name.      *
    * If DML is enabled, this function returns a DML string that will      *
    * enable the expansion of that managed variable using the !ClrStack    *
    * command to display the variable's fields, otherwise it will just     *
    * return the variable's name as a string.
    *                                                                      *
    * Params:                                                              *
    *   expansionName - the current variable expansion string              *
    *   frame - the frame that contains the variable of interest           *
    *   simpleName - simple name of the managed variable                   *
    *                                                                      *
    \**********************************************************************/
    CachedString BuildManagedVarValue(__in_z LPCWSTR expansionName, ULONG frame, __in_z LPCWSTR simpleName, FormatType type);
    CachedString BuildManagedVarValue(__in_z LPCWSTR expansionName, ULONG frame, int indexInArray, FormatType type);    //used for array indices (simpleName = "[<indexInArray>]")
}

class NoOutputHolder
{
public:
    NoOutputHolder(BOOL bSuppress = TRUE);
    ~NoOutputHolder();

private:
    BOOL mSuppress;
};

class EnableDMLHolder
{
public:
    EnableDMLHolder(BOOL enable);
    ~EnableDMLHolder();

private:
    BOOL mEnable;
};

size_t CountHexCharacters(CLRDATA_ADDRESS val);

// Normal output.
void DMLOut(PCSTR format, ...);         /* Prints out DML strings. */
void IfDMLOut(PCSTR format, ...);       /* Prints given DML string ONLY if DML is enabled; prints nothing otherwise. */
void ExtOut(PCSTR Format, ...);         /* Prints out to ExtOut (no DML). */
void ExtWarn(PCSTR Format, ...);        /* Prints out to ExtWarn (no DML). */
void ExtErr(PCSTR Format, ...);         /* Prints out to ExtErr (no DML). */
void ExtDbgOut(PCSTR Format, ...);      /* Prints out to ExtOut in a checked build (no DML). */
void WhitespaceOut(int count);          /* Prints out "count" number of spaces in the output. */

// Change indent for ExtOut
inline void IncrementIndent()  { Output::g_Indent++; }
inline void DecrementIndent()  { if (Output::g_Indent > 0) Output::g_Indent--; }
inline void ExtOutIndent()  { WhitespaceOut(Output::g_Indent << 2); }

// DML Generation Methods
#define DMLListNearObj(addr) Output::BuildHexValue(addr, Output::DML_ListNearObj).GetPtr()
#define DMLDumpHeapMT(addr) Output::BuildHexValue(addr, Output::DML_DumpHeapMT).GetPtr()
#define DMLMethodTable(addr) Output::BuildHexValue(addr, Output::DML_MethodTable).GetPtr()
#define DMLMethodDesc(addr) Output::BuildHexValue(addr, Output::DML_MethodDesc).GetPtr()
#define DMLClass(addr) Output::BuildHexValue(addr, Output::DML_EEClass).GetPtr()
#define DMLModule(addr) Output::BuildHexValue(addr, Output::DML_Module).GetPtr()
#define DMLIP(ip) Output::BuildHexValue(ip, Output::DML_IP).GetPtr()
#define DMLObject(addr) Output::BuildHexValue(addr, Output::DML_Object).GetPtr()
#define DMLDomain(addr) Output::BuildHexValue(addr, Output::DML_Domain).GetPtr()
#define DMLAssembly(addr) Output::BuildHexValue(addr, Output::DML_Assembly).GetPtr()
#define DMLThreadID(id) Output::BuildHexValue(id, Output::DML_ThreadID, false).GetPtr()
#define DMLValueClass(mt, addr) Output::BuildVCValue(mt, addr, Output::DML_ValueClass).GetPtr()
#define DMLRCWrapper(addr) Output::BuildHexValue(addr, Output::DML_RCWrapper).GetPtr()
#define DMLCCWrapper(addr) Output::BuildHexValue(addr, Output::DML_CCWrapper).GetPtr()
#define DMLManagedVar(expansionName,frame,simpleName) Output::BuildManagedVarValue(expansionName, frame, simpleName, Output::DML_ManagedVar).GetPtr()
#define DMLAsync(addr) Output::BuildHexValue(addr, Output::DML_Async).GetPtr()

bool IsDMLEnabled();


#ifndef SOS_Assert
#define SOS_Assert(x)
#endif

void ConvertToLower(__out_ecount(len) char *buffer, size_t len);

extern const char * const DMLFormats[];
int GetHex(CLRDATA_ADDRESS addr, __out_ecount(len) char *out, size_t len, bool fill);

// A simple string class for mutable strings.  We cannot use STL, so this is a stand in replacement
// for std::string (though it doesn't use the same interface).
template <class T, size_t (__cdecl *LEN)(const T *), errno_t (__cdecl *COPY)(T *, size_t, const T * _Src)>
class BaseString
{
public:
    BaseString()
        : mStr(0), mSize(0), mLength(0)
    {
        const size_t size = 64;
        
        mStr = new T[size];
        mSize = size;
        mStr[0] = 0;
    }

    BaseString(const T *str)
        : mStr(0), mSize(0), mLength(0)
    {
        CopyFrom(str, LEN(str));
    }

    BaseString(const BaseString<T, LEN, COPY> &rhs)
        : mStr(0), mSize(0), mLength(0)
    {
        *this = rhs;
    }

    ~BaseString()
    {
        Clear();
    }

    const BaseString<T, LEN, COPY> &operator=(const BaseString<T, LEN, COPY> &rhs)
    {
        Clear();
        CopyFrom(rhs.mStr, rhs.mLength);
        return *this;
    }

    const BaseString<T, LEN, COPY> &operator=(const T *str)
    {
        Clear();
        CopyFrom(str, LEN(str));
        return *this;
    }

    const BaseString<T, LEN, COPY> &operator +=(const T *str)
    {
        size_t len = LEN(str);
        CopyFrom(str, len);
        return *this;
    }

    const BaseString<T, LEN, COPY> &operator +=(const BaseString<T, LEN, COPY> &str)
    {
        CopyFrom(str.mStr, str.mLength);
        return *this;
    }

    BaseString<T, LEN, COPY> operator+(const T *str) const
    {
        return BaseString<T, LEN, COPY>(mStr, mLength, str, LEN(str));
    }

    BaseString<T, LEN, COPY> operator+(const BaseString<T, LEN, COPY> &str) const
    {
        return BaseString<T, LEN, COPY>(mStr, mLength, str.mStr, str.mLength);
    }

    operator const T *() const
    {
        return mStr;
    }
    
    const T *c_str() const
    {
        return mStr;
    }

    size_t GetLength() const
    {
        return mLength;
    }

private:
    BaseString(const T * str1, size_t len1, const T * str2, size_t len2)
    : mStr(0), mSize(0), mLength(0)
    {
        const size_t size = len1 + len2 + 1 + ((len1 + len2) >> 1);
        mStr = new T[size];
        mSize = size;
        
        CopyFrom(str1, len1);
        CopyFrom(str2, len2);
    }
    
    void Clear()
    {
        mLength = 0;
        mSize = 0;
        if (mStr)
        {
            delete [] mStr;
            mStr = 0;
        }
    }

    void CopyFrom(const T *str, size_t len)
    {
        if (mLength + len + 1 >= mSize)
            Resize(mLength + len + 1);

        COPY(mStr+mLength, mSize-mLength, str);
        mLength += len;
    }

    void Resize(size_t size)
    {
        /* We always resize at least one half bigger than we need.  When CopyFrom requests a resize
         * it asks for the exact size that's needed to concatenate strings.  However in practice
         * it's common to add multiple strings together in a row, e.g.:
         *    String foo = "One " + "Two " + "Three " + "Four " + "\n";
         * Ensuring the size of the string is bigger than we need, and that the minimum size is 64,
         * we will cut down on a lot of needless resizes at the cost of a few bytes wasted in some
         * cases.
         */
        size += size >> 1;
        if (size < 64)
            size = 64;

        T *newStr = new T[size];

        if (mStr)
        {
            COPY(newStr, size, mStr);
            delete [] mStr;
        }
        else
        {
            newStr[0] = 0;
        }
        
        mStr = newStr;
        mSize = size;
    }
private:
    T *mStr;
    size_t mSize, mLength;
};

typedef BaseString<char, strlen, strcpy_s> String;
typedef BaseString<WCHAR, _wcslen, wcscpy_s> WString;


template<class T>
void Flatten(__out_ecount(len) T *data, unsigned int len)
{
    for (unsigned int i = 0; i < len; ++i)
        if (data[i] < 32 || (data[i] > 126 && data[i] <= 255))
            data[i] = '.';
    data[len] = 0;
}

void Flatten(__out_ecount(len) char *data, unsigned int len);

/* Formats for the Format class.  We support the following formats:
 *      Pointer - Same as %p.
 *      Hex - Same as %x (same as %p, but does not output preceding zeros.
 *      PrefixHex - Same as %x, but prepends 0x.
 *      Decimal - Same as %d.
 * Strings and wide strings don't use this.
 */
class Formats
{
public:
    enum Format
    {
        Default,
        Pointer,
        Hex,
        PrefixHex,
        Decimal,
    };
};

enum Alignment
{
    AlignLeft,
    AlignRight
};

namespace Output
{
    /* Defines how a value will be printed.  This class understands how to format
     * and print values according to the format and DML settings provided.
     * The raw templated class handles the pointer/integer case.  Support for
     * character arrays and wide character arrays are handled by template
     * specializations.
     *
     * Note that this class is not used directly.  Instead use the typedefs and
     * macros which define the type of data you are outputing (for example ObjectPtr,
     * MethodTablePtr, etc).
     */
    template <class T>
    class Format
    {
    public:
        Format(T value)
            : mValue(value), mFormat(Formats::Default), mDml(Output::DML_None)
        {
        }

        Format(T value, Formats::Format format, Output::FormatType dmlType)
            : mValue(value), mFormat(format), mDml(dmlType)
        {
        }

        Format(const Format<T> &rhs)
            : mValue(rhs.mValue), mFormat(rhs.mFormat), mDml(rhs.mDml)
        {
        }

        /* Prints out the value according to the Format and DML settings provided.
         */
        void Output() const
        {
            if (IsDMLEnabled() && mDml != Output::DML_None)
            {
                const int len = GetDMLWidth(mDml);
                char *buffer = (char*)alloca(len);
            
                BuildDML(buffer, len, (CLRDATA_ADDRESS)mValue, mFormat, mDml);
                DMLOut(buffer);
            }
            else
            {
                if (mFormat == Formats::Default || mFormat == Formats::Pointer)
                {
                    ExtOut("%p", SOS_PTR(mValue));
                }
                else
                {
                    const char *format = NULL;
                    if (mFormat == Formats::PrefixHex)
                    {
                        format = "0x%x";
                    }
                    else if (mFormat == Formats::Hex)
                    {
                        format = "%x";
                    }
                    else if (mFormat == Formats::Decimal)
                    {
                        format = "%d";
                    }

                    ExtOut(format, (__int32)mValue);
                }

            }
        }

        /* Prints out the value based on a specified width and alignment.
         * Params:
         *   align - Whether the output should be left or right justified.
         *   width - The output width to fill.
         * Note:
         *   This function guarantees that exactly width will be printed out (so if width is 24,
         *   exactly 24 characters will be printed), even if the output wouldn't normally fit
         *   in the space provided.  This function makes no guarantees as to what part of the
         *   data will be printed in the case that width isn't wide enough.
         */
        void OutputColumn(Alignment align, int width) const
        {
            bool leftAlign = align == AlignLeft;
            if (IsDMLEnabled() && mDml != Output::DML_None)
            {
                const int len = GetDMLColWidth(mDml, width);
                char *buffer = (char*)alloca(len);
            
                BuildDMLCol(buffer, len, (CLRDATA_ADDRESS)mValue, mFormat, mDml, leftAlign, width);
                DMLOut(buffer);
            }
            else
            {
                int precision = GetPrecision();
                if (mFormat == Formats::Default || mFormat == Formats::Pointer)
                {
                    if (precision > width)
                        precision = width;

                    ExtOut(leftAlign ? "%-*.*p" : "%*.*p", width, precision, SOS_PTR(mValue));
                }
                else
                {
                    const char *format = NULL;
                    if (mFormat == Formats::PrefixHex)
                    {
                        format = leftAlign ? "0x%-*.*x" : "0x%*.*x";
                        width -= 2;
                    }
                    else if (mFormat == Formats::Hex)
                    {
                        format = leftAlign ? "%-*.*x" : "%*.*x";
                    }
                    else if (mFormat == Formats::Decimal)
                    {
                        format = leftAlign ? "%-*.*d" : "%*.*d";
                    }

                    if (precision > width)
                        precision = width;

                    ExtOut(format, width, precision, (__int32)mValue);
                }
            }
        }
    
        /* Converts this object into a Wide char string.  This allows you to write the following code:
         *    WString foo = L"bar " + ObjectPtr(obj);
         * Where ObjectPtr is a subclass/typedef of this Format class.
         */
        operator WString() const
        {
            String str = *this;
            const char *cstr = (const char *)str;
        
            int len = MultiByteToWideChar(CP_ACP, 0, cstr, -1, NULL, 0);
            WCHAR *buffer = (WCHAR *)alloca(len*sizeof(WCHAR));
        
            MultiByteToWideChar(CP_ACP, 0, cstr, -1, buffer, len);
        
            return WString(buffer);
        }
    
        /* Converts this object into a String object.  This allows you to write the following code:
         *    String foo = "bar " + ObjectPtr(obj);
         * Where ObjectPtr is a subclass/typedef of this Format class.
         */
        operator String() const
        {
            if (IsDMLEnabled() && mDml != Output::DML_None)
            {
                const int len = GetDMLColWidth(mDml, 0);
                char *buffer = (char*)alloca(len);
            
                BuildDMLCol(buffer, len, (CLRDATA_ADDRESS)mValue, mFormat, mDml, false, 0);
                return buffer;
            }
            else
            {
                char buffer[64];
                if (mFormat == Formats::Default || mFormat == Formats::Pointer)
                {
                    sprintf_s(buffer, _countof(buffer), "%p", (int *)(SIZE_T)mValue);
                    ConvertToLower(buffer, _countof(buffer));
                }
                else
                {
                    const char *format = NULL;
                    if (mFormat == Formats::PrefixHex)
                        format = "0x%x";
                    else if (mFormat == Formats::Hex)
                        format = "%x";
                    else if (mFormat == Formats::Decimal)
                        format = "%d";

                    sprintf_s(buffer, _countof(buffer), format, (__int32)mValue);
                    ConvertToLower(buffer, _countof(buffer));
                }
                
                return buffer;
            }
        }

    private:
        int GetPrecision() const
        {
            if (mFormat == Formats::Hex || mFormat == Formats::PrefixHex)
            {
                ULONGLONG val = mValue;
                int count = 0;
                while (val)
                {
                    val >>= 4;
                    count++;
                }

                if (count == 0)
                    count = 1;

                return count;
            }

            else if (mFormat == Formats::Decimal)
            {
                T val = mValue;
                int count = (val > 0) ? 0 : 1;
                while (val)
                {
                    val /= 10;
                    count++;
                }

                return count;
            }

            // mFormat == Formats::Pointer
            return sizeof(int*)*2;
        }

        static inline void BuildDML(__out_ecount(len) char *result, int len, CLRDATA_ADDRESS value, Formats::Format format, Output::FormatType dmlType)
        {
            BuildDMLCol(result, len, value, format, dmlType, true, 0);
        }
    
        static int GetDMLWidth(Output::FormatType dmlType)
        {
            return GetDMLColWidth(dmlType, 0);
        }
    
        static void BuildDMLCol(__out_ecount(len) char *result, int len, CLRDATA_ADDRESS value, Formats::Format format, Output::FormatType dmlType, bool leftAlign, int width)
        {
            char hex[64];
            int count = GetHex(value, hex, _countof(hex), format != Formats::Hex);
            int i = 0;
    
            if (!leftAlign)
            {
                for (; i < width - count; ++i)
                    result[i] = ' ';
        
                result[i] = 0;
            }
    
            int written = sprintf_s(result+i, len - i, DMLFormats[dmlType], hex, hex);
    
            SOS_Assert(written != -1);
            if (written != -1)
            {
                for (i = i + written; i < width; ++i)
                    result[i] = ' ';
        
                result[i] = 0;
            }
        }
    
        static int GetDMLColWidth(Output::FormatType dmlType, int width)
        {
            return 1 + 4*sizeof(int*) + (int)strlen(DMLFormats[dmlType]) + width;
        }

    private:
        T mValue;
        Formats::Format mFormat;
        Output::FormatType mDml;
     };

     /* Format class used for strings.
      */
    template <>
    class Format<const char *>
    {
    public:
        Format(const char *value)
            : mValue(value)
        {
        }

        Format(const Format<const char *> &rhs)
            : mValue(rhs.mValue)
        {
        }

        void Output() const
        {
            if (IsDMLEnabled())
                DMLOut("%s", mValue);
            else
                ExtOut("%s", mValue);
        }

        void OutputColumn(Alignment align, int width) const
        {
            int precision = (int)strlen(mValue);

            if (precision > width)
                precision = width;

            const char *format = align == AlignLeft ? "%-*.*s" : "%*.*s";
        
            if (IsDMLEnabled())
                DMLOut(format, width, precision, mValue);
            else
                ExtOut(format, width, precision, mValue);
        }

    private:
        const char *mValue;
    };

    /* Format class for wide char strings.
     */
    template <>
    class Format<const WCHAR *>
    {
    public:
        Format(const WCHAR *value)
            : mValue(value)
        {
        }

        Format(const Format<const WCHAR *> &rhs)
            : mValue(rhs.mValue)
        {
        }
    
        void Output() const
        {
            if (IsDMLEnabled())
                DMLOut("%S", mValue);
            else
                ExtOut("%S", mValue);
        }

        void OutputColumn(Alignment align, int width) const
        {
            int precision = (int)_wcslen(mValue);
            if (precision > width)
                precision = width;

            const char *format = align == AlignLeft ? "%-*.*S" : "%*.*S";

            if (IsDMLEnabled())
                DMLOut(format, width, precision, mValue);
            else
                ExtOut(format, width, precision, mValue);
        }

    private:
        const WCHAR *mValue;
    };


    template <class T>
    void InternalPrint(const T &t)
    {
        Format<T>(t).Output();
    }

    template <class T>
    void InternalPrint(const Format<T> &t)
    {
        t.Output();
    }

    inline void InternalPrint(const char t[])
    {
        Format<const char *>(t).Output();
    }
}

#define DefineFormatClass(name, format, dml) \
    template <class T>                       \
    Output::Format<T> name(T value)          \
    { return Output::Format<T>(value, format, dml); }

DefineFormatClass(EEClassPtr, Formats::Pointer, Output::DML_EEClass);
DefineFormatClass(ObjectPtr, Formats::Pointer, Output::DML_Object);
DefineFormatClass(ExceptionPtr, Formats::Pointer, Output::DML_PrintException);
DefineFormatClass(ModulePtr, Formats::Pointer, Output::DML_Module);
DefineFormatClass(MethodDescPtr, Formats::Pointer, Output::DML_MethodDesc);
DefineFormatClass(AppDomainPtr, Formats::Pointer, Output::DML_Domain);
DefineFormatClass(ThreadState, Formats::Hex, Output::DML_ThreadState);
DefineFormatClass(ThreadID, Formats::Hex, Output::DML_ThreadID);
DefineFormatClass(RCWrapper, Formats::Pointer, Output::DML_RCWrapper);
DefineFormatClass(CCWrapper, Formats::Pointer, Output::DML_CCWrapper);
DefineFormatClass(InstructionPtr, Formats::Pointer, Output::DML_IP);
DefineFormatClass(NativePtr, Formats::Pointer, Output::DML_None);

DefineFormatClass(Decimal, Formats::Decimal, Output::DML_None);
DefineFormatClass(Pointer, Formats::Pointer, Output::DML_None);
DefineFormatClass(PrefixHex, Formats::PrefixHex, Output::DML_None);
DefineFormatClass(Hex, Formats::Hex, Output::DML_None);

#undef DefineFormatClass

template <class T0>
void Print(const T0 &val0)
{
    Output::InternalPrint(val0);
}

template <class T0, class T1>
void Print(const T0 &val0, const T1 &val1)
{
    Output::InternalPrint(val0);
    Output::InternalPrint(val1);
}

template <class T0>
void PrintLn(const T0 &val0)
{
    Output::InternalPrint(val0);
    ExtOut("\n");
}

template <class T0, class T1>
void PrintLn(const T0 &val0, const T1 &val1)
{
    Output::InternalPrint(val0);
    Output::InternalPrint(val1);
    ExtOut("\n");
}

template <class T0, class T1, class T2>
void PrintLn(const T0 &val0, const T1 &val1, const T2 &val2)
{
    Output::InternalPrint(val0);
    Output::InternalPrint(val1);
    Output::InternalPrint(val2);
    ExtOut("\n");
}


/* This class handles the formatting for output which is in a table format.  To use this class you define
 * how the table is formatted by setting the number of columns in the table, the default column width,
 * the default column alignment, the indentation (whitespace) for the table, and the amount of padding
 * (whitespace) between each column. Once this has been setup, you output rows at a time or individual
 * columns to build the output instead of manually tabbing out space.
 * Also note that this class was built to work with the Format class.  When outputing data, use the
 * predefined output types to specify the format (such as ObjectPtr, MethodDescPtr, Decimal, etc).  This
 * tells the TableOutput class how to display the data, and where applicable, it automatically generates
 * the appropriate DML output.  See the DefineFormatClass macro.
 */
class TableOutput
{
public:

    TableOutput()
        : mColumns(0), mDefaultWidth(0), mIndent(0), mPadding(0), mCurrCol(0), mDefaultAlign(AlignLeft),
          mWidths(0), mAlignments(0)
      {
      }
    /* Constructor.
     * Params:
     *   numColumns - the number of columns the table has
     *   defaultColumnWidth - the default width of each column
     *   alignmentDefault - whether columns are by default left aligned or right aligned
     *   indent - the amount of whitespace to prefix at the start of the row (in characters)
     *   padding - the amount of whitespace to place between each column (in characters)
     */
    TableOutput(int numColumns, int defaultColumnWidth, Alignment alignmentDefault = AlignLeft, int indent = 0, int padding = 1)
        : mColumns(numColumns), mDefaultWidth(defaultColumnWidth), mIndent(indent), mPadding(padding), mCurrCol(0), mDefaultAlign(alignmentDefault),
          mWidths(0), mAlignments(0)
    {
    }

    ~TableOutput()
    {
        Clear();
    }

    /* See the documentation for the constructor.
     */
    void ReInit(int numColumns, int defaultColumnWidth, Alignment alignmentDefault = AlignLeft, int indent = 0, int padding = 1);

    /* Sets the amount of whitespace to prefix at the start of the row (in characters). 
     */
    void SetIndent(int indent)
    {
        SOS_Assert(indent >= 0);

        mIndent = indent;
    }

    /* Sets the exact widths for the the given columns.
     * Params:
     *   columns - the number of columns you are providing the width for, starting at the first column
     *   ... - an int32 for each column (given by the number of columns in the first parameter).
     * Example:
     *    If you have 5 columns in the table, you can set their widths like so:
     *       tableOutput.SetWidths(5, 2, 3, 5, 7, 13);
     * Note:
     *    It's fine to pass a value for "columns" less than the number of columns in the table.  This
     *    is useful when you set the default column width to be correct for most of the table, and need
     *    to make a minor adjustment to a few.
     */
    void SetWidths(int columns, ...);

    /* Individually sets a column to the given width.
     * Params:
     *   col - the column to set, 0 indexed
     *   width - the width of the column (note this must be non-negative)
     */
    void SetColWidth(int col, int width);

    /* Individually sets the column alignment.
     * Params:
     *   col - the column to set, 0 indexed
     *   align - the new alignment (left or right) for the column
     */
    void SetColAlignment(int col, Alignment align);

    
    /* The WriteRow family of functions allows you to write an entire row of the table at once.
     * The common use case for the TableOutput class is to individually output each column after
     * calculating what the value should contain.  However, this would be tedious if you already
     * knew the contents of the entire row which usually happenes when you are printing out the
     * header for the table.  To use this, simply pass each column as an individual parameter,
     * for example:
     *    tableOutput.WriteRow("First Column", "Second Column", Decimal(3), PrefixHex(4), "Fifth Column");
     */
    template <class T0, class T1>
    void WriteRow(T0 t0, T1 t1)
    {
        WriteColumn(0, t0);
        WriteColumn(1, t1);
    }

    template <class T0, class T1, class T2>
    void WriteRow(T0 t0, T1 t1, T2 t2)
    {
        WriteColumn(0, t0);
        WriteColumn(1, t1);
        WriteColumn(2, t2);
    }


    template <class T0, class T1, class T2, class T3>
    void WriteRow(T0 t0, T1 t1, T2 t2, T3 t3)
    {
        WriteColumn(0, t0);
        WriteColumn(1, t1);
        WriteColumn(2, t2);
        WriteColumn(3, t3);
    }

    
    template <class T0, class T1, class T2, class T3, class T4>
    void WriteRow(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4)
    {
        WriteColumn(0, t0);
        WriteColumn(1, t1);
        WriteColumn(2, t2);
        WriteColumn(3, t3);
        WriteColumn(4, t4);
    }
    
    template <class T0, class T1, class T2, class T3, class T4, class T5>
    void WriteRow(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
    {
        WriteColumn(0, t0);
        WriteColumn(1, t1);
        WriteColumn(2, t2);
        WriteColumn(3, t3);
        WriteColumn(4, t4);
        WriteColumn(5, t5);
    }

    template <class T0, class T1, class T2, class T3, class T4, class T5, class T6, class T7, class T8, class T9>
    void WriteRow(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
    {
        WriteColumn(0, t0);
        WriteColumn(1, t1);
        WriteColumn(2, t2);
        WriteColumn(3, t3);
        WriteColumn(4, t4);
        WriteColumn(5, t5);
        WriteColumn(6, t6);
        WriteColumn(7, t7);
        WriteColumn(8, t8);
        WriteColumn(9, t9);
    }

    /* The WriteColumn family of functions is used to output individual columns in the table.
     * The intent is that the bulk of the table will be generated in a loop like so:
     *   while (condition) {
     *      int value1 = CalculateFirstColumn();
     *      table.WriteColumn(0, value1);
     *
     *      String value2 = CalculateSecondColumn();
     *      table.WriteColumn(1, value2);
     *   }
     * Params:
     *   col - the column to write, 0 indexed
     *   t - the value to write
     * Note:
     *   You should generally use the specific instances of the Format class to generate output.
     *   For example, use the "Decimal", "Pointer", "ObjectPtr", etc.  When passing data to this
     *   function.  This tells the Table class how to display the value.
     */
    template <class T>
    void WriteColumn(int col, const Output::Format<T> &t)
    {
        SOS_Assert(col >= 0);
        SOS_Assert(col < mColumns);

        if (col != mCurrCol)
            OutputBlankColumns(col);

        if (col == 0)
            OutputIndent();
        
        bool lastCol = col == mColumns - 1;

        if (!lastCol)
            t.OutputColumn(GetColAlign(col), GetColumnWidth(col));
        else
            t.Output();

        ExtOut(lastCol ? "\n" : GetWhitespace(mPadding));

        if (lastCol)
            mCurrCol = 0;
        else
            mCurrCol = col+1;
    }
    
    template <class T>
    void WriteColumn(int col, T t)
    {
        WriteColumn(col, Output::Format<T>(t));
    }

    void WriteColumn(int col, const String &str)
    {
        WriteColumn(col, Output::Format<const char *>(str));
    }

    void WriteColumn(int col, const WString &str)
    {
        WriteColumn(col, Output::Format<const WCHAR *>(str));
    }

    void WriteColumn(int col, __in_z WCHAR *str)
    {
        WriteColumn(col, Output::Format<const WCHAR *>(str));
    }

    void WriteColumn(int col, const WCHAR *str)
    {
        WriteColumn(col, Output::Format<const WCHAR *>(str));
    }
    
    inline void WriteColumn(int col, __in_z char *str)
    {
        WriteColumn(col, Output::Format<const char *>(str));
    }

    /* Writes a column using a printf style format.  You cannot use the Format class with
     * this function to specify how the output should look, use printf style formatting
     * with the appropriate parameters instead.
     */
    void WriteColumnFormat(int col, const char *fmt, ...)
    {
        SOS_Assert(strstr(fmt, "%S") == NULL);

        char result[128];
        
        va_list list;
        va_start(list, fmt);
        vsprintf_s(result, _countof(result), fmt, list);
        va_end(list);

        WriteColumn(col, result);
    }
    
    void WriteColumnFormat(int col, const WCHAR *fmt, ...)
    {
        WCHAR result[128];
        
        va_list list;
        va_start(list, fmt);
        vswprintf_s(result, _countof(result), fmt, list);
        va_end(list);

        WriteColumn(col, result);
    }

    /* This function is a shortcut for writing the next column.  (That is, the one after the
     * one you just wrote.)
     */
    template <class T>
    void WriteColumn(T t)
    {
        WriteColumn(mCurrCol, t);
    }

private:
    void Clear();
    void AllocWidths();
    int GetColumnWidth(int col);
    Alignment GetColAlign(int col);
    const char *GetWhitespace(int amount);
    void OutputBlankColumns(int col);
    void OutputIndent();

private:
    int mColumns, mDefaultWidth, mIndent, mPadding, mCurrCol;
    Alignment mDefaultAlign;
    int *mWidths;
    Alignment *mAlignments;
};

HRESULT GetMethodDefinitionsFromName(DWORD_PTR ModulePtr, IXCLRDataModule* mod, const char* name, IXCLRDataMethodDefinition **ppMethodDefinitions, int numMethods, int *numMethodsNeeded);
HRESULT GetMethodDescsFromName(DWORD_PTR ModulePtr, IXCLRDataModule* mod, const char* name, DWORD_PTR **pOut, int *numMethodDescs);

HRESULT FileNameForModule (DacpModuleData *pModule, __out_ecount (MAX_LONGPATH) WCHAR *fileName);
HRESULT FileNameForModule (DWORD_PTR pModuleAddr, __out_ecount (MAX_LONGPATH) WCHAR *fileName);
void IP2MethodDesc (DWORD_PTR IP, DWORD_PTR &methodDesc, JITTypes &jitType,
                    DWORD_PTR &gcinfoAddr);
const char *ElementTypeName (unsigned type);
void DisplayFields (CLRDATA_ADDRESS cdaMT, DacpMethodTableData *pMTD, DacpMethodTableFieldData *pMTFD,
                    DWORD_PTR dwStartAddr = 0, BOOL bFirst=TRUE, BOOL bValueClass=FALSE);
int GetObjFieldOffset(CLRDATA_ADDRESS cdaObj, __in_z LPCWSTR wszFieldName, BOOL bFirst=TRUE);
int GetObjFieldOffset(CLRDATA_ADDRESS cdaObj, CLRDATA_ADDRESS cdaMT, __in_z LPCWSTR wszFieldName, BOOL bFirst=TRUE, DacpFieldDescData* pDacpFieldDescData=NULL);
int GetValueFieldOffset(CLRDATA_ADDRESS cdaMT, __in_z LPCWSTR wszFieldName, DacpFieldDescData* pDacpFieldDescData);

BOOL IsValidToken(DWORD_PTR ModuleAddr, mdTypeDef mb);
void NameForToken_s(DacpModuleData *pModule, mdTypeDef mb, __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName, 
                  bool bClassName=true);
void NameForToken_s(DWORD_PTR ModuleAddr, mdTypeDef mb, __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName, 
                  bool bClassName=true);
HRESULT NameForToken_s(mdTypeDef mb, IMetaDataImport *pImport, __out_ecount (capacity_mdName) WCHAR *mdName,  size_t capacity_mdName, 
                     bool bClassName);
HRESULT NameForTokenNew_s(mdTypeDef mb, IMDInternalImport *pImport, __out_ecount (capacity_mdName) WCHAR *mdName,  size_t capacity_mdName, 
                     bool bClassName);

void vmmap();
void vmstat();

#ifndef FEATURE_PAL
///////////////////////////////////////////////////////////////////////////////////////////////////
// Support for managed stack tracing
//

DWORD_PTR GetDebuggerJitInfo(DWORD_PTR md);

///////////////////////////////////////////////////////////////////////////////////////////////////
#endif // FEATURE_PAL

template <typename SCALAR>
inline
int bitidx(SCALAR bitflag)
{
    for (int idx = 0; idx < static_cast<int>(sizeof(bitflag))*8; ++idx)
    {
        if (bitflag & (1 << idx))
        {
            _ASSERTE((bitflag & (~(1 << idx))) == 0);
            return idx;
        }
    }
    return -1;
}

HRESULT
DllsName(
    ULONG_PTR addrContaining,
    __out_ecount (MAX_LONGPATH) WCHAR *dllName
    );

inline
BOOL IsElementValueType (CorElementType cet)
{
    return (cet >= ELEMENT_TYPE_BOOLEAN && cet <= ELEMENT_TYPE_R8) 
        || cet == ELEMENT_TYPE_VALUETYPE || cet == ELEMENT_TYPE_I || cet == ELEMENT_TYPE_U;
}


#define safemove(dst, src) \
SafeReadMemory (TO_TADDR(src), &(dst), sizeof(dst), NULL)

extern "C" PDEBUG_DATA_SPACES g_ExtData;

#include <arrayholder.h>

// This class acts a smart pointer which calls the Release method on any object
// you place in it when the ToRelease class falls out of scope.  You may use it
// just like you would a standard pointer to a COM object (including if (foo),
// if (!foo), if (foo == 0), etc) except for two caveats:
//     1. This class never calls AddRef and it always calls Release when it
//        goes out of scope.
//     2. You should never use & to try to get a pointer to a pointer unless
//        you call Release first, or you will leak whatever this object contains
//        prior to updating its internal pointer.
template<class T>
class ToRelease
{
public:
    ToRelease()
        : m_ptr(NULL)
    {}
    
    ToRelease(T* ptr)
        : m_ptr(ptr)
    {}
    
    ~ToRelease()
    {
        Release();
    }

    void operator=(T *ptr)
    {
        Release();

        m_ptr = ptr;
    }

    T* operator->()
    {
        return m_ptr;
    }

    operator T*()
    {
        return m_ptr;
    }

    T** operator&()
    {
        return &m_ptr;
    }

    T* GetPtr() const
    {
        return m_ptr;
    }

    T* Detach()
    {
        T* pT = m_ptr;
        m_ptr = NULL;
        return pT;
    }
    
    void Release()
    {
        if (m_ptr != NULL)
        {
            m_ptr->Release();
            m_ptr = NULL;
        }
    }

private:
    T* m_ptr;    
};

struct ModuleInfo
{
    ULONG64 baseAddr;
    ULONG64 size;
    BOOL hasPdb;
};
extern ModuleInfo moduleInfo[];

BOOL InitializeHeapData();
BOOL IsServerBuild ();
UINT GetMaxGeneration();
UINT GetGcHeapCount();
BOOL GetGcStructuresValid();

ULONG GetILSize(DWORD_PTR ilAddr); // REturns 0 if error occurs
HRESULT DecodeILFromAddress(IMetaDataImport *pImport, TADDR ilAddr);
void DecodeIL(IMetaDataImport *pImport, BYTE *buffer, ULONG bufSize);
void DecodeDynamicIL(BYTE *data, ULONG Size, DacpObjectData& tokenArray);

BOOL IsRetailBuild (size_t base);
EEFLAVOR GetEEFlavor ();
HRESULT InitCorDebugInterface();
VOID UninitCorDebugInterface();
#ifndef FEATURE_PAL
BOOL GetEEVersion(VS_FIXEDFILEINFO *pFileInfo);
BOOL GetSOSVersion(VS_FIXEDFILEINFO *pFileInfo);
#endif

BOOL IsDumpFile ();

// IsMiniDumpFile will return true if 1) we are in
// a small format minidump, and g_InMinidumpSafeMode is true.
extern BOOL g_InMinidumpSafeMode;

BOOL IsMiniDumpFile();
void ReportOOM();

BOOL SafeReadMemory (TADDR offset, PVOID lpBuffer, ULONG cb, PULONG lpcbBytesRead);
#if !defined(_TARGET_WIN64_) && !defined(_ARM64_)
// on 64-bit platforms TADDR and CLRDATA_ADDRESS are identical
inline BOOL SafeReadMemory (CLRDATA_ADDRESS offset, PVOID lpBuffer, ULONG cb, PULONG lpcbBytesRead)
{ return SafeReadMemory(TO_TADDR(offset), lpBuffer, cb, lpcbBytesRead); }
#endif

BOOL NameForMD_s (DWORD_PTR pMD, __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName);
BOOL NameForMT_s (DWORD_PTR MTAddr, __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName);

WCHAR *CreateMethodTableName(TADDR mt, TADDR cmt = NULL);

void isRetAddr(DWORD_PTR retAddr, DWORD_PTR* whereCalled);
DWORD_PTR GetValueFromExpression (___in __in_z const char *const str);

enum ModuleHeapType
{
    ModuleHeapType_ThunkHeap,
    ModuleHeapType_LookupTableHeap
};

HRESULT PrintDomainHeapInfo(const char *name, CLRDATA_ADDRESS adPtr, DWORD_PTR *size, DWORD_PTR *wasted = 0);
DWORD_PTR PrintModuleHeapInfo(DWORD_PTR *moduleList, int count, ModuleHeapType type, DWORD_PTR *wasted = 0);
void PrintHeapSize(DWORD_PTR total, DWORD_PTR wasted);
void DomainInfo(DacpAppDomainData *pDomain);
void AssemblyInfo(DacpAssemblyData *pAssembly);
DWORD_PTR LoaderHeapInfo(CLRDATA_ADDRESS pLoaderHeapAddr, DWORD_PTR *wasted = 0);
DWORD_PTR JitHeapInfo();
DWORD_PTR VSDHeapInfo(CLRDATA_ADDRESS appDomain, DWORD_PTR *wasted = 0);

DWORD GetNumComponents(TADDR obj);

struct GenUsageStat
{
    size_t allocd;
    size_t freed;
    size_t unrooted;
};

struct HeapUsageStat
{
    GenUsageStat  genUsage[4]; // gen0, 1, 2, LOH
};

extern DacpUsefulGlobalsData g_special_usefulGlobals;
BOOL GCHeapUsageStats(const DacpGcHeapDetails& heap, BOOL bIncUnreachable, HeapUsageStat *hpUsage);

class HeapStat
{
protected:
    struct Node
    {
        DWORD_PTR data;
        DWORD count;
        size_t totalSize;
        Node* left;
        Node* right;
        Node ()
            : data(0), count(0), totalSize(0), left(NULL), right(NULL)
        {
        }
    };
    BOOL bHasStrings;
    Node *head;
    BOOL fLinear;
public:
    HeapStat ()
        : bHasStrings(FALSE), head(NULL), fLinear(FALSE)
    {}
    ~HeapStat()
    {
        Delete();
    }
    // TODO: Change the aSize argument to size_t when we start supporting
    // TODO: object sizes above 4GB
    void Add (DWORD_PTR aData, DWORD aSize);
    void Sort ();
    void Print (const char* label = NULL);
    void Delete ();
    void HasStrings(BOOL abHasStrings)
        {
            bHasStrings = abHasStrings;
        }
private:
    int CompareData(DWORD_PTR n1, DWORD_PTR n2);
    void SortAdd (Node *&root, Node *entry);
    void LinearAdd (Node *&root, Node *entry);
    void ReverseLeftMost (Node *root);
    void Linearize();
};

class CGCDesc;

// The information MethodTableCache returns.
struct MethodTableInfo
{
    bool IsInitialized()       { return BaseSize != 0; }

    DWORD BaseSize;           // Caching BaseSize and ComponentSize for a MethodTable
    DWORD ComponentSize;      // here has HUGE perf benefits in heap traversals.
    BOOL  bContainsPointers;
    BOOL  bCollectible;
    DWORD_PTR* GCInfoBuffer;  // Start of memory of GC info
    CGCDesc* GCInfo;    // Just past GC info (which is how it is stored)
    bool  ArrayOfVC;
    TADDR LoaderAllocatorObjectHandle;
};

class MethodTableCache
{
protected:

    struct Node
    {
        DWORD_PTR data;            // This is the key (the method table pointer)
        MethodTableInfo info;  // The info associated with this MethodTable
        Node* left;
        Node* right;
        Node (DWORD_PTR aData) : data(aData), left(NULL), right(NULL)
        {
            info.BaseSize = 0;
            info.ComponentSize = 0;
            info.bContainsPointers = false;
            info.bCollectible = false;
            info.GCInfo = NULL;
            info.ArrayOfVC = false;
            info.GCInfoBuffer = NULL;
            info.LoaderAllocatorObjectHandle = NULL;
        }
    };
    Node *head;
public:
    MethodTableCache ()
        : head(NULL)
    {}
    ~MethodTableCache() { Clear(); }

    // Always succeeds, if it is not present it adds an empty Info struct and returns that
    // Thus you must call 'IsInitialized' on the returned value before using it
    MethodTableInfo* Lookup(DWORD_PTR aData);

    void Clear ();
private:
    int CompareData(DWORD_PTR n1, DWORD_PTR n2);    
    void ReverseLeftMost (Node *root);    
};

extern MethodTableCache g_special_mtCache;

struct DumpArrayFlags
{
    DWORD_PTR startIndex;
    DWORD_PTR Length;
    BOOL bDetail;
    LPSTR strObject;
    BOOL bNoFieldsForElement;
    
    DumpArrayFlags ()
        : startIndex(0), Length((DWORD_PTR)-1), bDetail(FALSE), strObject (0), bNoFieldsForElement(FALSE)
    {}
    ~DumpArrayFlags ()
    {
        if (strObject)
            delete [] strObject;
    }
}; //DumpArrayFlags



// -----------------------------------------------------------------------

#define BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX    0x08000000
#define BIT_SBLK_FINALIZER_RUN              0x40000000
#define BIT_SBLK_SPIN_LOCK                  0x10000000
#define SBLK_MASK_LOCK_THREADID             0x000003FF   // special value of 0 + 1023 thread ids
#define SBLK_MASK_LOCK_RECLEVEL             0x0000FC00   // 64 recursion levels
#define SBLK_RECLEVEL_SHIFT                 10           // shift right this much to get recursion level
#define BIT_SBLK_IS_HASHCODE            0x04000000
#define MASK_HASHCODE                   ((1<<HASHCODE_BITS)-1)
#define SYNCBLOCKINDEX_BITS             26
#define MASK_SYNCBLOCKINDEX             ((1<<SYNCBLOCKINDEX_BITS)-1)

HRESULT GetMTOfObject(TADDR obj, TADDR *mt);

struct needed_alloc_context 
{
    BYTE*   alloc_ptr;   // starting point for next allocation
    BYTE*   alloc_limit; // ending point for allocation region/quantum
};

struct AllocInfo
{
    needed_alloc_context *array;
    int num;                     // number of allocation contexts in array

    AllocInfo()
        : array(NULL)
        , num(0)
    {}
    void Init()
    {
        extern void GetAllocContextPtrs(AllocInfo *pallocInfo);
        GetAllocContextPtrs(this);
    }
    ~AllocInfo()
    { 
        if (array != NULL) 
            delete[] array; 
    }
};

struct GCHandleStatistics
{
    HeapStat hs;
    
    DWORD strongHandleCount;
    DWORD pinnedHandleCount;
    DWORD asyncPinnedHandleCount;
    DWORD refCntHandleCount;
    DWORD weakLongHandleCount;
    DWORD weakShortHandleCount;
    DWORD variableCount;
    DWORD sizedRefCount;
    DWORD dependentCount;
    DWORD weakWinRTHandleCount;
    DWORD unknownHandleCount;
    GCHandleStatistics()
        : strongHandleCount(0), pinnedHandleCount(0), asyncPinnedHandleCount(0), refCntHandleCount(0),
          weakLongHandleCount(0), weakShortHandleCount(0), variableCount(0), sizedRefCount(0),
          dependentCount(0), weakWinRTHandleCount(0), unknownHandleCount(0)
    {}
    ~GCHandleStatistics()
    {
        hs.Delete();
    }
};

struct SegmentLookup
{
    DacpHeapSegmentData *m_segments;
    int m_iSegmentsSize;
    int m_iSegmentCount;
        
    SegmentLookup();
    ~SegmentLookup();

    void Clear();
    BOOL AddSegment(DacpHeapSegmentData *pData);
    CLRDATA_ADDRESS GetHeap(CLRDATA_ADDRESS object, BOOL& bFound);
};

class GCHeapSnapshot
{
private:
    BOOL m_isBuilt;
    DacpGcHeapDetails *m_heapDetails;
    DacpGcHeapData m_gcheap;
    SegmentLookup m_segments;

    BOOL AddSegments(DacpGcHeapDetails& details);
public:
    GCHeapSnapshot();

    BOOL Build();
    void Clear();
    BOOL IsBuilt() { return m_isBuilt; }

    DacpGcHeapData *GetHeapData() { return &m_gcheap; }
    
    int GetHeapCount() { return m_gcheap.HeapCount; }    
    
    DacpGcHeapDetails *GetHeap(CLRDATA_ADDRESS objectPointer);
    int GetGeneration(CLRDATA_ADDRESS objectPointer);

    
};
extern GCHeapSnapshot g_snapshot;
    
BOOL IsSameModuleName (const char *str1, const char *str2);
BOOL IsModule (DWORD_PTR moduleAddr);
BOOL IsMethodDesc (DWORD_PTR value);
BOOL IsMethodTable (DWORD_PTR value);
BOOL IsStringObject (size_t obj);
BOOL IsObjectArray (DWORD_PTR objPointer);
BOOL IsObjectArray (DacpObjectData *pData);
BOOL IsDerivedFrom(CLRDATA_ADDRESS mtObj, __in_z LPCWSTR baseString);
BOOL TryGetMethodDescriptorForDelegate(CLRDATA_ADDRESS delegateAddr, CLRDATA_ADDRESS* pMD);

/* Returns a list of all modules in the process.
 * Params:
 *      name - The name of the module you would like.  If mName is NULL the all modules are returned.
 *      numModules - The number of modules in the array returned.
 * Returns:
 *      An array of modules whose length is *numModules, NULL if an error occurred.  Note that if this
 *      function succeeds but finds no modules matching the name given, this function returns a valid
 *      array, but *numModules will equal 0.
 * Note:
 *      You must clean up the return value of this array by calling delete [] on it, or using the
 *      ArrayHolder class.
 */
DWORD_PTR *ModuleFromName(__in_opt LPSTR name, int *numModules);
void GetInfoFromName(DWORD_PTR ModuleAddr, const char* name, mdTypeDef* retMdTypeDef=NULL);
void GetInfoFromModule (DWORD_PTR ModuleAddr, ULONG token, DWORD_PTR *ret=NULL);

    
typedef void (*VISITGCHEAPFUNC)(DWORD_PTR objAddr,size_t Size,DWORD_PTR methodTable,LPVOID token);
BOOL GCHeapsTraverse(VISITGCHEAPFUNC pFunc, LPVOID token, BOOL verify=true);

/////////////////////////////////////////////////////////////////////////////////////////////////////////

struct strobjInfo
{
    size_t  methodTable;
    DWORD   m_StringLength;
};

// Just to make figuring out which fill pointer element matches a generation
// a bit less confusing. This gen_segment function is ported from gc.cpp.
inline unsigned int gen_segment (int gen)
{
    return (DAC_NUMBERGENERATIONS - gen - 1);
}

inline CLRDATA_ADDRESS SegQueue(DacpGcHeapDetails& heapDetails, int seg)
{
    return heapDetails.finalization_fill_pointers[seg - 1];
}

inline CLRDATA_ADDRESS SegQueueLimit(DacpGcHeapDetails& heapDetails, int seg)
{
    return heapDetails.finalization_fill_pointers[seg];
}

#define FinalizerListSeg (DAC_NUMBERGENERATIONS+1)
#define CriticalFinalizerListSeg (DAC_NUMBERGENERATIONS)

void GatherOneHeapFinalization(DacpGcHeapDetails& heapDetails, HeapStat *stat, BOOL bAllReady, BOOL bShort);

CLRDATA_ADDRESS GetAppDomainForMT(CLRDATA_ADDRESS mtPtr);
CLRDATA_ADDRESS GetAppDomain(CLRDATA_ADDRESS objPtr);
void GCHeapInfo(const DacpGcHeapDetails &heap, DWORD_PTR &total_size);
BOOL GCObjInHeap(TADDR taddrObj, const DacpGcHeapDetails &heap, 
    TADDR_SEGINFO& trngSeg, int& gen, TADDR_RANGE& allocCtx, BOOL &bLarge);

BOOL VerifyObject(const DacpGcHeapDetails &heap, const DacpHeapSegmentData &seg, DWORD_PTR objAddr, DWORD_PTR MTAddr, size_t objSize, 
    BOOL bVerifyMember);
BOOL VerifyObject(const DacpGcHeapDetails &heap, DWORD_PTR objAddr, DWORD_PTR MTAddr, size_t objSize, 
    BOOL bVerifyMember);

BOOL IsMTForFreeObj(DWORD_PTR pMT);
void DumpStackObjectsHelper (TADDR StackTop, TADDR StackBottom, BOOL verifyFields);


enum ARGTYPE {COBOOL,COSIZE_T,COHEX,COSTRING};
struct CMDOption
{
    const char* name;
    void *vptr;
    ARGTYPE type;
    BOOL hasValue;
    BOOL hasSeen;
};
struct CMDValue
{
    void *vptr;
    ARGTYPE type;
};
BOOL GetCMDOption(const char *string, CMDOption *option, size_t nOption,
                  CMDValue *arg, size_t maxArg, size_t *nArg);

void DumpMDInfo(DWORD_PTR dwStartAddr, CLRDATA_ADDRESS dwRequestedIP = 0, BOOL fStackTraceFormat = FALSE);
void DumpMDInfoFromMethodDescData(DacpMethodDescData * pMethodDescData, BOOL fStackTraceFormat);
void GetDomainList(DWORD_PTR *&domainList, int &numDomain);
HRESULT GetThreadList(DWORD_PTR **threadList, int *numThread);
CLRDATA_ADDRESS GetCurrentManagedThread(); // returns current managed thread if any
void GetAllocContextPtrs(AllocInfo *pallocInfo);

void ReloadSymbolWithLineInfo();

size_t FunctionType (size_t EIP);

size_t Align (size_t nbytes);
// Aligns large objects
size_t AlignLarge (size_t nbytes);

ULONG OSPageSize ();
size_t NextOSPageAddress (size_t addr);

// This version of objectsize reduces the lookup of methodtables in the DAC.
// It uses g_special_mtCache for it's work.
BOOL GetSizeEfficient(DWORD_PTR dwAddrCurrObj, 
    DWORD_PTR dwAddrMethTable, BOOL bLarge, size_t& s, BOOL& bContainsPointers);

BOOL GetCollectibleDataEfficient(DWORD_PTR dwAddrMethTable, BOOL& bCollectible, TADDR& loaderAllocatorObjectHandle);

// ObjSize now uses the methodtable cache for its work too.
size_t ObjectSize (DWORD_PTR obj, BOOL fIsLargeObject=FALSE);
size_t ObjectSize(DWORD_PTR obj, DWORD_PTR mt, BOOL fIsValueClass, BOOL fIsLargeObject=FALSE);

void CharArrayContent(TADDR pos, ULONG num, bool widechar);
void StringObjectContent (size_t obj, BOOL fLiteral=FALSE, const int length=-1);  // length=-1: dump everything in the string object.

UINT FindAllPinnedAndStrong (DWORD_PTR handlearray[],UINT arraySize);
void PrintNotReachableInRange(TADDR rngStart, TADDR rngEnd, BOOL bExcludeReadyForFinalization, 
    HeapStat* stat, BOOL bShort);

const char *EHTypeName(EHClauseType et);

struct StringHolder
{
    LPSTR data;
    StringHolder() : data(NULL) { }
    ~StringHolder() { if(data) delete [] data; }
};


ULONG DebuggeeType();

inline BOOL IsKernelDebugger ()
{
    return DebuggeeType() == DEBUG_CLASS_KERNEL;
}

void    ResetGlobals(void);
HRESULT LoadClrDebugDll(void);
extern "C" void UnloadClrDebugDll(void);

extern IMetaDataImport* MDImportForModule (DacpModuleData *pModule);
extern IMetaDataImport* MDImportForModule (DWORD_PTR pModule);

//*****************************************************************************
//
// **** CQuickBytes
// This helper class is useful for cases where 90% of the time you allocate 512
// or less bytes for a data structure.  This class contains a 512 byte buffer.
// Alloc() will return a pointer to this buffer if your allocation is small
// enough, otherwise it asks the heap for a larger buffer which is freed for
// you.  No mutex locking is required for the small allocation case, making the
// code run faster, less heap fragmentation, etc...  Each instance will allocate
// 520 bytes, so use accordinly.
//
//*****************************************************************************
template <DWORD SIZE, DWORD INCREMENT> 
class CQuickBytesBase
{
public:
    CQuickBytesBase() :
        pbBuff(0),
        iSize(0),
        cbTotal(SIZE)
    { }

    void Destroy()
    {
        if (pbBuff)
        {
            delete[] (BYTE*)pbBuff;
            pbBuff = 0;
        }
    }

    void *Alloc(SIZE_T iItems)
    {
        iSize = iItems;
        if (iItems <= SIZE)
        {
            cbTotal = SIZE;
            return (&rgData[0]);
        }
        else
        {
            if (pbBuff) 
                delete[] (BYTE*)pbBuff;
            pbBuff = new BYTE[iItems];
            cbTotal = pbBuff ? iItems : 0;
            return (pbBuff);
        }
    }

    // This is for conformity to the CQuickBytesBase that is defined by the runtime so
    // that we can use it inside of some GC code that SOS seems to include as well.
    //
    // The plain vanilla "Alloc" version on this CQuickBytesBase doesn't throw either,
    // so we'll just forward the call.
    void *AllocNoThrow(SIZE_T iItems)
    {
        return Alloc(iItems);
    }

    HRESULT ReSize(SIZE_T iItems)
    {
        void *pbBuffNew;
        if (iItems <= cbTotal)
        {
            iSize = iItems;
            return NOERROR;
        }

        pbBuffNew = new BYTE[iItems + INCREMENT];
        if (!pbBuffNew)
            return E_OUTOFMEMORY;
        if (pbBuff) 
        {
            memcpy(pbBuffNew, pbBuff, cbTotal);
            delete[] (BYTE*)pbBuff;
        }
        else
        {
            _ASSERTE(cbTotal == SIZE);
            memcpy(pbBuffNew, rgData, SIZE);
        }
        cbTotal = iItems + INCREMENT;
        iSize = iItems;
        pbBuff = pbBuffNew;
        return NOERROR;
        
    }

    operator PVOID()
    { return ((pbBuff) ? pbBuff : &rgData[0]); }

    void *Ptr()
    { return ((pbBuff) ? pbBuff : &rgData[0]); }

    SIZE_T Size()
    { return (iSize); }

    SIZE_T MaxSize()
    { return (cbTotal); }

    void        *pbBuff;
    SIZE_T      iSize;              // number of bytes used
    SIZE_T      cbTotal;            // total bytes allocated in the buffer
    // use UINT64 to enforce the alignment of the memory
    UINT64 rgData[(SIZE+sizeof(UINT64)-1)/sizeof(UINT64)];
};

#define     CQUICKBYTES_BASE_SIZE           512
#define     CQUICKBYTES_INCREMENTAL_SIZE    128

class CQuickBytesNoDtor : public CQuickBytesBase<CQUICKBYTES_BASE_SIZE, CQUICKBYTES_INCREMENTAL_SIZE>
{
};

class CQuickBytes : public CQuickBytesNoDtor
{
public:
    CQuickBytes() { }

    ~CQuickBytes()
    {
        Destroy();
    }
};

template <DWORD CQUICKBYTES_BASE_SPECIFY_SIZE> 
class CQuickBytesNoDtorSpecifySize : public CQuickBytesBase<CQUICKBYTES_BASE_SPECIFY_SIZE, CQUICKBYTES_INCREMENTAL_SIZE>
{
};

template <DWORD CQUICKBYTES_BASE_SPECIFY_SIZE> 
class CQuickBytesSpecifySize : public CQuickBytesNoDtorSpecifySize<CQUICKBYTES_BASE_SPECIFY_SIZE>
{
public:
    CQuickBytesSpecifySize() { }

    ~CQuickBytesSpecifySize()
    {
        CQuickBytesNoDtorSpecifySize<CQUICKBYTES_BASE_SPECIFY_SIZE>::Destroy();
    }
};


#define STRING_SIZE 10
class CQuickString : public CQuickBytesBase<STRING_SIZE, STRING_SIZE> 
{
public:
    CQuickString() { }

    ~CQuickString()
    {
        Destroy();
    }
    
    void *Alloc(SIZE_T iItems)
    {
        return CQuickBytesBase<STRING_SIZE, STRING_SIZE>::Alloc(iItems*sizeof(WCHAR));
    }

    HRESULT ReSize(SIZE_T iItems)
    {
        return CQuickBytesBase<STRING_SIZE, STRING_SIZE>::ReSize(iItems * sizeof(WCHAR));
    }

    SIZE_T Size()
    {
        return CQuickBytesBase<STRING_SIZE, STRING_SIZE>::Size() / sizeof(WCHAR);
    }

    SIZE_T MaxSize()
    {
        return CQuickBytesBase<STRING_SIZE, STRING_SIZE>::MaxSize() / sizeof(WCHAR);
    }

    WCHAR* String()
    {
        return (WCHAR*) Ptr();
    }

};

enum GetSignatureStringResults
{
    GSS_SUCCESS,
    GSS_ERROR,
    GSS_INSUFFICIENT_DATA,
};

GetSignatureStringResults GetMethodSignatureString (PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, DWORD_PTR dwModuleAddr, CQuickBytes *sigString);
GetSignatureStringResults GetSignatureString (PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, DWORD_PTR dwModuleAddr, CQuickBytes *sigString);
void GetMethodName(mdMethodDef methodDef, IMetaDataImport * pImport, CQuickBytes *fullName);

#ifndef _TARGET_WIN64_
#define     itoa_s_ptr _itoa_s
#define     itow_s_ptr _itow_s
#else
#define     itoa_s_ptr _i64toa_s
#define     itow_s_ptr _i64tow_s
#endif

#ifdef FEATURE_PAL
extern "C"
int  _itoa_s( int inValue, char* outBuffer, size_t inDestBufferSize, int inRadix );
extern "C"
int  _ui64toa_s( unsigned __int64 inValue, char* outBuffer, size_t inDestBufferSize, int inRadix );
#endif // FEATURE_PAL

struct MemRange
{
    MemRange (ULONG64 s = NULL, size_t l = 0, MemRange * n = NULL) 
        : start(s), len (l), next (n)
        {}

    bool InRange (ULONG64 addr)
    {
        return addr >= start && addr < start + len;
    }
        
    ULONG64 start;
    size_t len;
    MemRange * next;
}; //struct MemRange

#ifndef FEATURE_PAL

class StressLogMem
{
private:
    // use a linked list for now, could be optimazied later
    MemRange * list;

    void AddRange (ULONG64 s, size_t l)
    {
        list = new MemRange (s, l, list);
    }
    
public:
    StressLogMem () : list (NULL)
        {}
    ~StressLogMem ();
    bool Init (ULONG64 stressLogAddr, IDebugDataSpaces* memCallBack);
    bool IsInStressLog (ULONG64 addr);
}; //class StressLogMem

// An adapter class that DIA consumes so that it can read PE data from the an image
// This implementation gets the backing data from the image loaded in debuggee memory
// that has been layed out identical to the disk format (ie not seperated by section)
class PEOffsetMemoryReader : IDiaReadExeAtOffsetCallback
{
public:
    PEOffsetMemoryReader(TADDR moduleBaseAddress);

    // IUnknown implementation
    HRESULT __stdcall QueryInterface(REFIID riid, VOID** ppInterface);
    ULONG __stdcall AddRef();
    ULONG __stdcall Release();
    
    // IDiaReadExeAtOffsetCallback implementation
    HRESULT __stdcall ReadExecutableAt(DWORDLONG fileOffset, DWORD cbData, DWORD* pcbData, BYTE data[]);

private:
    TADDR m_moduleBaseAddress;
    volatile ULONG m_refCount;
};

// An adapter class that DIA consumes so that it can read PE data from the an image
// This implementation gets the backing data from the image loaded in debuggee memory
// that has been layed out in LoadLibrary format
class PERvaMemoryReader : IDiaReadExeAtRVACallback
{
public:
    PERvaMemoryReader(TADDR moduleBaseAddress);

    // IUnknown implementation
    HRESULT __stdcall QueryInterface(REFIID riid, VOID** ppInterface);
    ULONG __stdcall AddRef();
    ULONG __stdcall Release();
    
    // IDiaReadExeAtOffsetCallback implementation
    HRESULT __stdcall ReadExecutableAtRVA(DWORD relativeVirtualAddress, DWORD cbData, DWORD* pcbData, BYTE data[]);

private:
    TADDR m_moduleBaseAddress;
    volatile ULONG m_refCount;
};

#endif // !FEATURE_PAL

static const char *SymbolReaderDllName = "SOS.NETCore";
static const char *SymbolReaderClassName = "SOS.SymbolReader";

typedef  int (*ReadMemoryDelegate)(ULONG64, char *, int);
typedef  PVOID (*LoadSymbolsForModuleDelegate)(const char*, BOOL, ULONG64, int, ULONG64, int, ReadMemoryDelegate);
typedef  void (*DisposeDelegate)(PVOID);
typedef  BOOL (*ResolveSequencePointDelegate)(PVOID, const char*, unsigned int, unsigned int*, unsigned int*);
typedef  BOOL (*GetLocalVariableName)(PVOID, int, int, BSTR*);
typedef  BOOL (*GetLineByILOffsetDelegate)(PVOID, mdMethodDef, ULONG64, ULONG *, BSTR*);

class SymbolReader
{
private:
#ifndef FEATURE_PAL
    ISymUnmanagedReader* m_pSymReader;
#endif
    PVOID m_symbolReaderHandle;

    static LoadSymbolsForModuleDelegate loadSymbolsForModuleDelegate;
    static DisposeDelegate disposeDelegate;
    static ResolveSequencePointDelegate resolveSequencePointDelegate;
    static GetLocalVariableName getLocalVariableNameDelegate;
    static GetLineByILOffsetDelegate getLineByILOffsetDelegate;
    static HRESULT PrepareSymbolReader();

    HRESULT GetNamedLocalVariable(___in ISymUnmanagedScope* pScope, ___in ICorDebugILFrame* pILFrame, ___in mdMethodDef methodToken, ___in ULONG localIndex, 
        __out_ecount(paramNameLen) WCHAR* paramName, ___in ULONG paramNameLen, ___out ICorDebugValue** ppValue);
    HRESULT LoadSymbolsForWindowsPDB(___in IMetaDataImport* pMD, ___in ULONG64 peAddress, __in_z WCHAR* pModuleName, ___in BOOL isFileLayout);
    HRESULT LoadSymbolsForPortablePDB(__in_z WCHAR* pModuleName, ___in BOOL isInMemory, ___in BOOL isFileLayout, ___in ULONG64 peAddress, ___in ULONG64 peSize, 
        ___in ULONG64 inMemoryPdbAddress, ___in ULONG64 inMemoryPdbSize);

public:
    SymbolReader()
    {
#ifndef FEATURE_PAL
        m_pSymReader = NULL;
#endif
        m_symbolReaderHandle = 0;
    }

    ~SymbolReader()
    {
#ifndef FEATURE_PAL
        if(m_pSymReader != NULL)
        {
            m_pSymReader->Release();
            m_pSymReader = NULL;
        }
#endif
        if (m_symbolReaderHandle != 0)
        {
            disposeDelegate(m_symbolReaderHandle);
            m_symbolReaderHandle = 0;
        }
    }

    HRESULT LoadSymbols(___in IMetaDataImport* pMD, ___in ICorDebugModule* pModule);
    HRESULT LoadSymbols(___in IMetaDataImport* pMD, ___in IXCLRDataModule* pModule);
    HRESULT GetLineByILOffset(___in mdMethodDef MethodToken, ___in ULONG64 IlOffset, ___out ULONG *pLinenum, __out_ecount(cchFileName) WCHAR* pwszFileName, ___in ULONG cchFileName);
    HRESULT GetNamedLocalVariable(___in ICorDebugFrame * pFrame, ___in ULONG localIndex, __out_ecount(paramNameLen) WCHAR* paramName, ___in ULONG paramNameLen, ___out ICorDebugValue** ppValue);
    HRESULT ResolveSequencePoint(__in_z WCHAR* pFilename, ___in ULONG32 lineNumber, ___in TADDR mod, ___out mdMethodDef* ___out pToken, ___out ULONG32* pIlOffset);
};

HRESULT
GetLineByOffset(
        ___in ULONG64 IP,
        ___out ULONG *pLinenum,
        __out_ecount(cchFileName) WCHAR* pwszFileName,
        ___in ULONG cchFileName);

/// X86 Context
#define X86_SIZE_OF_80387_REGISTERS      80
#define X86_MAXIMUM_SUPPORTED_EXTENSION     512

typedef struct {
    DWORD   ControlWord;
    DWORD   StatusWord;
    DWORD   TagWord;
    DWORD   ErrorOffset;
    DWORD   ErrorSelector;
    DWORD   DataOffset;
    DWORD   DataSelector;
    BYTE    RegisterArea[X86_SIZE_OF_80387_REGISTERS];
    DWORD   Cr0NpxState;
} X86_FLOATING_SAVE_AREA;

typedef struct {

    DWORD ContextFlags;
    DWORD   Dr0;
    DWORD   Dr1;
    DWORD   Dr2;
    DWORD   Dr3;
    DWORD   Dr6;
    DWORD   Dr7;

    X86_FLOATING_SAVE_AREA FloatSave;

    DWORD   SegGs;
    DWORD   SegFs;
    DWORD   SegEs;
    DWORD   SegDs;

    DWORD   Edi;
    DWORD   Esi;
    DWORD   Ebx;
    DWORD   Edx;
    DWORD   Ecx;
    DWORD   Eax;

    DWORD   Ebp;
    DWORD   Eip;
    DWORD   SegCs;
    DWORD   EFlags;
    DWORD   Esp;
    DWORD   SegSs;

    BYTE    ExtendedRegisters[X86_MAXIMUM_SUPPORTED_EXTENSION];

} X86_CONTEXT;

typedef struct {
    ULONGLONG Low;
    LONGLONG High;
} M128A_XPLAT;


/// AMD64 Context
typedef struct {
    WORD   ControlWord;
    WORD   StatusWord;
    BYTE  TagWord;
    BYTE  Reserved1;
    WORD   ErrorOpcode;
    DWORD ErrorOffset;
    WORD   ErrorSelector;
    WORD   Reserved2;
    DWORD DataOffset;
    WORD   DataSelector;
    WORD   Reserved3;
    DWORD MxCsr;
    DWORD MxCsr_Mask;
    M128A_XPLAT FloatRegisters[8];

#if defined(_WIN64)
    M128A_XPLAT XmmRegisters[16];
    BYTE  Reserved4[96];
#else
    M128A_XPLAT XmmRegisters[8];
    BYTE  Reserved4[220];

    DWORD   Cr0NpxState;
#endif

} AMD64_XMM_SAVE_AREA32;

typedef struct {

    DWORD64 P1Home;
    DWORD64 P2Home;
    DWORD64 P3Home;
    DWORD64 P4Home;
    DWORD64 P5Home;
    DWORD64 P6Home;

    DWORD ContextFlags;
    DWORD MxCsr;

    WORD   SegCs;
    WORD   SegDs;
    WORD   SegEs;
    WORD   SegFs;
    WORD   SegGs;
    WORD   SegSs;
    DWORD EFlags;

    DWORD64 Dr0;
    DWORD64 Dr1;
    DWORD64 Dr2;
    DWORD64 Dr3;
    DWORD64 Dr6;
    DWORD64 Dr7;

    DWORD64 Rax;
    DWORD64 Rcx;
    DWORD64 Rdx;
    DWORD64 Rbx;
    DWORD64 Rsp;
    DWORD64 Rbp;
    DWORD64 Rsi;
    DWORD64 Rdi;
    DWORD64 R8;
    DWORD64 R9;
    DWORD64 R10;
    DWORD64 R11;
    DWORD64 R12;
    DWORD64 R13;
    DWORD64 R14;
    DWORD64 R15;

    DWORD64 Rip;

    union {
        AMD64_XMM_SAVE_AREA32 FltSave;
        struct {
            M128A_XPLAT Header[2];
            M128A_XPLAT Legacy[8];
            M128A_XPLAT Xmm0;
            M128A_XPLAT Xmm1;
            M128A_XPLAT Xmm2;
            M128A_XPLAT Xmm3;
            M128A_XPLAT Xmm4;
            M128A_XPLAT Xmm5;
            M128A_XPLAT Xmm6;
            M128A_XPLAT Xmm7;
            M128A_XPLAT Xmm8;
            M128A_XPLAT Xmm9;
            M128A_XPLAT Xmm10;
            M128A_XPLAT Xmm11;
            M128A_XPLAT Xmm12;
            M128A_XPLAT Xmm13;
            M128A_XPLAT Xmm14;
            M128A_XPLAT Xmm15;
        } DUMMYSTRUCTNAME;
    } DUMMYUNIONNAME;

    M128A_XPLAT VectorRegister[26];
    DWORD64 VectorControl;

    DWORD64 DebugControl;
    DWORD64 LastBranchToRip;
    DWORD64 LastBranchFromRip;
    DWORD64 LastExceptionToRip;
    DWORD64 LastExceptionFromRip;

} AMD64_CONTEXT;

typedef struct{
    __int64 LowPart;
    __int64 HighPart;
} FLOAT128_XPLAT;


/// ARM Context
#define ARM_MAX_BREAKPOINTS_CONST     8
#define ARM_MAX_WATCHPOINTS_CONST     1
typedef DECLSPEC_ALIGN(8) struct {

    DWORD ContextFlags;

    DWORD R0;
    DWORD R1;
    DWORD R2;
    DWORD R3;
    DWORD R4;
    DWORD R5;
    DWORD R6;
    DWORD R7;
    DWORD R8;
    DWORD R9;
    DWORD R10;
    DWORD R11;
    DWORD R12;

    DWORD Sp;
    DWORD Lr;
    DWORD Pc;
    DWORD Cpsr;

    DWORD Fpscr;
    DWORD Padding;
    union {
        M128A_XPLAT Q[16];
        ULONGLONG D[32];
        DWORD S[32];
    } DUMMYUNIONNAME;

    DWORD Bvr[ARM_MAX_BREAKPOINTS_CONST];
    DWORD Bcr[ARM_MAX_BREAKPOINTS_CONST];
    DWORD Wvr[ARM_MAX_WATCHPOINTS_CONST];
    DWORD Wcr[ARM_MAX_WATCHPOINTS_CONST];

    DWORD Padding2[2];

} ARM_CONTEXT;

// On ARM this mask is or'ed with the address of code to get an instruction pointer
#ifndef THUMB_CODE
#define THUMB_CODE 1
#endif

///ARM64 Context
#define ARM64_MAX_BREAKPOINTS     8
#define ARM64_MAX_WATCHPOINTS     2
typedef struct {
    
    DWORD ContextFlags;
    DWORD Cpsr;       // NZVF + DAIF + CurrentEL + SPSel
    union {
        struct {
            DWORD64 X0;
            DWORD64 X1;
            DWORD64 X2;
            DWORD64 X3;
            DWORD64 X4;
            DWORD64 X5;
            DWORD64 X6;
            DWORD64 X7;
            DWORD64 X8;
            DWORD64 X9;
            DWORD64 X10;
            DWORD64 X11;
            DWORD64 X12;
            DWORD64 X13;
            DWORD64 X14;
            DWORD64 X15;
            DWORD64 X16;
            DWORD64 X17;
            DWORD64 X18;
            DWORD64 X19;
            DWORD64 X20;
            DWORD64 X21;
            DWORD64 X22;
            DWORD64 X23;
            DWORD64 X24;
            DWORD64 X25;
            DWORD64 X26;
            DWORD64 X27;
            DWORD64 X28;
       };

       DWORD64 X[29];
   };

   DWORD64 Fp;
   DWORD64 Lr;
   DWORD64 Sp;
   DWORD64 Pc;


   M128A_XPLAT V[32];
   DWORD Fpcr;
   DWORD Fpsr;

   DWORD Bcr[ARM64_MAX_BREAKPOINTS];
   DWORD64 Bvr[ARM64_MAX_BREAKPOINTS];
   DWORD Wcr[ARM64_MAX_WATCHPOINTS];
   DWORD64 Wvr[ARM64_MAX_WATCHPOINTS];

} ARM64_CONTEXT;

typedef struct _CROSS_PLATFORM_CONTEXT {

    _CROSS_PLATFORM_CONTEXT() {}

    union {
        X86_CONTEXT       X86Context;
        AMD64_CONTEXT     Amd64Context;
        ARM_CONTEXT       ArmContext;
        ARM64_CONTEXT     Arm64Context;
    };

} CROSS_PLATFORM_CONTEXT, *PCROSS_PLATFORM_CONTEXT;



WString BuildRegisterOutput(const SOSStackRefData &ref, bool printObj = true);
WString MethodNameFromIP(CLRDATA_ADDRESS methodDesc, BOOL bSuppressLines = FALSE, BOOL bAssemblyName = FALSE, BOOL bDisplacement = FALSE);
HRESULT GetGCRefs(ULONG osID, SOSStackRefData **ppRefs, unsigned int *pRefCnt, SOSStackRefError **ppErrors, unsigned int *pErrCount);
WString GetFrameFromAddress(TADDR frameAddr, IXCLRDataStackWalk *pStackwalk = NULL, BOOL bAssemblyName = FALSE);

/* This cache is used to read data from the target process if the reads are known
 * to be sequential.
 */
class LinearReadCache
{
public:
    LinearReadCache(ULONG pageSize = 0x10000);
    ~LinearReadCache();

    /* Reads an address out of the target process, caching the page of memory read.
     * Params:
     *   addr - The address to read out of the target process.
     *   t - A pointer to the data to stuff it in.  We will read sizeof(T) data
     *       from the process and write it into the location t points to.  This
     *       parameter must be non-null.
     * Returns:
     *   True if the read succeeded.  False if it did not, usually as a result
     *   of the memory simply not being present in the target process.
     * Note:
     *   The state of *t is undefined if this function returns false.  We may
     *   have written partial data to it if we return false, so you must
     *   absolutely NOT use it if Read returns false.
     */
    template <class T>
    bool Read(TADDR addr, T *t, bool update = true)
    {
        _ASSERTE(t);

        // Unfortunately the ctor can fail the alloc for the byte array.  In this case
        // we'll just fall back to non-cached reads.
        if (mPage == NULL)
            return MisalignedRead(addr, t);

        // Is addr on the current page?  If not read the page of memory addr is on.
        // If this fails, we will fall back to a raw read out of the process (which
        // is what MisalignedRead does).
        if ((addr < mCurrPageStart) || (addr - mCurrPageStart > mCurrPageSize))
            if (!update || !MoveToPage(addr))
                return MisalignedRead(addr, t);

        // If MoveToPage succeeds, we MUST be on the right page.
        _ASSERTE(addr >= mCurrPageStart);
        
        // However, the amount of data requested may fall off of the page.  In that case,
        // fall back to MisalignedRead.
        TADDR offset = addr - mCurrPageStart;
        if (offset + sizeof(T) > mCurrPageSize)
            return MisalignedRead(addr, t);

        // If we reach here we know we are on the right page of memory in the cache, and
        // that the read won't fall off of the end of the page.
#ifdef _DEBUG
        mReads++;
#endif

        *t = *reinterpret_cast<T*>(mPage+offset);
        return true;
    }

    void EnsureRangeInCache(TADDR start, unsigned int size)
    {
        if (mCurrPageStart == start)
        {
            if (size <= mCurrPageSize)
                return;
            
            // Total bytes to read, don't overflow buffer.
            unsigned int total = size + mCurrPageSize;
            if (total + mCurrPageSize > mPageSize)
                total = mPageSize-mCurrPageSize;

            // Read into the middle of the buffer, update current page size.
            ULONG read = 0;
            HRESULT hr = g_ExtData->ReadVirtual(mCurrPageStart+mCurrPageSize, mPage+mCurrPageSize, total, &read);
            mCurrPageSize += read;

            if (hr != S_OK)
            {
                mCurrPageStart = 0;
                mCurrPageSize = 0;
            }
        }
        else
        {
            MoveToPage(start, size);
        }
    }
    
    void ClearStats()
    {
#ifdef _DEBUG
        mMisses = 0;
        mReads = 0;
        mMisaligned = 0;
#endif
    }
    
    void PrintStats(const char *func)
    {
#ifdef _DEBUG
        char buffer[1024];
        sprintf_s(buffer, _countof(buffer), "Cache (%s): %d reads (%2.1f%% hits), %d misses (%2.1f%%), %d misaligned (%2.1f%%).\n",
                                             func, mReads, 100*(mReads-mMisses)/(float)(mReads+mMisaligned), mMisses,
                                             100*mMisses/(float)(mReads+mMisaligned), mMisaligned, 100*mMisaligned/(float)(mReads+mMisaligned));
        OutputDebugStringA(buffer);
#endif
    }

private:
    /* Sets the cache to the page specified by addr, or false if we could not move to
     * that page.
     */
    bool MoveToPage(TADDR addr, unsigned int size = 0x18);

    /* Attempts to read from the target process if the data is possibly hanging off
     * the end of a page.
     */
    template<class T>
    inline bool MisalignedRead(TADDR addr, T *t)
    {
        ULONG fetched = 0;
        HRESULT hr = g_ExtData->ReadVirtual(addr, (BYTE*)t, sizeof(T), &fetched);

        if (FAILED(hr) || fetched != sizeof(T))
            return false;

        mMisaligned++;
        return true;
    }

private:
    TADDR mCurrPageStart;
    ULONG mPageSize, mCurrPageSize;
    BYTE *mPage;
    
    int mMisses, mReads, mMisaligned;
};


///////////////////////////////////////////////////////////////////////////////////////////
//
// Methods for creating a database out of the gc heap and it's roots in xml format or CLRProfiler format
//

#include <unordered_map>
#include <unordered_set>
#include <list>

class TypeTree;
enum { FORMAT_XML=0, FORMAT_CLRPROFILER=1 };
enum { TYPE_START=0,TYPE_TYPES=1,TYPE_ROOTS=2,TYPE_OBJECTS=3,TYPE_HIGHEST=4};
class HeapTraverser
{
private:
    TypeTree *m_pTypeTree;
    size_t m_curNID;
    FILE *m_file;
    int m_format; // from the enum above
    size_t m_objVisited; // for UI updates
    bool m_verify;
    LinearReadCache mCache;
    
    std::unordered_map<TADDR, std::list<TADDR>> mDependentHandleMap;
    
public:           
    HeapTraverser(bool verify);
    ~HeapTraverser();

    FILE *getFile() { return m_file; }

    BOOL Initialize();
    BOOL CreateReport (FILE *fp, int format);

private:    
    // First all types are added to a tree
    void insert(size_t mTable);
    size_t getID(size_t mTable);    
    
    // Functions for writing to the output file.
    void PrintType(size_t ID,LPCWSTR name);

    void PrintObjectHead(size_t objAddr,size_t typeID,size_t Size);
    void PrintObjectMember(size_t memberValue, bool dependentHandle);
    void PrintLoaderAllocator(size_t memberValue);
    void PrintObjectTail();

    void PrintRootHead();
    void PrintRoot(LPCWSTR kind,size_t Value);
    void PrintRootTail();
    
    void PrintSection(int Type,BOOL bOpening);

    // Root and object member helper functions
    void FindGCRootOnStacks();
    void PrintRefs(size_t obj, size_t methodTable, size_t size);
    
    // Callback functions used during traversals
    static void GatherTypes(DWORD_PTR objAddr,size_t Size,DWORD_PTR methodTable, LPVOID token);
    static void PrintHeap(DWORD_PTR objAddr,size_t Size,DWORD_PTR methodTable, LPVOID token);
    static void PrintOutTree(size_t methodTable, size_t ID, LPVOID token);
    void TraceHandles();
};


class GCRootImpl
{
private:
    struct MTInfo
    {
        TADDR MethodTable;
        WCHAR  *TypeName;

        TADDR *Buffer;
        CGCDesc *GCDesc;

        TADDR LoaderAllocatorObjectHandle;
        bool ArrayOfVC;
        bool ContainsPointers;
        bool Collectible;
        size_t BaseSize;
        size_t ComponentSize;
        
        const WCHAR *GetTypeName()
        {
            if (!TypeName)
                TypeName = CreateMethodTableName(MethodTable);
            
            if (!TypeName)
                return W("<error>");
            
            return TypeName;
        }

        MTInfo()
            : MethodTable(0), TypeName(0), Buffer(0), GCDesc(0),
              ArrayOfVC(false), ContainsPointers(false), Collectible(false), BaseSize(0), ComponentSize(0)
        {
        }

        ~MTInfo()
        {
            if (Buffer)
                delete [] Buffer;

            if (TypeName)
                delete [] TypeName;
        }
    };

    struct RootNode
    {
        RootNode *Next;
        RootNode *Prev;
        TADDR Object;
        MTInfo *MTInfo;

        bool FilledRefs;
        bool FromDependentHandle;
        RootNode *GCRefs;
        
        
        const WCHAR *GetTypeName()
        {
            if (!MTInfo)
                return W("<unknown>");
                
            return MTInfo->GetTypeName();
        }

        RootNode()
            : Next(0), Prev(0)
        {
            Clear();
        }

        void Clear()
        {
            if (Next && Next->Prev == this)
                Next->Prev = NULL;

            if (Prev && Prev->Next == this)
                Prev->Next = NULL;

            Next = 0;
            Prev = 0;
            Object = 0;
            MTInfo = 0;
            FilledRefs = false;
            FromDependentHandle = false;
            GCRefs = 0;
        }
        
        void Remove(RootNode *&list)
        {
            RootNode *curr_next = Next;
            
            // We've already considered this object, remove it.
            if (Prev == NULL)
            {
                // If we've filtered out the head, update it.
                list = curr_next;

                if (curr_next)
                    curr_next->Prev = NULL;
            }
            else
            {
                // Otherwise remove the current item from the list
                Prev->Next = curr_next;

                if (curr_next)
                    curr_next->Prev = Prev;
            }
        }	
    };

public:
    static void GetDependentHandleMap(std::unordered_map<TADDR, std::list<TADDR>> &map);

public:
    // Finds all objects which root "target" and prints the path from the root
    // to "target".  If all is true, all possible paths to the object are printed.
    // If all is false, only completely unique paths will be printed.
    int PrintRootsForObject(TADDR obj, bool all, bool noStacks);

    // Finds a path from root to target if it exists and prints it out.  Returns
    // true if it found a path, false otherwise.
    bool PrintPathToObject(TADDR root, TADDR target);

    // Calculates the size of the closure of objects kept alive by root.
    size_t ObjSize(TADDR root);

    // Walks each root, printing out the total amount of memory held alive by it.
    void ObjSize();

    // Returns the set of all live objects in the process.
    const std::unordered_set<TADDR> &GetLiveObjects(bool excludeFQ = false);

    // See !FindRoots.
    int FindRoots(int gen, TADDR target);

private:
    // typedefs
    typedef void (*ReportCallback)(TADDR root, RootNode *path, bool printHeader);

    // Book keeping and debug.
    void ClearAll();
    void ClearNodes();
    void ClearSizeData();

    // Printing roots
    int PrintRootsOnHandleTable(int gen = -1);
    int PrintRootsOnAllThreads();
    int PrintRootsOnThread(DWORD osThreadId);
    int PrintRootsOnFQ(bool notReadyForFinalization = false);
    int PrintRootsInOlderGen();
    int PrintRootsInRange(LinearReadCache &cache, TADDR start, TADDR stop, ReportCallback func, bool printHeader);

    // Calculate gc root
    RootNode *FilterRoots(RootNode *&list);
    RootNode *FindPathToTarget(TADDR root);
    RootNode *GetGCRefs(RootNode *path, RootNode *node);
    
    void InitDependentHandleMap();

    //Reporting:
    void ReportOneHandlePath(const SOSHandleData &handle, RootNode *node, bool printHeader);
    void ReportOnePath(DWORD thread, const SOSStackRefData &stackRef, RootNode *node, bool printThread, bool printFrame);
    static void ReportOneFQEntry(TADDR root, RootNode *path, bool printHeader);
    static void ReportOlderGenEntry(TADDR root, RootNode *path, bool printHeader);
    void ReportSizeInfo(const SOSHandleData &handle, TADDR obj);
    void ReportSizeInfo(DWORD thread, const SOSStackRefData &ref, TADDR obj);

    // Data reads:
    TADDR ReadPointer(TADDR location);
    TADDR ReadPointerCached(TADDR location);

    // Object/MT data:
    MTInfo *GetMTInfo(TADDR mt);
    DWORD GetComponents(TADDR obj, TADDR mt);
    size_t GetSizeOfObject(TADDR obj, MTInfo *info);

    // RootNode management:
    RootNode *NewNode(TADDR obj = 0, MTInfo *mtinfo = 0, bool fromDependent = false);
    void DeleteNode(RootNode *node);

private:
    
    bool mAll,  // Print all roots or just unique roots?
         mSize; // Print rooting information or total size info?

    std::list<RootNode*> mCleanupList;  // A list of RootNode's we've newed up.  This is only used to delete all of them later.
    std::list<RootNode*> mRootNewList;  // A list of unused RootNodes that are free to use instead of having to "new" up more.
    
    std::unordered_map<TADDR, MTInfo*> mMTs;     // The MethodTable cache which maps from MT -> MethodTable data (size, gcdesc, string typename)
    std::unordered_map<TADDR, RootNode*> mTargets;   // The objects that we are searching for.
    std::unordered_set<TADDR> mConsidered;       // A hashtable of objects we've already visited.
    std::unordered_map<TADDR, size_t> mSizes;   // A mapping from object address to total size of data the object roots.
    
    std::unordered_map<TADDR, std::list<TADDR>> mDependentHandleMap;
    
    LinearReadCache mCache;     // A linear cache which stops us from having to read from the target process more than 1-2 times per object.
};

//
// Helper class used for type-safe bitflags
//   T - the enum type specifying the individual bit flags
//   U - the underlying/storage type
// Requirement:
//   sizeof(T) <= sizeof(U)
//
template <typename T, typename U>
struct Flags
{
    typedef T UnderlyingType;
    typedef U BitFlagEnumType;

    static_assert_no_msg(sizeof(BitFlagEnumType) <= sizeof(UnderlyingType));

    Flags(UnderlyingType v)
        : m_val(v)
    { }

    Flags(BitFlagEnumType v)
        : m_val(v)
    { }

    Flags(const Flags& other)
        : m_val(other.m_val)
    { }

    Flags& operator = (const Flags& other)
    { m_val = other.m_val; return *this; }

    Flags operator | (Flags other) const
    { return Flags<T, U>(m_val | other._val); }

    void operator |= (Flags other)
    { m_val |= other.m_val; }

    Flags operator & (Flags other) const
    { return Flags<T, U>(m_val & other.m_val); }

    void operator &= (Flags other)
    { m_val &= other.m_val; }

    Flags operator ^ (Flags other) const
    { return Flags<T, U>(m_val ^ other._val); }

    void operator ^= (Flags other)
    { m_val ^= other.m_val; }

    BOOL operator == (Flags other) const
    { return m_val == other.m_val; }

    BOOL operator != (Flags other) const
    { return m_val != other.m_val; }


private:
    UnderlyingType m_val;
};

#ifndef FEATURE_PAL

// Flags defining activation policy for COM objects
enum CIOptionsBits 
{
    cciLatestFx     = 0x01,     // look in the most recent .NETFx installation
    cciMatchFx      = 0x02,     // NYI: Look in the .NETFx installation matching the debuggee's runtime
    cciAnyFx        = 0x04,     // look in any .NETFx installation
    cciFxMask       = 0x0f,
    cciDbiColocated = 0x10,     // NYI: Look next to the already loaded DBI module
    cciDacColocated = 0x20,     // Look next to the already loaded DAC module
    cciDbgPath      = 0x40,     // Look in all folders in the debuggers symbols and binary path
};

typedef Flags<DWORD, CIOptionsBits> CIOptions;

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
* CreateInstanceCustom() provides a way to activate a COM object w/o   *
* triggering the FeatureOnDemand dialog. In order to do this we        *
* must avoid using  the CoCreateInstance() API, which, on a machine    *
* with v4+ installed and w/o v2, would trigger this.                   *
* CreateInstanceCustom() activates the requested COM object according  *
* to the specified passed in CIOptions, in the following order         *
* (skipping the steps not enabled in the CIOptions flags passed in):   *
*    1. Attempt to activate the COM object using a framework install:  *
*       a. If the debugger machine has a V4+ shell shim use the shim   *
*          to activate the object                                      *
*       b. Otherwise simply call CoCreateInstance                      *
*    2. If unsuccessful attempt to activate looking for the dllName in *
*       the same folder as the DAC was loaded from                     *
*    3. If unsuccessful attempt to activate the COM object looking in  *
*       every path specified in the debugger's .exepath and .sympath   *
\**********************************************************************/
HRESULT CreateInstanceCustom(
                        REFCLSID clsid,
                        REFIID   iid,
                        LPCWSTR  dllName,
                        CIOptions cciOptions,
                        void** ppItf);


//------------------------------------------------------------------------
// A typesafe version of GetProcAddress
//------------------------------------------------------------------------
template <typename T>
BOOL
GetProcAddressT(
    ___in PCSTR FunctionName,
    __in_opt PCWSTR DllName,
    __inout T* OutFunctionPointer,
    __inout HMODULE* InOutDllHandle
    )
{
    _ASSERTE(InOutDllHandle != NULL);
    _ASSERTE(OutFunctionPointer != NULL);

    T FunctionPointer = NULL;
    HMODULE DllHandle = *InOutDllHandle;
    if (DllHandle == NULL)
    {
        DllHandle = LoadLibraryExW(DllName, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
        if (DllHandle != NULL)
            *InOutDllHandle = DllHandle;
    }
    if (DllHandle != NULL)
    {
        FunctionPointer = (T) GetProcAddress(DllHandle, FunctionName);
    }
    *OutFunctionPointer = FunctionPointer;
    return FunctionPointer != NULL;
}


#endif // FEATURE_PAL

struct ImageInfo
{
    ULONG64 modBase;
};

// Helper class used in ClrStackFromPublicInterface() to keep track of explicit EE Frames
// (i.e., "internal frames") on the stack.  Call Init() with the appropriate
// ICorDebugThread3, and this class will initialize itself with the set of internal
// frames.  You can then call PrintPrecedingInternalFrames during your stack walk to
// have this class output any internal frames that "precede" (i.e., that are closer to
// the leaf than) the specified ICorDebugFrame.
class InternalFrameManager
{
private:
    // TODO: Verify constructor AND destructor is called for each array element
    // TODO: Comment about hard-coding 1000
    ToRelease<ICorDebugInternalFrame2> m_rgpInternalFrame2[1000];
    ULONG32 m_cInternalFramesActual;
    ULONG32 m_iInternalFrameCur;

public:
    InternalFrameManager();
    HRESULT Init(ICorDebugThread3 * pThread3);
    HRESULT PrintPrecedingInternalFrames(ICorDebugFrame * pFrame);

private:
    HRESULT PrintCurrentInternalFrame();
};

#endif // __util_h__
