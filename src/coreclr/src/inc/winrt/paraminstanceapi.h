// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************

*
*  Portions of this header fall under the following
*  copyrights and/or licenses:
* 
*     rfc4122 and supporting functions
*     * Algorithm from RFC 4122 - A Universally Unique IDentifier (UUID) URN Namespace
*     * By Paul J. Leach, Michael Mealling and Rich Sals, July 2005.
*     * 
*     * This function is adapted from the routines in the document
*     * uuid_create_sha1_from_name and format_uuid_v3or5
*     *  
*     *
*     * Copyright (c) 1990- 1993, 1996 Open Software Foundation, Inc.
*     * Copyright (c) 1989 by Hewlett-Packard Company, Palo Alto, Ca. &
*     * Digital Equipment Corporation, Maynard, Mass.
*     * Copyright (c) 1998 Microsoft.
*     * To anyone who acknowledges that this file is provided "AS IS"
*     * without any express or implied warranty: permission to use, copy,
*     * modify, and distribute this file for any purpose is hereby
*     * granted without fee, provided that the above copyright notices and
*     * this notice appears in all source code copies, and that none of
*     * the names of Open Software Foundation, Inc., Hewlett-Packard
*     * Company, Microsoft, or Digital Equipment Corporation be used in
*     * advertising or publicity pertaining to distribution of the software
*     * without specific, written prior permission. Neither Open Software
*     * Foundation, Inc., Hewlett-Packard Company, Microsoft, nor Digital
*     * Equipment Corporation makes any representations about the
*     * suitability of this software for any purpose.
*     *
*/

#ifdef _MSC_VER
#pragma once
#endif  /* _MSC_VER */

#ifndef WINRT_PARAMINSTANCEAPI_H
#define WINRT_PARAMINSTANCEAPI_H

#ifdef __cplusplus

#ifdef _MSC_VER
#pragma warning( push )
#pragma warning( disable : 4180 ) // qualifier applied to function type has no meaning; ignored 
#endif 

#include <wtypes.h>
#include <ntassert.h>
#include <sal.h>
#include <stdlib.h>
#include <string.h>
#include <objbase.h>
#include <limits.h>

//#ifdef _MSC_VER
//#include <new.h>
//#else
//#include <new>
//#endif

#ifndef WINRT_PARAMINSTANCE_NOCRYPT_SHA1
#include <bcrypt.h>
#endif

#ifdef _MSC_VER
#pragma push_macro("CHKHR")
#pragma push_macro("CHKNT")
#endif

namespace Ro { namespace detail {

    // 
    // Debugging aide.  Set breakpoint on _FailedHR 
    //  to see HRESULT propagation.
    // 
    #ifdef DEBUG
    inline HRESULT __declspec(noinline) _FailedHR(HRESULT hr) { static HRESULT _hr = hr; return hr; }
    #else
    inline HRESULT _FailedHR(HRESULT hr) { return hr; }
    #endif
}}

#undef CHKHR
// 
// Call HRESULT returning code and propagate any errors.
// Note: only use in code that is exception-safe / uses RAII.
// 
#define CHKHR(expr) \
    { HRESULT _zzhr;  \
      _zzhr = expr;   \
      if (FAILED(_zzhr)) return Ro::detail::_FailedHR(_zzhr); }

#undef CHKNT
// 
// Call NTSTATUS returning code and propagate any errors, as HRESULTs.
// Note: 
//   - only use in code that is exception-safe / uses RAII / RRID.
//   - HRESULT_FROM_NT does safely convert STATUS_SUCCESS into 
//     a SUCCEEDED hr.
//  
#define CHKNT(expr) \
    CHKHR( HRESULT_FROM_NT( expr ) )

namespace Ro { namespace detail {
    
    // 
    // Runtime check for an invariant.  This check executes in release builds.
    // 
    
    inline HRESULT Verify(bool invariant, HRESULT defaultHr = E_UNEXPECTED)
    {
        if (!invariant)
        {
            CHKHR(defaultHr);
        }
        return S_OK;
    }
}}


extern "C" {


// sha1 adaptor
// create hash instance

HRESULT _RoSha1Create(
    __out void** handle); 


// sha1 adaptor
// append more data to the input stream

HRESULT _RoSha1AppendData(
    __in void* handle,
    __in size_t numBytes,
    __in_bcount(numBytes) const void* data);


// sha1 adaptor
// return the first 16 bytes of SHA1 hash

HRESULT _RoSha1Finish(
    __in void* handle,
    __out BYTE (*hashValue)[20]);


// sha1 adaptor
// free this instance

void _RoSha1Release(__in void* handle);

}

struct IRoSimpleMetaDataBuilder;
struct IRoMetaDataLocator;

// The 'detail' namespace includes implementation details that
//   are subject to change without notice.
namespace Ro { namespace detail
{ 
    struct SimpleMetaDataBuffer;
}}


//
// Purpose: 
//   Given a parameterized type instance name and metadata, 
//   computes the IID for that instance.
// 
// Parameters:
// 
//   nameElementCount
//     number of elements in nameElements
//   nameElements
//     a parsed WinRt type name, as would be returned by RoParseTypeName.
//     Eg: ["W.F.C.IVector`1", "N1.N2.IFoo"]
//   metaDataLocator
//     A callback to use for resolving metadata.
//     
//     An implementation could, for example, forward all calls
//     to RoGetMetaData, then passing the results to 
//     RoWriteImporterToPushSimpleMetaData.  As RoGetMetadata does
//     not cache results, such an implementation would be inefficient.
//     A better implementation will cache the results to RoGetMetaData,
//     as appropriate.
//
//     The Locator helper function can be used to wrap a lambda 
//     expression, or function pointer.  eg:
//         RoGetParameterizedTypeInstanceIID(
//             ..., 
//             Locate([&](PCWSTR* name, IRoSimpleMetaDataBuilder& push){...}),
//             ...);
//   iid
//     out param.  Returns the iid for the parameterized type specified 
//     by nameElements
//   extra 
//     out param. returns a handle that holds extra information about the
//     IID result, for diagnostic purposes. If this handle is not desired,
//     provide nullptr instead.
// 
// Notes:
//   -  This function is stateless.  IRoMetaDataLocator will not be preserved
//      between calls.
//   -  This function does not perform deep semantic analysis.  For instance, 
//      if IRoSimpleMetaDataBuilder specifies that a struct contains an interface pointer,
//      this API will return success, even though such metadata is semantically
//      invalid.  The value of the IID returned is unspecified in such cases.      
//   -  This function does introduce reentrancy.  Its implementation
//      of IRoSimpleMetaDataBuilder may make reentrant calls to IRoMetaDataLocator.
//   -  If a call to IRoSimpleMetaDataBuilder fails, this function will return that 
//      failure code.
//


DECLARE_HANDLE(ROPARAMIIDHANDLE);

inline HRESULT RoGetParameterizedTypeInstanceIID(
    UINT32                                  nameElementCount,  
    __in_ecount(nameElementCount) PCWSTR*   nameElements, 
    __in const IRoMetaDataLocator&          metaDataLocator, 
    __out GUID*                             iid,
    __deref_opt_out ROPARAMIIDHANDLE*       pExtra = nullptr);

// Frees the 'extra' handle allocated 
//   by RoGetParameterizedTypeInstanceIID
inline void RoFreeParameterizedTypeExtra(__in ROPARAMIIDHANDLE extra);

// Fetches the TypeSignature used to compute the IID by the last
//   call to RoGetParameterizedTypeInstanceIID on this extra handle.
//   The string contains ASCII code only, and the string is valid
//   until RoFreeParameterizedTypeExtra is called on the extra pointer.
inline PCSTR RoParameterizedTypeExtraGetTypeSignature(__in ROPARAMIIDHANDLE extra);

namespace Ro { namespace detail 
{
    
    // private type used in helper function 
    
    template <typename Fn>
    struct _Locator;
}} // namespace Ro::detail

namespace Ro 
{
    
    // helper function to create IRoMetaDataLocator from lambda expression
    
    template <typename Fn>
    Ro::detail::_Locator<Fn> Locator(const Fn& fn);
} // namespace Ro


// 
// Purpose:
//   Destination for IRoMetaDataLocator::Locate to write parsed metadata to.
//   'Locate' should set the appropriate Windows Runtime metadata information gleaned
//   from Windows Runtime metadata file, or other appropriate source.
//  
// Notes:
//   -  Methods for base types and COM interfaces (eg, Int32 and IInspectable
//      respectively) are not needed -- RoGetParameterizedTypeInstanceIID already 
//      knows the WinRT base type names, so will not invoke IMetDataLocator 
//      to discover them. 
//   -  This is not a COM interface.  It does not derive from IUnknown.
//

struct IRoSimpleMetaDataBuilder
{
    
    // Notes: 
    //  IInspectable and other non-WinRT interfaces are not permissible.
    //  Not for use with parameterized type instances.  See SetParameterizedInterface
    
    STDMETHOD(SetWinRtInterface)(
        GUID iid) = 0;

    
    // Notes: 
    //  Not for use with parameterized type instances.  See SetParameterizedDelegate
    
    STDMETHOD(SetDelegate)(
        GUID iid) = 0;

    
    // Notes:
    //  Call this method when an interface group has a default interface 
    //  that is a non-parametric type.
    
    STDMETHOD(SetInterfaceGroupSimpleDefault)(
        PCWSTR                  name,
        PCWSTR                  defaultInterfaceName,
        __in_opt const GUID*    defaultInterfaceIID) = 0;

    
    // Notes:
    //  Call this method when an interface group has a parameterized 
    //  interface as its default interface.
    
    STDMETHOD(SetInterfaceGroupParameterizedDefault)(
        PCWSTR                              name,
        UINT32                              elementCount,
        __in_ecount(elementCount) PCWSTR*   defaultInterfaceNameElements) = 0;

    STDMETHOD(SetRuntimeClassSimpleDefault)(
        PCWSTR                  name,
        PCWSTR                  defaultInterfaceName,
        __in_opt const GUID*    defaultInterfaceIID) = 0;

    STDMETHOD(SetRuntimeClassParameterizedDefault)(
        PCWSTR                              name,
        UINT32                              elementCount,
        __in_ecount(elementCount) PCWSTR*   defaultInterfaceNameElements) = 0;

    STDMETHOD(SetStruct)(
        PCWSTR                          name,
        UINT32                          numFields, 
        __in_ecount(numFields) PCWSTR*  fieldTypeNames) = 0;

    STDMETHOD(SetEnum)(
        PCWSTR name,
        PCWSTR baseType) = 0;
    
    
    // Notes: 
    //   This is only for the 'non-instantiated' parameterized interface itself - 
    //   instances are handled by RoGetParameterizedTypeInstanceIID, and the 
    //   caller need not parse them.
    
    STDMETHOD(SetParameterizedInterface)(
        GUID   piid,
        UINT32 numArgs) = 0;

    STDMETHOD(SetParameterizedDelegate)(
        GUID   piid,
        UINT32 numArgs) = 0;
};


//
// Purpose: 
//   Callback for resolving metadata.
// 

struct IRoMetaDataLocator
{
    
    // 
    // Parameters:
    //   nameElement
    //     a metadata typeref name to resolve.  
    //     Eg: "N1.N2.IFoo", or "W.F.C.IVector`1".
    //   pushMetaData
    //     data sink for providing information about the 
    //     type information for nameElement
    // 
    
    STDMETHOD(Locate)(
        PCWSTR                      nameElement,
        __in IRoSimpleMetaDataBuilder&   metaDataDestination
    ) const = 0;
};

namespace Ro { namespace detail {

    
    // 
    // helper function, moves range of elements
    // 
    
    template <typename T>
    void _VecMoveRange(
        __in_ecount(size) T* dst,
        __in_ecount(size) T* src, 
        size_t               size)
    {
        for (size_t i = 0; i != size; ++i) 
        {
            dst[i] = static_cast<T&&>(src[i]);
        }
    }
    
    // 
    // specializations to move strings more efficiently
    // 
    
    inline void _VecMoveRange(
        __in_ecount(size) char* dst,
        __in_ecount(size) char* src, 
        size_t                  size)
    {
        errno_t err = memcpy_s(dst, size*sizeof(*dst), src, size*sizeof(*dst));
        NT_ASSERT(!err);
        (void)err;
    }
    inline void _VecMoveRange(
        __in_ecount(size) wchar_t* dst,
        __in_ecount(size) wchar_t* src, 
        size_t                  size)
    {
        errno_t err = memcpy_s(dst, size*sizeof(*dst), src, size*sizeof(*dst));
        NT_ASSERT(!err);
        (void)err;
    }

        
    // 
    // helper function, moves range of elements
    // 
    
    template <typename T>
    void _VecCopyRange(
        __in_ecount(size) T* dst,
        __in_ecount(size) const T* src, 
        size_t               size)
    {
        for (size_t i = 0; i != _size; ++i) 
        {
            dst[i] = src[i];
        }
    }
    
    // 
    // specializations to move strings more efficiently
    // 
    
    inline void _VecCopyRange(
        __in_ecount(size) char* dst,
        __in_ecount(size) const char* src, 
        size_t                  size)
    {
        errno_t err = memcpy_s(dst, size*sizeof(*dst), const_cast<char*>(src), size*sizeof(*dst));
        NT_ASSERT(!err);
        (void)err;
    }
    inline void _VecCopyRange(
        __in_ecount(size) wchar_t* dst,
        __in_ecount(size) const wchar_t* src, 
        size_t                  size)
    {
        errno_t err = memcpy_s(dst, size*sizeof(*dst), const_cast<wchar_t*>(src), size*sizeof(*dst));
        NT_ASSERT(!err);
        (void)err;
    }
    
    // 
    // Single-owner smart pointer for arrays
    // 
    
    template <class T>
    struct ArrayHolder
    {
        ArrayHolder() : _value(NULL)
        {
        }
        T* Value() const
        { 
            return _value;
        }
        T*& Value()
        { 
            return _value;
        }
        T* Detach()
        {
            T* tmp = _value;
            _value = NULL;
            return tmp;
        }
        ~ArrayHolder()
        {
            delete[] _value;
        }

    private:
        T* _value;
    };
    
    // 
    // Single-owner smart pointer for object pointer
    // 
    
    template <class T>
    struct ElementHolder
    {
        ElementHolder() : _value(NULL)
        {
        }
        T* operator->() const
        {
            return _value;
        }
        T* Value() const
        { 
            return _value;
        }
        T*& Value()
        { 
            return _value;
        }
        T* Detach()
        {
            T* tmp = _value;
            _value = NULL;
            return tmp;
        }
        ~ElementHolder()
        {
            delete _value;
        }

    private:
        T* _value;
    };


    
    // 
    // simple vector, with small vector optimization
    //   T - must be default constructable and movable.
    //       const input overload of AppendN requires copyable.
    //   FixedBufSize - number of bytes to use for small array 
    //       optimization, to avoid heap allocation in case of 
    //       small vectors.  Defaults to at least one element, 
    //       otherwise the largest value such that <= 64 bytes 
    //       are used.
    // 
    
    template <
        typename T, 
        size_t FixedBufSize = 0
        >
    class Vec
    {
    private:
        static const size_t _fixedBufSize = 
            FixedBufSize/sizeof(T) 
                         ? FixedBufSize/sizeof(T)
                         : (((64/sizeof(T)) > 0) ? (64/sizeof(T)) 
                                                 : 1);
    public:
        Vec() : 
            _size(0), 
            _cap(_countof(_fixedBuf)), 
            _buf(_fixedBuf)
        {
        }

        
        // Appends an element, or a default value if one 
        // it not specified. If called with an rvalue, 
        // it uses move assignment instead of copy.
        
        HRESULT Append(T value = T())
        {
            if (_cap - _size < 1)
            {
                CHKHR(_Grow());
            }
            _buf[_size] = static_cast<T&&>(value);
            ++_size;

            return S_OK;
        }
        
        // Moves elements (move assignment) into array.
        
        HRESULT MoveN(__in_ecount(n) T* values, size_t n)
        {
            if (_cap - _size < n)
            {
                CHKHR(_Grow(n - (_cap - _size)));
            }
            _VecMoveRange(_buf + _size, values, n);
            _size += n;

            return S_OK;
        }
        
        
        // Appends elements. Does not invoke move assignment.
        
        HRESULT AppendN(__in_ecount(n) const T* values, size_t n)
        {
            if (_cap - _size < n)
            {
                CHKHR(_Grow(n - (_cap - _size)));
            }
            _VecCopyRange(_buf + _size, values, n);
            _size += n;

            return S_OK;
        }

        HRESULT Pop()
        {
            CHKHR(Verify( _size > 0 ));
            --_size;
            return S_OK;
        }

        HRESULT Resize(size_t newSize)
        {
            if (_cap < newSize)
            {
                CHKHR(_Grow(newSize - _cap));
            }
            _size = newSize;
            return S_OK;
        }

        size_t Size() const
        {
            return _size;
        }

        T& operator[](size_t index)
        {
            NT_ASSERT(index < _size);
            return _buf[index];
        }

        T& Last()
        {
            return (*this)[_size-1];
        }

        ~Vec()
        {
            if (_buf != _fixedBuf) 
            {
                delete[] _buf;
            }
        }

    private:
        
        // 
        // growth factor (does not check for overflow) -- returns amount to grow by
        // 
        
        static size_t _GrowthIncrement(size_t n)
        {
            return n / 2;
        }

        HRESULT _Grow(size_t byAtLeast = 4)
        {
            size_t increase = _GrowthIncrement(_cap);
            if (increase < byAtLeast)
            {
                increase = byAtLeast;
            }
            size_t newCap = _cap + increase;
            if (newCap <= _cap) 
            {
                CHKHR(E_OUTOFMEMORY);
            }
            ArrayHolder<T> newBuf;

            void* p = (newBuf.Value() = new (std::nothrow) T[newCap]);
            if (!p) 
            {
                CHKHR(E_OUTOFMEMORY);
            }

            _VecMoveRange( newBuf.Value(), _buf, _size );

            if (_buf != _fixedBuf) 
            {
                delete _buf;
            }
            _buf = newBuf.Detach();
            _cap = newCap;

            return S_OK;
        }

        size_t  _size;
        size_t  _cap;
        T*      _buf;
        T       _fixedBuf[_fixedBufSize];
    };

    struct SimpleMetaDataBuilder : IRoSimpleMetaDataBuilder
    {
    public:
        SimpleMetaDataBuilder(SimpleMetaDataBuffer& buffer, const IRoMetaDataLocator& locator) 
        : _buffer(&buffer), _locator(&locator), _invoked(false)
        {
        }
        IFACEMETHOD(SetWinRtInterface)(GUID iid);
        IFACEMETHOD(SetDelegate)(GUID iid);
        IFACEMETHOD(SetInterfaceGroupSimpleDefault)(PCWSTR name, PCWSTR defaultInterfaceName, __in_opt const GUID *defaultInterfaceIID);
        IFACEMETHOD(SetInterfaceGroupParameterizedDefault)(PCWSTR name, UINT32 elementCount, __in_ecount(elementCount) PCWSTR *defaultInterfaceNameElements);
        IFACEMETHOD(SetRuntimeClassSimpleDefault)(PCWSTR name, PCWSTR defaultInterfaceName, __in_opt const GUID *defaultInterfaceIID);
        IFACEMETHOD(SetRuntimeClassParameterizedDefault)(PCWSTR name, UINT32 elementCount, __in_ecount(elementCount) PCWSTR *defaultInterfaceNameElements);
        IFACEMETHOD(SetStruct)(PCWSTR name, UINT32 numFields, __in_ecount(numFields) PCWSTR *fieldTypeNames);
        IFACEMETHOD(SetEnum)(PCWSTR name, PCWSTR baseType);
        IFACEMETHOD(SetParameterizedInterface)(GUID piid, UINT32 numArgs);
        IFACEMETHOD(SetParameterizedDelegate)(GUID piid, UINT32 numArgs);

        
        // Runs the locating process for a parameterized type. 
        // Notes:
        //   _buffer->_nestingLevel is used to determine the number of 
        //   arguments left to consume for nested parameterized types. 
        
        HRESULT SendArguments(UINT32 nameElementCount, __in_ecount(nameElementCount) PCWSTR *nameElements);

    private:

        
        // Writes the type signature for the type 'name'
        //   Notes:
        //     - If a builtin type, writes the type directly.
        //     - Otherwise, uses the IRoMetaDataLocator to 
        //       write the type signature into _buffer
        //     - As the sole function to call 
        //       IRoMetaDataLocator, it also performs the check
        //       on recursion depth bounds.
        
        HRESULT _WriteType(PCWSTR name);

        
        // The tail portion of IG and RC formats is the same.  This 
        //   function implements the shared portion of that format.
        
        HRESULT _CommonInterfaceGroupSimple(PCWSTR name, PCWSTR defaultInterfaceName, __in_opt const GUID *defaultInterfaceIID);

         
        // Called at the beginning of every 'Set' method.  Set must only be called once. 
        
        HRESULT _OnSet();

        
        // Called at the end of every 'Set' method, only if successful. 
        
        void _Completed();

        static char _AsciiLower(char ch)
        {
            if ('A' <= ch && ch <= 'Z')
            {
                return ch + ('a' - 'A');
            }
            else
            {
                return ch;
            }
        }

        
        // Writes a guid into the type signature being built, in lower case.
        
        HRESULT _WriteGuid(const GUID& iid);
        HRESULT _WriteString(PCSTR str);
        HRESULT _WriteChar(char c);
        HRESULT _WriteWideString(PCWSTR str);

        SimpleMetaDataBuilder();
        SimpleMetaDataBuilder(const SimpleMetaDataBuilder&);
        void operator=(const SimpleMetaDataBuilder&);

        SimpleMetaDataBuffer*       _buffer;
        const IRoMetaDataLocator*   _locator;
        bool                        _invoked;
    };

    
    // If the type string describes a built-in type, modifies 
    // this instance to use builtin type table entry instead of name.
    
    inline bool _IsBuiltin(__in PCWSTR name, __out PCSTR * typeSignature)
    {
        *typeSignature = nullptr;

        struct BuiltinEntry { PCWSTR name; PCSTR typeSignature; };
        static const BuiltinEntry entries[] = {
            
            { L"UInt8",     "u1" },
            { L"Int16",     "i2" },
            { L"UInt16",    "u2" },
            { L"Int32",     "i4" },
            { L"UInt32",    "u4" },
            { L"Int64",     "i8" },
            { L"UInt64",    "u8" },
            { L"Single",    "f4" },
            { L"Double",    "f8" },
            { L"Boolean",   "b1" },
            { L"Char16",    "c2" },
            { L"String",    "string" },
            { L"Guid",      "g16" },
            { L"Object",    "cinterface(IInspectable)" },
        };
        for (const BuiltinEntry* tip = entries;
             tip != &entries[_countof(entries)];
             ++tip)
        {
            if (wcscmp(tip->name, name) == 0)
            {
                *typeSignature = tip->typeSignature;
                return true;
            }
        }
        
        // if not found, assume is a normal type name
        
        return false;
    }

    
    // Linked list (stack allocated) of type resolution calls,
    // used to detect if an InterfaceGroup/RuntimeClass type
    // signature depends on itself. In that case, we use "*"
    // in the type signature instead of recurring further.
    
    struct ResolutionPathEntry
    {
        ResolutionPathEntry*    _next;
        PCWSTR                  _typeName;

        ResolutionPathEntry(PCWSTR typeName) 
        : _next(nullptr)
        , _typeName(typeName)
        {
        }
    };

    inline void Push(ResolutionPathEntry*& top, ResolutionPathEntry* item)
    {
        item->_next = top;
        top = item;
    }
    inline HRESULT Pop(ResolutionPathEntry*& top)
    {
        if (!top)
        {
            return E_UNEXPECTED;
        }
        top = top->_next;
        return S_OK;
    }

    
    // Holds metadata state that is shared between RoGetParamInstanceIID and SimpleMetaDataBuilder
    
    struct SimpleMetaDataBuffer 
    {   
        SimpleMetaDataBuffer()
        {
            Clear();
        }

        // reset all tables
        void Clear()
        {
            _recursionDepth = 0;
            _topLevelTypes = 0;
            _resolutionPath = nullptr;
            _outputStream.Resize(0);
        }

        static const size_t             _maxTypeName = 256;

        
        // Estimate of 'reasonable' level of Interface Group / Runtime 
        // Class / Parameterized Type nesting.
        
        static const size_t             _maxRecursionDepth = 64;

        Vec<char, _maxTypeName>         _outputStream;
        ResolutionPathEntry*            _resolutionPath;

        
        // RAII object, places an item on the resolution path, and pops it on destruction
        
        class ResolutionPathGuard
        {
        private:
            ResolutionPathEntry     _entry;
            SimpleMetaDataBuffer*   _buffer;

        public:
            ResolutionPathGuard(PCWSTR typeName, SimpleMetaDataBuffer* buffer)
            : _buffer(buffer)
            , _entry(typeName)
            {
                Push(buffer->_resolutionPath, &_entry);
            }
            ~ResolutionPathGuard()
            {
                HRESULT hr = Pop(_buffer->_resolutionPath);
                NT_ASSERT(SUCCEEDED(hr));
                (void)hr;
            }
        };

        
        // Searches the resolution path for 'name' returning true if exists
        
        bool ExistsCycle(PCWSTR typeName)
        {
            for (auto pTip = _resolutionPath; pTip; pTip = pTip->_next)
            {
                if (wcscmp(typeName, pTip->_typeName) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        
        // Indicates the nesting level of compound types, used
        //   to properly balance parenthesis on parameterized types,
        //   and used to bound recursion depth.
        // 
        // - Pinterfaces 
        //     : push 'numArgs' on to _nestingLevel
        // - A compound type that doesn't know number of arguments
        //     eg, RoGetParameterizedInstanceIID arguments, or
        //         SetIG..Parameterized
        //     : will 0) note nesting level
        //            1) iterate calling Locate on the compound arguments.
        //            2) the above should cause exactly one push of _nestingLevel
        //            3) reduce nesting level back to original nesting level, 
        //               inserting the difference in closing parens
        //  - Compound types that do know number of arguments (eg SetStruct)
        //     : will 1) increase nesting level by 1
        //            2) iterate calling Locate on arguments
        //            3) decrease nesting level again
        // 
        // 
        
        Vec<size_t>                     _nestedArgs;
        
        // topLevelTypes should be incremented once, by the initial
        // parameterized type, then never again.
        
        size_t                          _topLevelTypes;
        size_t                          _recursionDepth;
    };
}} // namespace Ro::detail

namespace Ro { namespace detail 
{
    template <typename Fn>
    struct _Locator : IRoMetaDataLocator
    {
        Fn _fn;

        _Locator(const Fn& fn) 
        : _fn(fn)
        {
        }
        
        IFACEMETHOD(Locate)(
            PCWSTR name,
            IRoSimpleMetaDataBuilder& pushMetaData) const
        {
            return _fn(name, pushMetaData);
        }
    };
}} // namespace Ro::detail

namespace Ro
{
    template <typename Fn>
    Ro::detail::_Locator<Fn> Locator(const Fn& fn)
    {
        return Ro::detail::_Locator<Fn>(fn);
    }
}

namespace Ro { namespace detail
{
    
    // Figure out if we're compiling for a big- or little-endian machine.
    
    inline bool BigEndian()
    {
        unsigned long n = 0xff000000L;

        return 0 != *reinterpret_cast<unsigned char *>(&n);
    }

    
    // HostToNetworkLong converts a 32-bit long to network byte order
    
    inline ULONG HostToNetworkLong(ULONG hostlong)
    {
        if (BigEndian())
            return hostlong;
        else
            return	( (hostlong >> 24) & 0x000000FFL) |
                    ( (hostlong >>  8) & 0x0000FF00L) |
                    ( (hostlong <<  8) & 0x00FF0000L) |
                    ( (hostlong << 24) & 0xFF000000L);
    }

    
    // HostToNetworkLong converts a 16-bit short to network byte order
    
    inline USHORT HostToNetworkShort(USHORT hostshort)
    {
        if (BigEndian())
            return hostshort;
        else
            return ((hostshort >> 8) & 0x00FF) | ((hostshort << 8) & 0xFF00);
    }

    
    // NetworkToHostLong converts a 32-bit long to local host byte order
    
    inline ULONG NetworkToHostLong(ULONG netlong)
    {
        if (BigEndian())
            return netlong;
        else
            return	( (netlong >> 24) & 0x000000FFL) |
                    ( (netlong >>  8) & 0x0000FF00L) |
                    ( (netlong <<  8) & 0x00FF0000L) |
                    ( (netlong << 24) & 0xFF000000L);
    }

    
    // NetworkToHostShort converts a 16-bit short to local host byte order
    
    inline USHORT NetworkToHostShort(USHORT netshort)
    {
        if (BigEndian())
            return netshort;
        else
            return ((netshort >> 8) & 0x00FF) | ((netshort << 8) & 0xFF00);
    }

    
    // smart pointer for Sha1 handle
    
    struct Sha1Holder
    {
        Sha1Holder() : _handle(nullptr)
        {
        }
        void*& Value()
        {
            return _handle;
        }
        ~Sha1Holder()
        {
            if (_handle)
            {
                _RoSha1Release(_handle);
            }
        }
    private:
        void* _handle;
    };

    
    
    // 
    // Computes the rfc4122 v5 UUID from GUID,name pair.
    // 
    // Notes:
    //   - see copyright at beginning of file.
    // 
    
    inline HRESULT
    GuidFromName(   
        __in const GUID& guidNamespace,
        __in_bcount(dwcbSize) const void* pbName,
        __in DWORD  dwcbSize,
        __out GUID* pGuid)
    {
        Sha1Holder sha1;

        CHKHR( _RoSha1Create(&sha1.Value()) );
        {
            GUID networkOrderGuidNamespace = guidNamespace;
            
            // Put name space ID in network byte order so it hashes the same
            // no matter what endian machine we're on
            
            if (!BigEndian())
            {
                networkOrderGuidNamespace.Data1 = HostToNetworkLong (networkOrderGuidNamespace.Data1);
                networkOrderGuidNamespace.Data2 = HostToNetworkShort(networkOrderGuidNamespace.Data2);
                networkOrderGuidNamespace.Data3 = HostToNetworkShort(networkOrderGuidNamespace.Data3);
            }
            CHKHR( _RoSha1AppendData(sha1.Value(), sizeof(networkOrderGuidNamespace), reinterpret_cast<BYTE*>(&networkOrderGuidNamespace)) );
        }
        CHKHR( _RoSha1AppendData(sha1.Value(), dwcbSize, pbName) );

        {
            BYTE sha1Result[20];
            CHKHR( _RoSha1Finish(sha1.Value(), &sha1Result) );
            
            errno_t err = memcpy_s(pGuid, sizeof(GUID), &sha1Result[0], sizeof(GUID));
            CHKHR(Verify( 0 == err ));

            
            // Restore the byte order
            
            if (!BigEndian())
            {
                pGuid->Data1 = NetworkToHostLong (pGuid->Data1);
                pGuid->Data2 = NetworkToHostShort(pGuid->Data2);
                pGuid->Data3 = NetworkToHostShort(pGuid->Data3);
            }

            
            // set version number 
            // 1: clear version number nibble
            // 2: set version 5 = name-based SHA1
            
            pGuid->Data3 &= 0x0FFF;
            pGuid->Data3 |= (5 << 12); 
    
            
            // set variant field by clearing variant bits.
            
            pGuid->Data4[0] &= 0x3F;    
            pGuid->Data4[0] |= 0x80;    
        }
        return S_OK;
    }
}} // namespace Ro::detail

inline HRESULT RoGetParameterizedTypeInstanceIID(
    UINT32                                  nameElementCount,  
    __in_ecount(nameElementCount) PCWSTR*   nameElements, 
    __in const IRoMetaDataLocator&          metaDataLocator, 
    __out GUID*                             iid,
    __deref_opt_out ROPARAMIIDHANDLE*       pExtra)
{
    using namespace Ro::detail;
    memset(iid, 0, sizeof(*iid));

    SimpleMetaDataBuffer reserveBuffer;
    SimpleMetaDataBuffer *pBuffer = &reserveBuffer;

    // if user wishes to hold on to the result value, 
    //   dynamically allocate this buffer.
    if (pExtra)
    {
        pBuffer = new (std::nothrow) SimpleMetaDataBuffer;
        *pExtra = static_cast<ROPARAMIIDHANDLE>(static_cast<void*>(pBuffer));
    }
    SimpleMetaDataBuffer& buffer = *pBuffer;
    SimpleMetaDataBuilder builder(*pBuffer, metaDataLocator);
    
    // send initial arguments
    CHKHR(builder.SendArguments(nameElementCount, nameElements));

    // verify that precisely one type was resolved, to completion.
    CHKHR(Verify(buffer._topLevelTypes == 1 
                   && buffer._nestedArgs.Size() == 0,
                 E_INVALIDARG));

    // compute type signature hash
    static const GUID guidPinterfaceNamespace 
        = { 0x11f47ad5, 0x7b73, 0x42c0, { 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee }};

    CHKHR(Ro::detail::Verify( buffer._outputStream.Size() <= DWORD(-1) ));

    // null terminate
    CHKHR( buffer._outputStream.Append('\0') );

    
    // 
    // Unit test logging, to verify proper signatures
    // 
    #ifdef UNITTEST_TRACE
    {
        CHKHR( UNITTEST_TRACE("type signature", &buffer._outputStream[0]) );
    }
    #endif
    

    CHKHR( GuidFromName(guidPinterfaceNamespace, 
                        &buffer._outputStream[0], 
                        DWORD(buffer._outputStream.Size() - 1), // does not include terminator
                        iid) );
    return S_OK;
}

inline void RoFreeParameterizedTypeExtra(__in ROPARAMIIDHANDLE extra)
{
    using namespace Ro::detail;
    delete static_cast<SimpleMetaDataBuffer*>(static_cast<void*>(extra));
}
inline PCSTR RoParameterizedTypeExtraGetTypeSignature(__in ROPARAMIIDHANDLE extra)
{
    using namespace Ro::detail;
    SimpleMetaDataBuffer* pBuffer = static_cast<SimpleMetaDataBuffer*>(static_cast<void*>(extra));

    return &pBuffer->_outputStream[0];
}
    
namespace Ro { namespace detail 
{

    inline HRESULT SimpleMetaDataBuilder::_WriteType(PCWSTR name)
    {
        PCSTR builtInName = nullptr;
        SimpleMetaDataBuilder builder(*_buffer, *_locator);

        if (_IsBuiltin(name, &builtInName))
        {
            CHKHR(builder._OnSet());
            CHKHR(builder._WriteString(builtInName));
            builder._Completed();
        }
        else 
        {
            size_t newDepth = ++_buffer->_recursionDepth;
            size_t pinterfaceNesting = _buffer->_nestedArgs.Size();
            if (newDepth + pinterfaceNesting > _buffer->_maxRecursionDepth)
            {
                
                // Terminate recursion; bounds call stack consumption
                
                CHKHR(E_UNEXPECTED);
            }
            CHKHR(_locator->Locate(name, builder));
            
            // Note, buffers aren't reusable, so it's fine that we don't
            // unwind this value on return.  Also note, we do not unwind
            // this value if the user provides inconsistent data either 
            // (eg, if they provide only 1 argument to a 2 parameter
            // parameterized type).
            
            --_buffer->_recursionDepth;
        }
        return S_OK;
    }

    inline HRESULT SimpleMetaDataBuilder::_OnSet()
    {
        if (_invoked)
        {
            CHKHR(E_INVALIDARG);
        }
        _invoked = true;

        
        // Reduce the number of arguments left for this compound type.
        
        if(_buffer->_nestedArgs.Size() > 0)
        {
            --(_buffer->_nestedArgs.Last());
        }
        else
        {
            
            // Increase number of top level types in signature
            // string.  (should never exceed one)
            
            ++_buffer->_topLevelTypes;
        }
    
        return S_OK;
    }

    inline void SimpleMetaDataBuilder::_Completed()
    {
    }
    inline HRESULT SimpleMetaDataBuilder::SendArguments(UINT32 nameElementCount, __in_ecount(nameElementCount) PCWSTR *nameElements)
    {
        CHKHR(Verify(nameElementCount > 0));

        CHKHR(Verify(_buffer->_nestedArgs.Size() <= UINT32(-1)));
        UINT32 previousLevel = UINT32(_buffer->_nestedArgs.Size());

        for (UINT32 i = 0; i < nameElementCount; ++i)
        {
            CHKHR(_WriteType(nameElements[i]));

            
            // Close any nested parameterized types that are complete
            
            while (_buffer->_nestedArgs.Size() > previousLevel
                   && _buffer->_nestedArgs.Last() == 0)
            {
                CHKHR(_buffer->_nestedArgs.Pop());
                CHKHR(_WriteChar(')'));
            }
            
            // insert separator between parameterized type arguments
            
            CHKHR(_WriteChar(';'));
        }
        
        // remove final separator
        
        CHKHR(_buffer->_outputStream.Pop());

        
        // Verify that all the arguments were consumed.
        
        CHKHR(Verify(_buffer->_nestedArgs.Size() == previousLevel,
                     E_INVALIDARG));
        return S_OK;
    }

    inline HRESULT SimpleMetaDataBuilder::_WriteGuid(const GUID& iid)
    {
        static const size_t guidStringLength = _countof("{11223344-1122-1122-1122-334455667788}") - 1;
        WCHAR tmpString[guidStringLength+1];

        int numWritten = StringFromGUID2(iid, tmpString, guidStringLength + 1);
        CHKHR(Verify( numWritten == guidStringLength + 1 ));
        NT_ASSERT( numWritten == guidStringLength + 1 );

        size_t offset = _buffer->_outputStream.Size();
        CHKHR(Verify( offset + guidStringLength > offset )) 
        CHKHR( _buffer->_outputStream.Resize(offset + guidStringLength) );
        char* writePtr = &_buffer->_outputStream[offset];

        
        // All characters are ascii.  Just truncate.
        
        for(size_t i = 0; i < guidStringLength; ++i)
        {
            writePtr[i] = _AsciiLower(char(tmpString[i]));
        }
        return S_OK;
    }
    inline HRESULT SimpleMetaDataBuilder::_WriteString(PCSTR str)
    {
        CHKHR( _buffer->_outputStream.AppendN(str, strlen(str)) );
        return S_OK;
    }
    inline HRESULT SimpleMetaDataBuilder::_WriteChar(char c)
    {
        CHKHR( _buffer->_outputStream.Append(c) );
        return S_OK;
    }
    inline HRESULT SimpleMetaDataBuilder::_WriteWideString(PCWSTR str)
    {
        size_t len = wcslen(str);
        size_t offset = _buffer->_outputStream.Size();
        int written;

        
        // provision enough space for conversion to take place
        
        size_t provision = len + 1;
        for(;;)
        {
            CHKHR( _buffer->_outputStream.Resize(offset+provision));
            char* writePtr = &_buffer->_outputStream[offset];

            CHKHR(Verify(len <= INT_MAX));
            CHKHR(Verify(provision <= INT_MAX));

            written = WideCharToMultiByte(
                  CP_UTF8,
                  0,
                  str,
                  int(len),
                  writePtr,
                  int(provision),
                  nullptr,
                  nullptr
                );

            if (written > 0)
            {
                break;
            }
            else if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
            {
                CHKHR(HRESULT_FROM_WIN32(GetLastError())); 
            }
            else
            {
                provision *= 2;
                CHKHR(Verify( offset + provision > offset ));
            }
        }
        
        // reduce size to reflect number of characters actually written. 
        // Note that since we specified string length, no null terminator 
        // was injected, so we don't have to remove it.
        
        CHKHR( _buffer->_outputStream.Resize(offset+written) );
        
        return S_OK;
    }

    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetWinRtInterface(
        GUID iid)
    {
        CHKHR(_OnSet());

        CHKHR(_WriteGuid(iid));

        _Completed();
        return S_OK;
    }

    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetDelegate(
        GUID iid)
    {
        CHKHR(_OnSet());

        CHKHR(_WriteString("delegate("));
        CHKHR(_WriteGuid(iid));
        CHKHR(_WriteChar(')'));
            
        _Completed();
        return S_OK;
    }
        
    inline HRESULT SimpleMetaDataBuilder::_CommonInterfaceGroupSimple(
        PCWSTR                  name, 
        PCWSTR                  defaultInterfaceName, 
        __in_opt const GUID *   defaultInterfaceIID)
    {
        CHKHR(_WriteWideString(name));
        CHKHR(_WriteChar(';'));

        
        // InterfaceGroups and RuntimeClasses take one nested argument
        
        CHKHR(_buffer->_nestedArgs.Append(1));
        if (!defaultInterfaceIID)
        {
            CHKHR(_WriteType(defaultInterfaceName));
        }
        else
        {
            
            // complete the type signature immediately; no nested 
            //   call needed to resolve the interface.
            
            SimpleMetaDataBuilder builder(*_buffer, *_locator);
            CHKHR(builder.SetWinRtInterface(*defaultInterfaceIID))
        }
        CHKHR(_WriteChar(')'));
        CHKHR(_buffer->_nestedArgs.Pop());
        return S_OK;
    }
    
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetInterfaceGroupSimpleDefault(
        PCWSTR                  name,
        PCWSTR                  defaultInterfaceName,
        __in_opt const GUID*    defaultInterfaceIID)
    {
        CHKHR(_OnSet());

        CHKHR(_WriteString("ig("));
        CHKHR(_CommonInterfaceGroupSimple(name, defaultInterfaceName, defaultInterfaceIID));
        
        _Completed();
        return S_OK;
    }

    
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetInterfaceGroupParameterizedDefault(
        PCWSTR                              name,
        UINT32                              elementCount,
        __in_ecount(elementCount) PCWSTR*   defaultInterfaceNameElements)
    {
        CHKHR(_OnSet());

        
        // If an interface group or runtime class has a compound type as its default, and that 
        // type directly or indirectly refers to itself, the second occurrence instead used '*'
        // to signal that the default interface has already been specified earlier up the call
        // stack.  This prevents unbounded recursion.
        
        if (_buffer->ExistsCycle(name))
        {
            CHKHR( _WriteString("ig(") );
            CHKHR( _WriteWideString(name) );
            CHKHR( _WriteString(";*)") );
        }
        else
        {
            SimpleMetaDataBuffer::ResolutionPathGuard guard(name, _buffer);

            CHKHR( _WriteString("ig(") );
            CHKHR( _WriteWideString(name) );
            CHKHR( _WriteChar(';') );

            
            // InterfaceGroups and RuntimeClasses take one nested argument
            
            CHKHR( _buffer->_nestedArgs.Append(1) );
            CHKHR( SendArguments(elementCount, defaultInterfaceNameElements) );
            CHKHR( _buffer->_nestedArgs.Pop() );
            CHKHR( _WriteChar(')') );;

        }
        _Completed();
        return S_OK;
    }
    
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetRuntimeClassSimpleDefault(
        PCWSTR                  name,
        PCWSTR                  defaultInterfaceName,
        __in_opt const GUID*    defaultInterfaceIID)
    {
        CHKHR(_OnSet());

        CHKHR(_WriteString("rc("));
        CHKHR(_CommonInterfaceGroupSimple(name, defaultInterfaceName, defaultInterfaceIID));

        _Completed();
        return S_OK;
    }

    
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetRuntimeClassParameterizedDefault(
        PCWSTR                              name,
        UINT32                              elementCount,
        __in_ecount(elementCount) PCWSTR*   defaultInterfaceNameElements)
    {
        CHKHR(_OnSet());

        if (_buffer->ExistsCycle(name))
        {
            CHKHR(_WriteString("rc("));
            CHKHR(_WriteWideString(name));
            CHKHR(_WriteString(";*)"));
        }
        else
        {
            SimpleMetaDataBuffer::ResolutionPathGuard guard(name, _buffer);

            CHKHR(_WriteString("rc("));
            CHKHR(_WriteWideString(name));
            CHKHR(_WriteChar(';'));

            
            // InterfaceGroups and RuntimeClasses take one nested argument
            
            CHKHR(_buffer->_nestedArgs.Append(1));
            CHKHR(SendArguments(elementCount, defaultInterfaceNameElements));
            CHKHR(_buffer->_nestedArgs.Pop());

            CHKHR(_WriteChar(')'));
        }        
        _Completed();
        return S_OK;
    }
        
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetStruct(
        PCWSTR                          name,
        UINT32                          numFields, 
        __in_ecount(numFields) PCWSTR*  fieldTypeNames)
    {
        CHKHR(_OnSet());
        CHKHR(_WriteString("struct("));
        CHKHR(_WriteWideString(name));
        CHKHR(_WriteChar(';'));

        CHKHR(_buffer->_nestedArgs.Append(1));
        CHKHR(SendArguments(numFields, fieldTypeNames));
        CHKHR(_buffer->_nestedArgs.Pop());

        CHKHR(_WriteChar(')'));

        _Completed();
        return S_OK;
    }
    
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetEnum(
        PCWSTR name,
        PCWSTR baseType)
    {
        CHKHR(_OnSet());

        CHKHR(_WriteString("enum("));
        CHKHR(_WriteWideString(name));
        CHKHR(_WriteChar(';'));
        CHKHR(_buffer->_nestedArgs.Append(1));
        CHKHR(_WriteType(baseType));
        CHKHR(_buffer->_nestedArgs.Pop());
        CHKHR(_WriteChar(')'));
        
        _Completed();
        return S_OK;
    }
    
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetParameterizedInterface(
        GUID   piid,
        UINT32 numArgs)
    {
        CHKHR(_OnSet());

        CHKHR(_WriteString("pinterface("));
        CHKHR(_WriteGuid(piid));

        
        // Note the number of arguments. The SendArguments
        //   function will append the ')' after that number of 
        //   arguments are consumed.
        
        CHKHR(_buffer->_nestedArgs.Append(numArgs));

        _Completed();
        return S_OK;
    }
    
    inline __override HRESULT STDMETHODCALLTYPE SimpleMetaDataBuilder::SetParameterizedDelegate(
        GUID   piid,
        UINT32 numArgs)
    {
        
        // Parameterized interfaces and parameterized delegates use the same signature scheme.
        
        return SetParameterizedInterface(piid, numArgs);
    }

}} // namespace Ro::detail

#ifndef WINRT_PARAMINSTANCE_NOCRYPT_SHA1

namespace Ro { namespace detail {

    class Sha1
    {
    public:
        Sha1()
        : _hAlg(nullptr)
        , _hHash(nullptr)
        {
        }

        HRESULT Initialize()
        {
            DWORD dwcb;
            DWORD dwcbResult;

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable: 33098) // "Banned hash algorithm is used" - SHA-1 is required for compatibility
#endif // _PREFAST_
            CHKNT(BCryptOpenAlgorithmProvider(&_hAlg, BCRYPT_SHA1_ALGORITHM, MS_PRIMITIVE_PROVIDER, 0));
#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_

            CHKNT(BCryptGetProperty(_hAlg, BCRYPT_OBJECT_LENGTH, reinterpret_cast<PBYTE>(&dwcb), sizeof(dwcb), &dwcbResult, 0));

            _ahBuf.Value() = new (std::nothrow) BYTE[dwcb];
            if (nullptr == _ahBuf.Value())
            {
                CHKHR( E_OUTOFMEMORY );
            }

            CHKNT(BCryptCreateHash(_hAlg, &_hHash, _ahBuf.Value(), dwcb, NULL, 0, 0));
            return S_OK;
        }
        HRESULT AppendData(size_t numBytes, __in_bcount(numBytes) const void* bytes)
        {
            CHKHR(Verify(numBytes <= DWORD(-1)));
            CHKNT(BCryptHashData(_hHash, reinterpret_cast<UCHAR*>(const_cast<void*>(bytes)), DWORD(numBytes), 0));
            return S_OK;;
        }
        HRESULT GetResult(__out BYTE (*hashValue)[20])
        {
            
            // Sha1 hash result is fixed size, at 20 bytes.
            
            CHKNT(BCryptFinishHash(_hHash, reinterpret_cast<PUCHAR>(&hashValue[0]), _countof(*hashValue), 0));
            return S_OK;
        }
        ~Sha1()
        {
            if (_hHash)
            {
                BCryptDestroyHash(_hHash);
            }
            if (_hAlg)
            {
                BCryptCloseAlgorithmProvider(_hAlg, 0);
            }
        }
    private:

        ArrayHolder<BYTE>   _ahBuf;
        BCRYPT_ALG_HANDLE   _hAlg;
        BCRYPT_HASH_HANDLE  _hHash;
    };
}} // namespace Ro::detail

extern "C" 
{

inline HRESULT _RoSha1Create(
    __out void** handle)
{
    *handle = nullptr;

    Ro::detail::ElementHolder<Ro::detail::Sha1> sha1Instance;
    sha1Instance.Value() = new (std::nothrow) Ro::detail::Sha1;
    if (!sha1Instance.Value())
    {
        CHKHR(E_OUTOFMEMORY);
    }
    CHKHR(sha1Instance->Initialize());

    *handle = sha1Instance.Detach();
    return S_OK;
}


inline HRESULT _RoSha1AppendData(
    __in void* handle,
    __in size_t numBytes,
    __in_bcount(numBytes) const void* data)
{
    Ro::detail::Sha1* sha1Instance = static_cast<Ro::detail::Sha1*>(handle);
    CHKHR(sha1Instance->AppendData(numBytes, data));
    return S_OK;
}


inline HRESULT _RoSha1Finish(
    __in void* handle,
    __out BYTE (*hashValue)[20])
{
    Ro::detail::Sha1* sha1Instance = static_cast<Ro::detail::Sha1*>(handle);
    CHKHR(sha1Instance->GetResult(hashValue));
    return S_OK;
}

inline void _RoSha1Release(__in void* handle)
{
    Ro::detail::Sha1* sha1Instance = static_cast<Ro::detail::Sha1*>(handle);
    delete sha1Instance;
}

}

#endif /* ifdef WINRT_PARAMINSTANCE_NOCRYPT_SHA1 */

#ifdef _MSC_VER
#pragma pop_macro("CHKNT")
#pragma pop_macro("CHKHR")
#pragma warning( pop )
#endif

#endif /* ifdef __cplusplus */
#endif /* ifndef WINRT_PARAMINSTANCEAPI_H */
