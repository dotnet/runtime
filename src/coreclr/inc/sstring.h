// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// SString.h  (Safe String)
//

// ---------------------------------------------------------------------------

// ------------------------------------------------------------------------------------------
// SString is the "standard" string representation for the EE.  Its has two purposes.
// (1) it provides an easy-to-use, relatively efficient, string class for APIs to standardize
//      on.
// (2) it completely encapsulates all "unsafe" string operations - that is, string operations
//      which yield possible buffer overrun bugs.  Typesafe use of this API should help guarantee
//      safety.
//
// A SString is conceptually unicode, although the internal conversion might be delayed as long as possible
// Basically it's up to the implementation whether conversion takes place immediately or is delayed, and if
// delayed, what operations trigger the conversion.
//
// Note that anywhere you express a "position" in a string, it is in terms of the Unicode representation of the
// string.
//
// If you need a direct non-unicode representation, you will have to provide a fresh SString which can
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
// SString is the base class for safe strings.
// ==========================================================================================


typedef DPTR(class SString) PTR_SString;
class EMPTY_BASES_DECL SString : private SBuffer
{
    friend struct _DacGlobals;

private:
    enum Representation
    {
        // Note: bits are meaningful:      xVS  V == Variable?  S == Single byte width?
        REPRESENTATION_EMPTY    = 0x00, // 000
        REPRESENTATION_UNICODE  = 0x04, // 100
        REPRESENTATION_ASCII    = 0x01, // 001
        REPRESENTATION_UTF8     = 0x03, // 011
        REPRESENTATION_ANSI     = 0x07, // 111

        REPRESENTATION_VARIABLE_MASK    = 0x02,
        REPRESENTATION_SINGLE_MASK      = 0x01,
        REPRESENTATION_MASK             = 0x07,
    };

    // Minimum guess for Printf buffer size
    const static COUNT_T MINIMUM_GUESS = 20;


#ifdef _DEBUG
    // Used to have a public ctor of this form - made it too easy to lose
    // utf8 info by accident. Now you have to specify the representation type
    // explicitly - this privator ctor prevents reinsertion of this ctor.
    explicit SString(const ASCII *)
    {
        _ASSERTE(!"Don't call this.");
    }
#endif

  protected:
    class Index;
    class UIndex;

    friend class Index;
    friend class UIndex;

  public:

    // UIterator is character-level assignable.
    class UIterator;

    // CIterators/Iterator'string must be modified by SString APIs.
    class CIterator;
    class Iterator;

    // Tokens for constructor overloads
    enum tagUTF8Literal { Utf8Literal };
    enum tagLiteral { Literal };
    enum tagUTF8 { Utf8 };
    enum tagANSI { Ansi };
    enum tagASCII {Ascii };

    static void Startup();
    static CHECK CheckStartup();

    static const SString &Empty();

    SString();

    explicit SString(const SString &s);

    SString(const SString &s1, const SString &s2);
    SString(const SString &s1, const SString &s2, const SString &s3);
    SString(const SString &s1, const SString &s2, const SString &s3, const SString &s4);
    SString(const SString &s, const CIterator &i, COUNT_T length);
    SString(const SString &s, const CIterator &start, const CIterator &end);
    SString(const WCHAR *string);
    SString(const WCHAR *string, COUNT_T count);
    SString(enum tagASCII dummyTag, const ASCII *string);
    SString(enum tagASCII dummyTag, const ASCII *string, COUNT_T count);
    SString(enum tagUTF8 dummytag, const UTF8 *string);
    SString(enum tagUTF8 dummytag, const UTF8 *string, COUNT_T count);
    SString(enum tagANSI dummytag, const ANSI *string);
    SString(enum tagANSI dummytag, const ANSI *string, COUNT_T count);
    SString(WCHAR character);

    // NOTE: Literals MUST be read-only never-freed strings.
    SString(enum tagLiteral dummytag, const CHAR *literal);
    SString(enum tagUTF8Literal dummytag, const UTF8 *literal);
    SString(enum tagLiteral dummytag, const WCHAR *literal);
    SString(enum tagLiteral dummytag, const WCHAR *literal, COUNT_T count);

    // Set this string to the concatenation of s1,s2,s3,s4
    void Set(const SString &s);
    void Set(const SString &s1, const SString &s2);
    void Set(const SString &s1, const SString &s2, const SString &s3);
    void Set(const SString &s1, const SString &s2, const SString &s3, const SString &s4);

    // Set this string to the substring of s, starting at i, of length characters.
    void Set(const SString &s, const CIterator &i, COUNT_T length);

    // Set this string to the substring of s, starting at start and ending at end (exclusive)
    void Set(const SString &s, const CIterator &start, const CIterator &end);

    // Set this string to a copy of the given string
    void Set(const WCHAR *string);
    void SetASCII(const ASCII *string);
    void SetUTF8(const UTF8 *string);
    void SetANSI(const ANSI *string);

    // Set this string to a copy of the first count chars of the given string
    void Set(const WCHAR *string, COUNT_T count);

    // Set this string to a prellocated copy of a given string.
    // The caller is the owner of the bufffer and has to coordinate its lifetime.
    void SetPreallocated(const WCHAR *string, COUNT_T count);

    void SetASCII(const ASCII *string, COUNT_T count);

    void SetUTF8(const UTF8 *string, COUNT_T count);
    void SetANSI(const ANSI *string, COUNT_T count);

    // Set this string to the unicode character
    void Set(WCHAR character);

    // Set this string to the UTF8 character
    void SetUTF8(CHAR character);

    // Set this string to the given literal. We share the mem and don't make a copy.
    void SetLiteral(const CHAR *literal);
    void SetLiteral(const WCHAR *literal);

    // ------------------------------------------------------------------
    // Public operations
    // ------------------------------------------------------------------

    // Normalizes the string representation to unicode.  This can be used to
    // make basic read-only operations non-failing.
    void Normalize() const;

    // Return the number of characters in the string (excluding the terminating NULL).
    COUNT_T GetCount() const;
    BOOL IsEmpty() const;

    // Return whether a single byte string has all characters which fit in the ASCII set.
    // (Note that this will return FALSE if the string has been converted to unicode for any
    // reason.)
    BOOL IsASCII() const;

    // !!!!!!!!!!!!!! WARNING about case insensitive operations !!!!!!!!!!!!!!!
    //
    //                 THIS IS NOT SUPPORTED FULLY ON WIN9x
    //      SString case-insensitive comparison is based off LCMapString,
    //      which does not work on characters outside the current OS code page.
    //
    //      Case insensitive code in SString is primarily targeted at
    //      supporting path comparisons, which is supported correctly on 9x,
    //      since file system names are limited to the OS code page.
    //
    // !!!!!!!!!!!!!! WARNING about case insensitive operations !!!!!!!!!!!!!!!

    // Compute a content-based hash value
    ULONG Hash() const;
    ULONG HashCaseInsensitive() const;

    // Do a string comparison. Return 0 if the strings
    // have the same value,  -1 if this is "less than" s, or 1 if
    // this is "greater than" s.
    int Compare(const SString &s) const;
    int CompareCaseInsensitive(const SString &s) const; // invariant locale

    // Do a case sensitive string comparison. Return TRUE if the strings
    // have the same value FALSE if not.
    BOOL Equals(const SString &s) const;
    BOOL EqualsCaseInsensitive(const SString &s) const; // invariant locale

    // Match s to a portion of the string starting at the position.
    // Return TRUE if the strings have the same value
    // (regardless of representation), FALSE if not.
    BOOL Match(const CIterator &i, const SString &s) const;
    BOOL MatchCaseInsensitive(const CIterator &i, const SString &s) const; // invariant locale

    BOOL Match(const CIterator &i, WCHAR c) const;
    BOOL MatchCaseInsensitive(const CIterator &i, WCHAR c) const; // invariant locale

    // Like match, but advances the iterator past the match
    // if successful
    BOOL Skip(CIterator &i, const SString &s) const;
    BOOL Skip(CIterator &i, WCHAR c) const;

    // Start searching for a match of the given string, starting at
    // the given iterator point.
    // If a match exists, move the iterator to point to the nearest
    // occurrence of s in the string and return TRUE.
    // If no match exists, return FALSE and leave the iterator unchanged.
    BOOL Find(CIterator &i, const SString &s) const;
    BOOL Find(CIterator &i, const WCHAR *s) const;
    BOOL FindASCII(CIterator &i, const ASCII *s) const;
    BOOL FindUTF8(CIterator &i, const UTF8 *s) const;
    BOOL Find(CIterator &i, WCHAR c) const;

    BOOL FindBack(CIterator &i, const SString &s) const;
    BOOL FindBack(CIterator &i, const WCHAR *s) const;
    BOOL FindBackASCII(CIterator &i, const ASCII *s) const;
    BOOL FindBackUTF8(CIterator &i, const UTF8 *s) const;
    BOOL FindBack(CIterator &i, WCHAR c) const;

    // Returns TRUE if this string begins with the contents of s
    BOOL BeginsWith(const SString &s) const;
    BOOL BeginsWithCaseInsensitive(const SString &s) const; // invariant locale

    // Returns TRUE if this string ends with the contents of s
    BOOL EndsWith(const SString &s) const;
    BOOL EndsWithCaseInsensitive(const SString &s) const; // invariant locale

    // Sets this string to an empty string "".
    void Clear();

    // Truncate the string to the iterator position
    void Truncate(const Iterator &i);

    // Append s to the end of this string.
    void Append(const SString &s);
    void Append(const WCHAR *s);
    void AppendASCII(const CHAR *s);
    void AppendUTF8(const CHAR *s);

    // Append char c to the end of this string.
    void Append(const WCHAR c);
    void AppendUTF8(const CHAR c);

    // Insert s into this string at the 'position'th character.
    void Insert(const Iterator &i, const SString &s);
    void Insert(const Iterator &i, const WCHAR *s);
    void InsertASCII(const Iterator &i, const CHAR *s);
    void InsertUTF8(const Iterator &i, const CHAR *s);

    // Delete substring position + length
    void Delete(const Iterator &i, COUNT_T length);

    // Replace character at i with c
    void Replace(const Iterator &i, WCHAR c);

    // Replace substring at (i,i+length) with s
    void Replace(const Iterator &i, COUNT_T length, const SString &s);

    // Make sure that string buffer has room to grow
    void Preallocate(COUNT_T characters) const;

    // Shrink buffer size as much as possible (reallocate if necessary.)
    void Trim() const;

    // ------------------------------------------------------------------
    // Iterators:
    // ------------------------------------------------------------------

    // SString splits iterators into two categories.
    //
    // CIterator and Iterator are cheap to create, but allow only read-only
    // access to the string.
    //
    // UIterator forces a unicode conversion, but allows
    // assignment to individual string characters.  They are also a bit more
    // efficient once created.

    // ------------------------------------------------------------------
    // UIterator:
    // ------------------------------------------------------------------

 protected:

    class EMPTY_BASES_DECL UIndex : public SBuffer::Index
    {
        friend class SString;
        friend class Indexer<WCHAR, UIterator>;

      protected:

        UIndex();
        UIndex(SString *string, SCOUNT_T index);
        WCHAR &GetAt(SCOUNT_T delta) const;
        void Skip(SCOUNT_T delta);
        SCOUNT_T Subtract(const UIndex &i) const;
        CHECK DoCheck(SCOUNT_T delta) const;

        WCHAR *GetUnicode() const;
    };

 public:

    class EMPTY_BASES_DECL UIterator : public UIndex, public Indexer<WCHAR, UIterator>
    {
        friend class SString;

    public:
        UIterator()
        {
        }

        UIterator(SString *string, int index)
          : UIndex(string, index)
        {
        }
    };

    UIterator BeginUnicode();
    UIterator EndUnicode();

    // For CIterator & Iterator, we try our best to iterate the string without
    // modifying it. (Currently, we do require an ASCII or Unicode string
    // for simple WCHAR retrival, but you could imagine being more flexible
    // going forward - perhaps even supporting iterating multibyte encodings
    // directly.)
    //
    // Because of the runtime-changable nature of the string, CIterators
    // require an extra member to record the character size. They also
    // are unable to properly implement GetAt as required by the template
    // (since there may not be a direct WCHAR pointer), so they provide
    // further customization in a subclass.
    //
    // Normally the user expects to cast Iterators to CIterators transparently, so
    // we provide a constructor on CIterator to support this.

 protected:

    class EMPTY_BASES_DECL Index : public SBuffer::Index
    {
        friend class SString;

        friend class Indexer<const WCHAR, CIterator>;
        friend class Indexer<WCHAR, Iterator>;

      protected:
        int               m_characterSizeShift;

        Index();
        Index(SString *string, SCOUNT_T index);
        BYTE &GetAt(SCOUNT_T delta) const;
        void Skip(SCOUNT_T delta);
        SCOUNT_T Subtract(const Index &i) const;
        CHECK DoCheck(SCOUNT_T delta) const;

        void Resync(const SString *string, BYTE *ptr) const;

        const WCHAR *GetUnicode() const;
        const CHAR *GetASCII() const;

      public:
        // Note these should supercede the Indexer versions
        // since this class comes first in the inheritence list
        WCHAR operator*() const;
        void operator->() const;
        WCHAR operator[](int index) const;
    };

 public:

    class EMPTY_BASES_DECL CIterator : public Index, public Indexer<const WCHAR, CIterator>
    {
        friend class SString;

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

        CIterator(const SString *string, int index)
          : Index(const_cast<SString *>(string), index)
        {
        }

        // explicitly resolve these for gcc
        WCHAR operator*() const { return Index::operator*(); }
        void operator->() const { Index::operator->(); }
        WCHAR operator[](int index) const { return Index::operator[](index); }
    };

    class EMPTY_BASES_DECL Iterator : public Index, public Indexer<WCHAR, Iterator>
    {
        friend class SString;

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

        Iterator(SString *string, int index)
          : Index(string, index)
        {
            SUPPORTS_DAC;
        }

        // explicitly resolve these for gcc
        WCHAR operator*() const { return Index::operator*(); }
        void operator->() const { Index::operator->(); }
        WCHAR operator[](int index) const { return Index::operator[](index); }
    };

    CIterator Begin() const;
    CIterator End() const;

    Iterator Begin();
    Iterator End();

    // ------------------------------------------------------------------
    // Conversion:
    // ------------------------------------------------------------------

    // Get a const pointer to the string in the current representation.
    // This pointer can not be cached because it will become invalid if
    // the SString changes representation or reallocates its buffer.

    // You can always get a unicode string.  This will force a conversion
    // if necessary.
    const WCHAR *GetUnicode() const;
    const WCHAR *GetUnicode(const CIterator &i) const;

    void LowerCase();
    void UpperCase();

    // Helper function to convert string in-place to lower-case (no allocation overhead for SString instance)
    static void LowerCase(__inout_z LPWSTR wszString);

    // These routines will use the given scratch string if necessary
    // to perform a conversion to the desired representation

    // Use a local declaration of InlineScratchBuffer or StackScratchBuffer for parameters of
    // AbstractScratchBuffer.
    class AbstractScratchBuffer;

    // These routines will use the given scratch buffer if necessary
    // to perform a conversion to the desired representation.  Note that
    // the lifetime of the pointer return is limited by BOTH the
    // scratch string and the source (this) string.
    //
    // Typical usage:
    //
    // SString *s = ...;
    // {
    //   StackScratchBuffer buffer;
    //   const UTF8 *utf8 = s->GetUTF8(buffer);
    //   CallFoo(utf8);
    // }
    // // No more pointers to returned buffer allowed.

    const UTF8 *GetUTF8(AbstractScratchBuffer &scratch) const;
    const UTF8 *GetUTF8(AbstractScratchBuffer &scratch, COUNT_T *pcbUtf8) const;
    const ANSI *GetANSI(AbstractScratchBuffer &scratch) const;

    // Used when the representation is known, throws if the representation doesn't match
    const UTF8 *GetUTF8NoConvert() const;

    // Converts/copies into the given output string
    void ConvertToUnicode(SString &dest) const;
    void ConvertToANSI(SString &dest) const;
    COUNT_T ConvertToUTF8(SString &dest) const;

    //-------------------------------------------------------------------
    // Accessing the string contents directly
    //-------------------------------------------------------------------

    // To write directly to the SString's underlying buffer:
    // 1) Call OpenXXXBuffer() and pass it the count of characters
    // you need. (Not including the null-terminator).
    // 2) That returns a pointer to the raw buffer which you can write to.
    // 3) When you are done writing to the pointer, call CloseBuffer()
    // and pass it the count of characters you actually wrote (not including
    // the null). The pointer from step 1 is now invalid.

    // example usage:
    // void GetName(SString & str) {
    //      char * p = str.OpenANSIBuffer(3);
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
    WCHAR *OpenUnicodeBuffer(COUNT_T maxCharCount);
    UTF8 *OpenUTF8Buffer(COUNT_T maxSingleCharCount);
    ANSI *OpenANSIBuffer(COUNT_T maxSingleCharCount);

    //Returns the unicode string, the caller is reponsible for lifetime of the string
    WCHAR *GetCopyOfUnicodeString();

    // Get the max size that can be passed to OpenUnicodeBuffer without causing allocations.
    COUNT_T GetUnicodeAllocation();

    // Call after OpenXXXBuffer().

    // Provide the count of characters actually used (not including the
    // null terminator). This will make sure the SString's size is correct
    // and that we have a null-terminator.
    void CloseBuffer(COUNT_T finalCount);

    // Close the buffer. Assumes that we completely filled the buffer
    // that OpenBuffer() gave back. If we didn't write all the characters,
    // call CloseBuffer(int) instead.
    void CloseBuffer();

#ifdef DACCESS_COMPILE
    // DAC access to string functions.
    // Note that other accessors above are not DAC-safe and will return TARGET pointers into
    // the string instead of copying the string over to the host.
    // @dbgtodo  dac support: Prevent usage of such DAC-unsafe SString APIs in DAC code

    // Instantiate a copy of the raw buffer in the host and return a pointer to it
    void * DacGetRawContent() const;

    // Instantiate a copy of the raw buffer in the host.  Requires that the underlying
    // representation is already unicode.
    const WCHAR * DacGetRawUnicode() const;

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

    //---------------------------------------------------------------------
    // Utilities
    //---------------------------------------------------------------------

    // WARNING: The MBCS version of printf function are factory for globalization
    // issues when used to format Unicode strings (%S). The Unicode versions are
    // preferred in this case.
    void Printf(const CHAR *format, ...);
    void VPrintf(const CHAR *format, va_list args);
    void AppendPrintf(const CHAR *format, ...);
    void AppendVPrintf(const CHAR *format, va_list args);

    void Printf(const WCHAR *format, ...);

public:
    BOOL LoadResource(CCompRC::ResourceCategory eCategory, int resourceID);
    HRESULT LoadResourceAndReturnHR(CCompRC::ResourceCategory eCategory, int resourceID);
    HRESULT LoadResourceAndReturnHR(CCompRC* pResourceDLL, CCompRC::ResourceCategory eCategory, int resourceID);
    BOOL FormatMessage(DWORD dwFlags, LPCVOID lpSource, DWORD dwMessageId, DWORD dwLanguageId,
                       const SString &arg1 = Empty(), const SString &arg2 = Empty(),
                       const SString &arg3 = Empty(), const SString &arg4 = Empty(),
                       const SString &arg5 = Empty(), const SString &arg6 = Empty(),
                       const SString &arg7 = Empty(), const SString &arg8 = Empty(),
                       const SString &arg9 = Empty(), const SString &arg10 = Empty());

#if 1
    // @todo - get rid of this and move it outside of SString
    void MakeFullNamespacePath(const SString &nameSpace, const SString &name);
#endif

    //--------------------------------------------------------------------
    // Operators
    //--------------------------------------------------------------------

    operator const WCHAR * () const { WRAPPER_NO_CONTRACT; return GetUnicode(); }

    WCHAR operator[](int index) { WRAPPER_NO_CONTRACT; return Begin()[index]; }
    WCHAR operator[](int index) const { WRAPPER_NO_CONTRACT; return Begin()[index]; }

    SString &operator= (const SString &s) { WRAPPER_NO_CONTRACT; Set(s); return *this; }
    SString &operator+= (const SString &s) { WRAPPER_NO_CONTRACT; Append(s); return *this; }

    // -------------------------------------------------------------------
    // Check functions
    // -------------------------------------------------------------------

    CHECK CheckIteratorRange(const CIterator &i) const;
    CHECK CheckIteratorRange(const CIterator &i, COUNT_T length) const;
    CHECK CheckEmpty() const;

    static CHECK CheckCount(COUNT_T count);
    static CHECK CheckRepresentation(int representation);

#if CHECK_INVARIANTS
    static CHECK CheckASCIIString(const ASCII *string);
    static CHECK CheckASCIIString(const ASCII *string, COUNT_T count);

    CHECK Check() const;
    CHECK Invariant() const;
    CHECK InternalInvariant() const;
#endif  // CHECK_INVARIANTS

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

    // -------------------------------------------------------------------
    // Internal routines
    // -------------------------------------------------------------------


 protected:
    // Use this via InlineSString<X>
    SString(void *buffer, COUNT_T size);

 private:
    static int CaseCompareHelperA(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount);
    static int CaseCompareHelper(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount);

    // Internal helpers:

    static const BYTE s_EmptyBuffer[2];

    static UINT s_ACP;

    SPTR_DECL(SString,s_Empty);

    COUNT_T GetRawCount() const;

    // Get buffer as appropriate string rep
    ASCII *GetRawASCII() const;
    UTF8 *GetRawUTF8() const;
    ANSI *GetRawANSI() const;
    WCHAR *GetRawUnicode() const;

    void InitEmpty();

    Representation GetRepresentation() const;
    void SetRepresentation(Representation representation);
    BOOL IsRepresentation(Representation representation) const;
    BOOL IsFixedSize() const;
    BOOL IsIteratable() const;
    BOOL IsSingleByte() const;

    int GetCharacterSizeShift() const;

    COUNT_T SizeToCount(COUNT_T size) const;
    COUNT_T CountToSize(COUNT_T count) const;

    COUNT_T GetBufferSizeInCharIncludeNullChar() const;

    BOOL IsLiteral() const;
    BOOL IsAllocated() const;
    BOOL IsBufferOpen() const;
    BOOL IsASCIIScanned() const;
    void SetASCIIScanned() const;
    void SetNormalized() const;
    BOOL IsNormalized() const;
    void ClearNormalized() const;

    void EnsureWritable() const;
    void ConvertToFixed() const;
    void ConvertToIteratable() const;

    void ConvertASCIIToUnicode(SString &dest) const;
    void ConvertToUnicode() const;
    void ConvertToUnicode(const CIterator &i) const;

    const SString &GetCompatibleString(const SString &s, SString &scratch) const;
    const SString &GetCompatibleString(const SString &s, SString &scratch, const CIterator &i) const;
    BOOL ScanASCII() const;
    void NullTerminate();

    void Resize(COUNT_T count, Representation representation,
                Preserve preserve = DONT_PRESERVE);

    void OpenBuffer(Representation representation, COUNT_T countChars);
};

// ===========================================================================
// InlineSString is used for stack allocation of strings, or when the string contents
// are expected or known to be small.  Note that it still supports expandability via
// heap allocation if necessary.
// ===========================================================================

template <COUNT_T MEMSIZE>
class EMPTY_BASES_DECL InlineSString : public SString
{
private:
    DAC_ALIGNAS(SString)
    BYTE m_inline[SBUFFER_PADDED_SIZE(MEMSIZE)];

public:
    FORCEINLINE InlineSString()
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE InlineSString(const SString &s)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(s);
    }

    FORCEINLINE InlineSString(const SString &s1, const SString &s2)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(s1, s2);
    }

    FORCEINLINE InlineSString(const SString &s1, const SString &s2, const SString &s3)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(s1, s2, s3);
    }

    FORCEINLINE InlineSString(const SString &s1, const SString &s2, const SString &s3, const SString &s4)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(s1, s2, s3, s4);
    }

    FORCEINLINE InlineSString(const SString &s, const CIterator &start, const CIterator &end)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(s, start, end);
    }

    FORCEINLINE InlineSString(const SString &s, const CIterator &i, COUNT_T length)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(s, i, length);
    }

    FORCEINLINE InlineSString(const WCHAR *string)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(string);
    }

    FORCEINLINE InlineSString(const WCHAR *string, COUNT_T count)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(string, count);
    }

    FORCEINLINE InlineSString(enum tagASCII, const CHAR *string)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        SetASCII(string);
    }

    FORCEINLINE InlineSString(enum tagASCII, const CHAR *string, COUNT_T count)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        SetASCII(string, count);
    }

    FORCEINLINE InlineSString(tagUTF8 dummytag, const UTF8 *string)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        SetUTF8(string);
    }

    FORCEINLINE InlineSString(tagUTF8 dummytag, const UTF8 *string, COUNT_T count)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        SetUTF8(string, count);
    }

    FORCEINLINE InlineSString(enum tagANSI dummytag, const ANSI *string)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        SetANSI(string);
    }

    FORCEINLINE InlineSString(enum tagANSI dummytag, const ANSI *string, COUNT_T count)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        SetANSI(string, count);
    }

    FORCEINLINE InlineSString(WCHAR character)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        Set(character);
    }

    FORCEINLINE InlineSString(tagUTF8 dummytag, const UTF8 character)
      : SString(m_inline, SBUFFER_PADDED_SIZE(MEMSIZE))
    {
        WRAPPER_NO_CONTRACT;
        SetUTF8(character);
    }

    FORCEINLINE InlineSString<MEMSIZE> &operator= (const SString &s)
    {
        WRAPPER_NO_CONTRACT;
        Set(s);
        return *this;
    }

    FORCEINLINE InlineSString<MEMSIZE> &operator= (const InlineSString<MEMSIZE> &s)
    {
        WRAPPER_NO_CONTRACT;
        Set(s);
        return *this;
    }
};

// ================================================================================
// StackSString is a lot like CQuickBytes.  Use it to create an SString object
// using some stack space as a preallocated buffer.
// ================================================================================

typedef InlineSString<512> StackSString;

// This is a smaller version for when it is known that the string that's going to
// be needed is small and it's preferable not to take up the stack space.
typedef InlineSString<32>  SmallStackSString;

// To be used specifically for path strings.
#ifdef _DEBUG
// This is a smaller version for debug builds to exercise the buffer allocation path
typedef InlineSString<32> PathString;
typedef InlineSString<2 * 32> LongPathString;
#else
// Set it to the current MAX_PATH
typedef InlineSString<260> PathString;
typedef InlineSString<2 * 260> LongPathString;
#endif

// ================================================================================
// Quick macro to create an SString around a literal string.
// usage:
//        s = SL("My literal String");
// ================================================================================

#define SL(_literal) SString(SString::Literal, _literal)

// ================================================================================
// ScratchBuffer classes are used by the GetXXX() routines to allocate scratch space in.
// ================================================================================

class EMPTY_BASES_DECL SString::AbstractScratchBuffer : private SString
{
  protected:
    // Do not use this class directly - use
    // ScratchBuffer or StackScratchBuffer.
    AbstractScratchBuffer(void *buffer, COUNT_T size);
};

template <COUNT_T MEMSIZE>
class EMPTY_BASES_DECL ScratchBuffer : public SString::AbstractScratchBuffer
{
  private:
    DAC_ALIGNAS(::SString::AbstractScratchBuffer)
    BYTE m_inline[MEMSIZE];

  public:
    ScratchBuffer()
    : AbstractScratchBuffer((void *)m_inline, MEMSIZE)
    {
        WRAPPER_NO_CONTRACT;
    }
};

typedef ScratchBuffer<256> StackScratchBuffer;

// ================================================================================
// Special contract definition - THROWS_UNLESS_NORMALIZED
// this is used for operations which might fail for generalized strings but
// not if the string has already been converted to unicode.  Rather than just
// setting this on all conversions to unicode, we only set it when explicitly
// asked.  This should expose more potential problems.
// ================================================================================

#define THROWS_UNLESS_NORMALIZED \
    if (IsNormalized()) NOTHROW; else THROWS

#define THROWS_UNLESS_BOTH_NORMALIZED(s) \
    if (IsNormalized() && s.IsNormalized()) NOTHROW; else THROWS

#define FAULTS_UNLESS_NORMALIZED(stmt) \
    if (IsNormalized()) FORBID_FAULT; else INJECT_FAULT(stmt)

#define FAULTS_UNLESS_BOTH_NORMALIZED(s, stmt) \
    if (IsNormalized() && s.IsNormalized()) FORBID_FAULT; else INJECT_FAULT(stmt)

// ================================================================================
// Inline definitions
// ================================================================================

#include <sstring.inl>

#endif  // _SSTRING_H_
