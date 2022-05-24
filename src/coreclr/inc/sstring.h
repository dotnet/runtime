// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// EString.h  (Safe String)
//

// ---------------------------------------------------------------------------

// ------------------------------------------------------------------------------------------
// EString is the "standard" string representation for the EE.  Its has two purposes.
// (1) it provides an easy-to-use, relatively efficient, string class for APIs to standardize
//      on.
// (2) it completely encapsulates all "unsafe" string operations - that is, string operations
//      which yield possible buffer overrun bugs.  Typesafe use of this API should help guarantee
//      safety.
//
// A EString is conceptually unicode, although the internal conversion might be delayed as long as possible
// Basically it's up to the implementation whether conversion takes place immediately or is delayed, and if
// delayed, what operations trigger the conversion.
//
// Note that anywhere you express a "position" in a string, it is in terms of the Unicode representation of the
// string.
//
// If you need a direct non-unicode representation, you will have to provide a fresh EString which can
// receive a conversion operation if necessary.
//
// The alternate encodings available are:
// 1. ASCII - string consisting entirely of ASCII (7 bit) characters.  This is the only 1 byte encoding
//    guaranteed to be fixed width. Such a string is also a valid instance of all the other 1 byte string
//    representations, and we take advantage of this fact.
// 2. UTF-8 - standard multibyte unicode encoding.
// 3. ANSI - Potentially multibyte encoding using the ANSI page determined by GetACP().
//
// @todo: Note that we could also provide support for several other cases (but currently do not.)
//  - Page specified by GetOEMCP() (OEM page)
//  - Arbitrary page support
//
// @todo: argument & overflow/underflow checking needs to be added
// ------------------------------------------------------------------------------------------


#ifndef _SSTRING_H_
#define _SSTRING_H_

#include "utilcode.h"
#include "sbuffer.h"
#include "debugmacros.h"

// ==========================================================================================
// Documentational typedefs: use these to indicate specific representations of 8 bit strings:
// ==========================================================================================

// Note that LPCSTR means ASCII (7-bit) only!

typedef CHAR ASCII;
typedef ASCII *LPASCII;
typedef const ASCII *LPCASCII;

typedef CHAR ANSI;
typedef ANSI *LPANSI;
typedef const ANSI *LPCANSI;

typedef CHAR UTF8;
typedef UTF8 *LPUTF8;
typedef const UTF8 *LPCUTF8;

// ==========================================================================================
// EString is the base class for safe strings.
// ==========================================================================================

int CaseCompareHelperA(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount);
int CaseCompareHelper(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount);
int CaseHashHelperA(const CHAR *buffer, COUNT_T count);
int CaseHashHelper(const WCHAR *buffer, COUNT_T count);
struct EncodingASCII
{
    using char_t = CHAR;

    inline static int vsnprintf_s(
        char_t*       const buffer,
        size_t        const bufferCount,
        size_t        const maxCount,
        char_t const* const format,
        va_list             argList)
    {
        return _vsnprintf_s(buffer, bufferCount, maxCount, format, argList);
    }

    inline static size_t strlen(const char_t* str)
    {
        return ::strlen(str);
    }

    inline static int strncmp(const char_t* buffer1, const char_t* buffer2, size_t maxCount)
    {
        return ::strncmp(buffer1, buffer2, maxCount);
    }

    inline static errno_t strncpy_s(char_t* dst, size_t bufferSize, const char_t* src, size_t count)
    {
        return ::strncpy_s(dst, bufferSize, src, count);
    }

    inline static int CaseCompareHelper(const char_t* buffer1, const char_t* buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount)
    {
        return CaseCompareHelperA(buffer1, buffer2, count, stopOnNull, stopOnCount);
    }

    inline static int CaseHashHelper(const char_t* buffer, COUNT_T count)
    {
        return ::CaseHashHelperA(buffer, count);
    }
};
struct EncodingUnicode
{
    using char_t = WCHAR;

    inline static int vsnprintf_s(
        char_t*       const buffer,
        size_t        const bufferCount,
        size_t        const maxCount,
        char_t const* const format,
        va_list             argList)
    {
        return _vsnwprintf_s(buffer, bufferCount, maxCount, format, argList);
    }
    
    inline static size_t strlen(const char_t* str)
    {
        return ::wcslen(str);
    }

    inline static int strncmp(const char_t* buffer1, const char_t* buffer2, size_t maxCount)
    {
        return ::wcsncmp(buffer1, buffer2, maxCount);
    }

    inline static errno_t strncpy_s(char_t* dst, size_t bufferSize, const char_t* src, size_t count)
    {
        return ::wcsncpy_s(dst, bufferSize, src, count);
    }

    inline static int CaseCompareHelper(const char_t* buffer1, const char_t* buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount)
    {
        return ::CaseCompareHelper(buffer1, buffer2, count, stopOnNull, stopOnCount);
    }

    inline static int CaseHashHelper(const char_t* buffer, COUNT_T count)
    {
        return ::CaseHashHelper(buffer, count);
    }
};
struct EncodingUTF8
{
    using char_t = CHAR;

    inline static int vsnprintf_s(
        char_t*       const buffer,
        size_t        const bufferCount,
        size_t        const maxCount,
        char_t const* const format,
        va_list             argList)
    {
        return _vsnprintf_s(buffer, bufferCount, maxCount, format, argList);
    }
    
    inline static size_t strlen(const char_t* str)
    {
        return ::strlen(str);
    }

    inline static int strncmp(const char_t* buffer1, const char_t* buffer2, size_t maxCount)
    {
        return ::strncmp(buffer1, buffer2, maxCount);
    }

    inline static errno_t strncpy_s(char_t* dst, size_t bufferSize, const char* src, size_t count)
    {
        return ::strncpy_s(dst, bufferSize, src, count);
    }

    inline static int CaseCompareHelper(const char_t* buffer1, const char_t* buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount)
    {
        return ::CaseCompareHelperA(buffer1, buffer2, count, stopOnNull, stopOnCount);
    }

    inline static int CaseHashHelper(const char_t* buffer, COUNT_T count)
    {
        return ::CaseHashHelperA(buffer, count);
    }
};

template<typename TEncoding>
class EString;

template<typename TEncoding>
using PTR_EString = DPTR(EString<TEncoding>);

using PTR_SString = PTR_EString<EncodingUnicode>;

class StaticStringHelpers
{
    friend struct _DacGlobals;
    template<typename TEncoding>
    friend class EString;
    static const BYTE s_EmptyBuffer[2];
    
    SPTR_DECL(EString<EncodingUnicode>, s_EmptyUnicode);
    SPTR_DECL(EString<EncodingUTF8>, s_EmptyUtf8);
    SPTR_DECL(EString<EncodingASCII>, s_EmptyAscii);

    CHECK CheckStartup();

public:
    StaticStringHelpers() = delete;
    static void Startup();

    // Helpers for CRT function equivalance.
    static int __cdecl _stricmp(const CHAR *buffer1, const CHAR *buffer2);
    static int __cdecl _strnicmp(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count);

    static int __cdecl _wcsicmp(const WCHAR *buffer1, const WCHAR *buffer2);
    static int __cdecl _wcsnicmp(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count);

    // C++ convenience overloads
    static int _tstricmp(const CHAR *buffer1, const CHAR *buffer2);
    static int _tstricmp(const WCHAR *buffer1, const WCHAR *buffer2);

    static int _tstrnicmp(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count);
    static int _tstrnicmp(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count);
};

template<typename TEncoding>
class EString : private SBuffer
{
protected:
    using char_t = typename TEncoding::char_t;
public:
    enum literal_tag_t { Literal };
    EString(void* buffer, COUNT_T size, bool isAllocated);
    class Iterator;
    class CIterator;
    friend HRESULT LoadResourceAndReturnHR(EString<EncodingUnicode>& buffer, CCompRC* pResourceDLL, CCompRC::ResourceCategory eCategory, int resourceID);
    EString();

    EString(literal_tag_t, const char_t* str)
        :SBuffer(Immutable, reinterpret_cast<const BYTE*>(str), static_cast<COUNT_T>(((TEncoding::strlen(str) + 1) * sizeof(char_t))))
    {
    }

    EString(const char_t* str)
        :EString()
    {
        Set(str);
    }
    EString(const char_t* str, COUNT_T count)
        :EString()
    {
        Set(str, count);
    }
    
    EString(const EString &s, const CIterator &start, const CIterator &end)
        :EString()
    {
        Set(s, start, end);
    }

    explicit EString(const EString& s)
        :EString()
    {
        Set(s);
    }

    EString(EString&& s);

    EString(const EString& s, const EString& s1)
        :EString()
    {
        Set(s, s1);
    }

    EString(const EString& s, const EString& s1, const EString& s2)
        :EString()
    {
        Set(s, s1, s2);
    }

    EString(const EString& s, const EString& s1, const EString& s2, const EString& s3)
        :EString()
    {
        Set(s, s1, s2, s3);
    }

    EString(const EString &s, const CIterator &i, COUNT_T count);

    static const EString& Empty();
    
    // Return the number of characters in the string (excluding the terminating NULL).
    BOOL IsEmpty() const;

    // Sets this string to an empty string "".
    void Clear();

    // Truncate the string to the iterator position
    void Truncate(const Iterator &i);

    // Make sure that string buffer has room to grow
    void Preallocate(COUNT_T characters) const;

    // Shrink buffer size as much as possible (reallocate if necessary.)
    void Trim() const;

        // Returns TRUE if this string begins with the contents of s
    BOOL BeginsWith(const EString &s) const { return Match(Begin(), s); }
    BOOL BeginsWithCaseInsensitive(const EString &s) const { return MatchCaseInsensitive(Begin(), s); } // invariant locale

    // Returns TRUE if this string ends with the contents of s
    BOOL EndsWith(const EString& s) const
    {
        WRAPPER_NO_CONTRACT;

        // Need this check due to iterator arithmetic below.
        if (GetCount() < s.GetCount())
        {
            return FALSE;
        }

        return Match(End() - s.GetCount(), s);
    }

    BOOL EndsWithCaseInsensitive(const EString &s) const // invariant locale
    {
        WRAPPER_NO_CONTRACT;

        // Need this check due to iterator arithmetic below.
        if (GetCount() < s.GetCount())
        {
            return FALSE;
        }

        return MatchCaseInsensitive(End() - s.GetCount(), s);
    }
    
    EString &operator= (const EString &s) { WRAPPER_NO_CONTRACT; Set(s); return *this; }
    EString &operator= (EString &&s) { WRAPPER_NO_CONTRACT; Set(s); return *this; } // TODO: Look at stealing the buffer from the rhs value if we'd have to reallocate (and always clear the rhs for deterministic behavior).

    const EString& operator+=(const EString& s)
    {
        WRAPPER_NO_CONTRACT;
        Append(s);
        return *this;
    }

    operator const char_t* () const
    {
        return GetRawBuffer();
    }

    void Append(const EString &s);

    void Append(const char_t* s);

    void Append(char_t c);

    // Do a string comparison. Return 0 if the strings
    // have the same value,  -1 if this is "less than" s, or 1 if
    // this is "greater than" s.
    int Compare(const EString &s) const;
    int CompareCaseInsensitive(const EString& s) const; // invariant locale

    // Do a case sensitive string comparison. Return TRUE if the strings
    // have the same value FALSE if not.
    BOOL Equals(const EString &s) const;
    BOOL EqualsCaseInsensitive(const EString &s) const; // invariant locale

    // Set this string to the concatenation of s1,s2,s3,s4
    void Set(const EString &s);

    void Set(const EString& s1, const EString& s2)
    {
        Preallocate(s1.GetCount() + s2.GetCount());
        Set(s1);
        Append(s2);
    }

    void Set(const EString &s1, const EString &s2, const EString &s3)
    {
        Preallocate(s1.GetCount() + s2.GetCount() + s3.GetCount());
        Set(s1);
        Append(s2);
        Append(s3);
    }

    void Set(const EString &s1, const EString &s2, const EString &s3, const EString &s4)
    {
        Preallocate(s1.GetCount() + s2.GetCount() + s3.GetCount() + s4.GetCount());
        Set(s1);
        Append(s2);
        Append(s3);
        Append(s4);
    }
    
    // Set this string to the substring of s, starting at i, of length characters.
    void Set(const EString &s, const CIterator &i, COUNT_T length);

    // Set this string to the substring of s, starting at start and ending at end (exclusive)
    void Set(const EString &s, const CIterator &start, const CIterator &end);

    // Set this string to a copy of the given string
    void Set(const char_t*string);

    // Set this string to a copy of the first count chars of the given string
    void Set(const char_t*string, COUNT_T count);

    void Set(char_t c);

    // Set this string to point to an existing string. The memory will be shared,
    // so it is the responsibility of the owner of the parameter to ensure lifetime
    // is at least as long as this EString.
    void SetLiteral(const char_t* string);
    void SetPreallocated(const char_t*string, COUNT_T count);

    void Printf(const char_t* format, ...)
    {
        WRAPPER_NO_CONTRACT;

        va_list args;
        va_start(args, format);
        VPrintf(format, args);
        va_end(args);
    }

    void VPrintf(const char_t* format, va_list args);

    void AppendPrintf(const char_t* format, ...)
    {
        WRAPPER_NO_CONTRACT;

        va_list args;
        va_start(args, format);
        AppendVPrintf(format, args);
        va_end(args);
    }

    void AppendVPrintf(const char_t* format, va_list args);

    BOOL FormatMessage(DWORD dwFlags, LPCVOID lpSource, DWORD dwMessageId, DWORD dwLanguageId,
                            const EString &arg1 = Empty(), const EString &arg2 = Empty(),
                            const EString &arg3 = Empty(), const EString &arg4 = Empty(),
                            const EString &arg5 = Empty(), const EString &arg6 = Empty(),
                            const EString &arg7 = Empty(), const EString &arg8 = Empty(),
                            const EString &arg9 = Empty(), const EString &arg10 = Empty()) = delete;

    void Resize(COUNT_T count, SBuffer::Preserve preserve = SBuffer::PRESERVE)
    {
        CONTRACT_VOID
        {
            PRECONDITION(CountToSize(count) >= count);
            POSTCONDITION(GetCount() == count);
            if (count == 0) NOTHROW; else THROWS;
            GC_NOTRIGGER;
            SUPPORTS_DAC_HOST_ONLY;
        }
        CONTRACT_END;

        // If we are resizing to zero, Clear is more efficient
        if (count == 0)
        {
            Clear();
        }
        else
        {
            COUNT_T size = CountToSize(count);

            // detect overflow
            if (size < count)
                ThrowOutOfMemory();

            SBuffer::Resize(size, preserve);

            if (IsImmutable())
                EnsureMutable();

            NullTerminate();
        }

        RETURN;
    }
    
    // Compute a content-based hash value
    ULONG Hash() const;
    ULONG HashCaseInsensitive() const;
    // ------------------------------------------------------------------
    // Iterators:
    // ------------------------------------------------------------------

    // EString splits iterators into two categories.
    //
    // CIterator and Iterator are cheap to create, but allow only read-only
    // access to the string.
    //
    // Normally the user expects to cast Iterators to CIterators transparently, so
    // we provide a constructor on CIterator to support this.

 protected:

    class EMPTY_BASES_DECL Index : public SBuffer::Index
    {
        friend class EString;

        friend class Indexer<const char_t, CIterator>;
        friend class Indexer<char_t, Iterator>;

      protected:
        int               m_characterSizeShift;

        Index();
        Index(EString *string, SCOUNT_T index);
        BYTE &GetAt(SCOUNT_T delta) const;
        void Skip(SCOUNT_T delta);
        SCOUNT_T Subtract(const Index &i) const;
        CHECK DoCheck(SCOUNT_T delta) const;

        void Resync(const EString *string, BYTE *ptr) const;

        const char_t* GetBuffer() const;

      public:
        // Note these should supercede the Indexer versions
        // since this class comes first in the inheritence list
        const char_t& operator*() const;
        char_t& operator*();
        void operator->() const;
        char_t operator[](int index) const;
    };

 public:

    class EMPTY_BASES_DECL CIterator : public Index, public Indexer<const char_t, CIterator>
    {
        friend class EString;

      public:
        const Iterator &ConstCast() const
        {
            return *(const Iterator *)this;
        }

        Iterator &ConstCast()
        {
            return *(Iterator *)this;
        }

        operator const SBuffer::CIterator &() const
        {
            return *(const SBuffer::CIterator *)this;
        }

        operator SBuffer::CIterator &()
        {
            return *(SBuffer::CIterator *)this;
        }

        CIterator()
        {
        }

        CIterator(const EString *string, int index)
          : Index(const_cast<EString *>(string), index)
        {
        }

        // explicitly resolve these for gcc
        const char_t& operator*() const { return Index::operator*(); }
        char_t& operator*() { return Index::operator*(); }
        void operator->() const { Index::operator->(); }
        char_t operator[](int index) const { return Index::operator[](index); }
    };

    class EMPTY_BASES_DECL Iterator : public Index, public Indexer<char_t, Iterator>
    {
        friend class EString;

      public:
        operator const CIterator &() const
        {
            return *(const CIterator *)this;
        }

        operator CIterator &()
        {
            return *(CIterator *)this;
        }

        operator const SBuffer::Iterator &() const
        {
            return *(const SBuffer::Iterator *)this;
        }

        operator SBuffer::Iterator &()
        {
            return *(SBuffer::Iterator *)this;
        }

        Iterator()
        {
        }

        Iterator(EString *string, int index)
          : Index(string, index)
        {
            SUPPORTS_DAC;
        }

        // explicitly resolve these for gcc
        const char_t& operator*() const { return Index::operator*(); }
        void operator->() const { Index::operator->(); }
        char_t operator[](int index) const { return Index::operator[](index); }
    };

    CIterator Begin() const;
    CIterator End() const;

    Iterator Begin();
    Iterator End();

    char_t operator[](int index) { WRAPPER_NO_CONTRACT; return Begin()[index]; }
    char_t operator[](int index) const { WRAPPER_NO_CONTRACT; return Begin()[index]; }
    
    // Delete substring position + length
    void Delete(const Iterator &i, COUNT_T length);

    void Replace(Iterator& i, char_t c)
    {
        *(char_t*)i.m_ptr = c;
    }

    void Replace(const Iterator &i, COUNT_T length, const EString &s);
    void Insert(const Iterator &i, const EString &s);
    void Insert(const Iterator &i, const char_t *s);

    BOOL Find(CIterator &i, const EString &s) const;
    BOOL Find(CIterator &i, const char_t *s) const { return Find(i, EString(EString::Literal, s)); }
    BOOL Find(CIterator &i, char_t s) const;
    BOOL FindBack(CIterator &i, const EString &s) const;
    BOOL FindBack(CIterator &i, const char_t *s) const { return FindBack(i, EString(EString::Literal, s)); }
    BOOL FindBack(CIterator &i, char_t s) const;
    BOOL Skip(CIterator &i, const EString &s) const;
    BOOL Skip(CIterator &i, char_t s) const;
    BOOL Match(const CIterator &i, const EString &s) const;
    BOOL Match(const CIterator &i, char_t s) const;
    BOOL MatchCaseInsensitive(const CIterator &i, const EString &s) const;
    
    //Returns a copy of the string, allocated with new[]. The caller is reponsible for lifetime of the string
    char_t* CreateCopyOfString() const
    {
        char_t* copy = new char_t[GetCount() + 1];
        memcpy(copy, GetRawBuffer(), (GetCount() + 1) * sizeof(char_t));
        return copy;
    }

    COUNT_T GetAllocation()
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        COUNT_T allocation = SBuffer::GetAllocation();
        return ( (allocation > sizeof(WCHAR))
            ? (allocation - sizeof(WCHAR)) / sizeof(WCHAR) : 0 );
    }

    // Conversion/Move routines
    void LowerCase();
    void UpperCase();
    COUNT_T ConvertToUTF8(EString<EncodingUTF8>& resultBuffer) const;
    COUNT_T ConvertToUnicode(EString<EncodingUnicode>& resultBuffer) const;
    EString<EncodingUTF8> MoveToUTF8();
    EString<EncodingUnicode> MoveToUnicode();

    //-------------------------------------------------------------------
    // Accessing the string contents directly
    //-------------------------------------------------------------------

    // To write directly to the EString's underlying buffer:
    // 1) Call OpenBuffer() and pass it the count of characters
    // you need. (Not including the null-terminator).
    // 2) That returns a pointer to the raw buffer which you can write to.
    // 3) When you are done writing to the pointer, call CloseBuffer()
    // and pass it the count of characters you actually wrote (not including
    // the null). The pointer from step 1 is now invalid.

    // example usage:
    // void GetName(EString<EncodingUTF8> & str) {
    //      char * p = str.OpenBuffer(3);
    //      strcpy(p, "Cat");
    //      str.CloseBuffer();
    // }

    // Regarding the null-terminator:
    // 1) Note that we wrote 4 characters (3 + a null). That's ok. OpenBuffer
    // allocates 1 extra byte for the null.
    // 2) If we only wrote 3 characters and no null, that's ok too. CloseBuffer()
    // will add a null-terminator.

    // You should open the buffer, write the data, and immediately close it.
    // No sstring operations are valid while the buffer is opened.
    //
    // In a debug build, Open/Close will do lots of little checks to make sure
    // you don't buffer overflow while it's opened. In a retail build, this
    // is a very streamlined action.
    // Open the raw buffer for writing countChars characters (not including the null).
    char_t *OpenBuffer(COUNT_T maxCharCount)
    {
        return (char_t *)SBuffer::OpenRawBuffer(maxCharCount * sizeof(char_t));
    }

    // Call after OpenBuffer().

    // Provide the count of characters actually used (not including the
    // null terminator). This will make sure the EString's size is correct
    // and that we have a null-terminator.
    void CloseBuffer(COUNT_T finalCount)
    {
        SBuffer::CloseRawBuffer(finalCount * sizeof(char_t));
    }

    // Close the buffer. Assumes that we completely filled the buffer
    // that OpenBuffer() gave back. If we didn't write all the characters,
    // call CloseBuffer(int) instead.
    void CloseBuffer()
    {
        SBuffer::CloseRawBuffer();
    }
    // Return the number of characters in the string (excluding the terminating NULL).
    COUNT_T GetCount() const
    {
        return GetSize() / sizeof(char_t);
    }

#ifdef DACCESS_COMPILE
    // DAC access to string functions.
    // Note that other accessors above are not DAC-safe and will return TARGET pointers into
    // the string instead of copying the string over to the host.
    // @dbgtodo  dac support: Prevent usage of such DAC-unsafe EString APIs in DAC code
    void* DacGetRawContent(void) const
    {
        return SBuffer::DacGetRawContent();
    }

    // Instantiate a copy of the raw buffer in the host.  Requires that the underlying
    // representation is already unicode.
    const WCHAR * DacGetRawUnicode() const = delete;

    // Copy the string from the target into the provided buffer, converting to unicode if necessary
    bool DacGetUnicode(COUNT_T                                  bufChars,
                       _Inout_updates_z_(bufChars) WCHAR * buffer,
                       COUNT_T *                                needChars) const;

    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags) const
    {
        SUPPORTS_DAC;
        SBuffer::EnumMemoryRegions(flags);
    }
#endif

    // -------------------------------------------------------------------
    // Check functions
    // -------------------------------------------------------------------
    CHECK CheckEmpty() const;

    static CHECK CheckCount(COUNT_T count);
    static CHECK CheckRepresentation(int representation);

#if CHECK_INVARIANTS
    static CHECK CheckASCIIString(const ASCII *string) { CHECK_OK; }
    static CHECK CheckASCIIString(const ASCII *string, COUNT_T count) { CHECK_OK; }

    CHECK Check() const;
    CHECK Invariant() const;
    CHECK InternalInvariant() const;
#endif  // CHECK_INVARIANTS
private:
    // -------------------------------------------------------------------
    // Check functions
    // -------------------------------------------------------------------

    CHECK CheckIteratorRange(const CIterator &i) const;
    CHECK CheckIteratorRange(const CIterator &i, COUNT_T length) const;
    void NullTerminate();



    FORCEINLINE char_t* GetRawBuffer() const
    {
        return reinterpret_cast<char_t*>(m_buffer);
    }

    FORCEINLINE static COUNT_T SizeToCount(COUNT_T size)
    {
        return size / sizeof(char_t) - 1;
    }

    FORCEINLINE static COUNT_T CountToSize(COUNT_T count)
    {
        return (count + 1) * sizeof(char_t);
    }
    
    // Minimum guess for Printf buffer size
    const static COUNT_T MINIMUM_GUESS = 20;
    
    void EnsureWritable() const;

    BOOL IsLiteral() const;
    BOOL IsAllocated() const;
};

using SString = EString<EncodingUnicode>;

inline SString SL(const WCHAR* str)
{
    return {SString::Literal, str};
}

// ===========================================================================
// InlineEString is used for stack allocation of strings, or when the string contents
// are expected or known to be small.  Note that it still supports expandability via
// heap allocation if necessary.
// ===========================================================================

template <COUNT_T MEMSIZE, typename TEncoding>
class InlineEString : public EString<TEncoding>
{
    using typename EString<TEncoding>::char_t;
private:
    DAC_ALIGNAS(EString<TEncoding>)
    char_t m_inline[SBUFFER_PADDED_SIZE(MEMSIZE)];
public:
    InlineEString()
        :EString<TEncoding>(m_inline, MEMSIZE, /* isAllocated */ false)
    {

    }

    InlineEString(const char_t c)
        :InlineEString()
    {
        EString<TEncoding>::Set(c);
    }

    InlineEString(const char_t* str)
        :InlineEString()
    {
        EString<TEncoding>::Set(str);
    }

    InlineEString(literal_tag_t, const char_t* str)
        :InlineEString()
    {
        EString<TEncoding>::SetLiteral(str);
    }

    InlineEString(const char_t* str, COUNT_T count)
        :InlineEString()
    {
        EString<TEncoding>::Set(str, count);
    }
};

template<COUNT_T MEMSIZE>
using InlineSString = InlineEString<MEMSIZE, EncodingUnicode>;

// ================================================================================
// StackEString is a lot like CQuickBytes.  Use it to create an EString object
// using some stack space as a preallocated buffer.
// ================================================================================

template<typename TEncoding>
using StackEString = InlineEString<512, TEncoding>;
using StackSString = StackEString<EncodingUnicode>;

// This is a smaller version for when it is known that the string that's going to
// be needed is small and it's preferable not to take up the stack space.
template<typename TEncoding>
using SmallStackEString = InlineEString<32, TEncoding>;
using SmallStackSString = SmallStackEString<EncodingUnicode>;

// To be used specifically for path strings.
#ifdef _DEBUG
// This is a smaller version for debug builds to exercise the buffer allocation path
typedef InlineEString<32, EncodingUnicode> PathString;
typedef InlineEString<2 * 32, EncodingUnicode> LongPathString;
#else
// Set it to the current MAX_PATH
typedef InlineEString<260, EncodingUnicode> PathString;
typedef InlineEString<2 * 260, EncodingUnicode> LongPathString;
#endif

BOOL LoadResource(EString<EncodingUnicode>& buffer, CCompRC::ResourceCategory eCategory, int resourceID);
HRESULT LoadResourceAndReturnHR(EString<EncodingUnicode>& buffer, CCompRC::ResourceCategory eCategory, int resourceID);
HRESULT LoadResourceAndReturnHR(EString<EncodingUnicode>& buffer, CCompRC* pResourceDLL, CCompRC::ResourceCategory eCategory, int resourceID);

// ================================================================================
// Inline definitions
// ================================================================================

#include <sstring.inl>

#endif  // _SSTRING_H_
