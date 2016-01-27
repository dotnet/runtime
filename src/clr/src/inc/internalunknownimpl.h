// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
//
// InternalUnknownImpl.h
//
// Defines utility class ComUtil::IUnknownCommon, which provides default
// implementations for IUnknown's AddRef, Release, and QueryInterface methods.
//
// Use: a class that implements one or more interfaces should derive from
// ComUtil::IUnknownCommon with a template parameter list consisting of the
// list of implemented interfaces.
//
// Example:
//   class MyInterfacesImpl :
//     public IUnknownCommon<MyInterface1, MyInterface2>
//   { ... };
//
// IUnknownCommon will provide base AddRef and Release semantics, and will
// also provide an implementation of QueryInterface that will evaluate the
// arguments against the set of supported interfaces and return the
// appropriate result.
//
// If you need to specify multiple interfaces where one is a base interface
// of another and implementing all of them would result in a compiler error,
// you can use the NoDerive wrapper to tell IUnknownCommon to not derive from
// this interface but just use it for QueryInterface calls.
//
// Example:
//   interface A
//   { ... };
//   interface B : public A
//   { ... };
//   class MyInterfacesImpl : public IUnknownCommon<B, NoDerive<A> >
//   { ... };
//
// If a base type also implements IUnknownCommon, then you must override
// QueryInterface with a method that delegates to your type's
// IUnknownCommon::QueryInterface and then to BaseType::QueryInterface.
//


//
//*****************************************************************************

#ifndef __InternalUnknownImpl_h__
#define __InternalUnknownImpl_h__

#include <winnt.h>
#include "winwrap.h"
#include "contract.h"
#include "ex.h"
#include "volatile.h"
#include "mpl/type_list"
#include "debugmacros.h"

#define COMUTIL_IIDOF(x) __uuidof(x)

namespace ComUtil
{
    //---------------------------------------------------------------------------------------------
    template <typename T>
    struct TypeWrapper
    { typedef T wrapped_type; };

    namespace detail
    {
        typedef char (&_Yes)[1];
        typedef char (&_No)[2];

        static inline _No _IsTypeWrapper(...);

        template <typename T>
        static _Yes _IsTypeWrapper(T *, typename T::wrapped_type * = nullptr);
    }

    //---------------------------------------------------------------------------------------------
    template <typename T>
    struct IsTypeWrapper
    {
        static const bool value = std::integral_constant<
            bool, 
            sizeof(detail::_IsTypeWrapper((T*)0)) == sizeof(detail::_Yes)>::value;
    };

    //-----------------------------------------------------------------------------------------
    // Utility to remove marker type wrappers.
    template <typename T, bool IsWrapper = IsTypeWrapper<T>::value>
    struct UnwrapOne
    { typedef T type; };

    template <typename T>
    struct UnwrapOne<T, true>
    { typedef typename T::wrapped_type type; };

    template <typename T, bool IsWrapper = IsTypeWrapper<T>::value>
    struct Unwrap
    { typedef T type; };

    template <typename T>
    struct Unwrap<T, true>
    { typedef typename Unwrap< typename UnwrapOne<T>::type >::type type; };

    //---------------------------------------------------------------------------------------------
    // Used as a flag to indicate that an interface should not be used as a base class.
    // See DeriveTypeList below.
    template <typename T>
    struct NoDerive : public TypeWrapper<T>
    { };

    //---------------------------------------------------------------------------------------------
    // Used to indicate that a base class contributes implemented interfaces.
    template <typename T>
    struct ItfBase : public TypeWrapper<T>
    { };

    namespace detail
    {
        using namespace mpl;

        //-----------------------------------------------------------------------------------------
        // Exposes a type that derives every type in the given type list, except for those marked
        // with NoDerive.
        template <typename ListT>
        struct DeriveTypeList;

        // Common case. Derive from list head and recursively on list tail.
        template <typename HeadT, typename TailT>
        struct DeriveTypeList< type_list<HeadT, TailT> > :
            public Unwrap<HeadT>::type,
            public DeriveTypeList<TailT>
        {};

        // Non-derived case. Skip this type, continue with tail.
        template <typename HeadT, typename TailT>
        struct DeriveTypeList< type_list< NoDerive< HeadT >, TailT> > :
            public DeriveTypeList<TailT>
        {};

        // Termination case.
        template <>
        struct DeriveTypeList<null_type>
        {};

        //-----------------------------------------------------------------------------------------
        template <typename ItfTypeListT>
        struct GetFirstInterface;

        template <typename HeadT, typename TailT>
        struct GetFirstInterface< type_list<HeadT, TailT> >
        { typedef HeadT type; };
        
        template <typename HeadT, typename TailT>
        struct GetFirstInterface< type_list< ItfBase< HeadT >, TailT> >
        { typedef typename GetFirstInterface<TailT>::type type; };

        template <>
        struct GetFirstInterface< null_type >
        { typedef IUnknown type; };

        //-----------------------------------------------------------------------------------------
        // Uses type lists to implement the helper. Type lists are implemented
        // through templates, and can be best understood if compared to Scheme
        // cdr and cons: each list type has a head type and a tail type. The
        // head type is typically a concrete type, and the tail type is
        // typically another list containing the remainder of the list. Type
        // lists are terminated with a head type of null_type.
        //
        // QueryInterface is implemented using QIHelper, which uses type_lists
        // and partial specialization to recursively walk the type list and
        // look to see if the requested interface is supported. If not, then
        // the termination case is reached and a final test against IUknown
        // is made before returning a failure.
        //-----------------------------------------------------------------------------------------
        template <typename InterfaceTypeList>
        struct QIHelper;

        template <typename HeadT, typename TailT>
        struct QIHelper< type_list< HeadT, TailT > >
        {
            template <typename IUnknownCommonT>
            static inline HRESULT QI(
                REFIID           riid,
                void           **ppvObject,
                IUnknownCommonT *pThis)
            {
                STATIC_CONTRACT_NOTHROW;
                STATIC_CONTRACT_GC_NOTRIGGER;
                STATIC_CONTRACT_ENTRY_POINT;

                HRESULT hr = S_OK;

                typedef typename Unwrap<HeadT>::type ItfT;

                // If the interface type matches that of the head of the list,
                // then cast to it and return success.
                if (riid == COMUTIL_IIDOF(ItfT))
                {
                    ItfT *pItf = static_cast<ItfT *>(pThis);
                    pItf->AddRef();
                    *ppvObject = pItf;
                }
                // If not, recurse on the tail of the list.
                else
                    hr = QIHelper<TailT>::QI(riid, ppvObject, pThis);

                return hr;
            }
        };

        template <typename HeadT, typename TailT>
        struct QIHelper< type_list< ItfBase< HeadT >, TailT> >
        {
            template <typename IUnknownCommonT>
            static inline HRESULT QI(
                REFIID           riid,
                void           **ppvObject,
                IUnknownCommonT *pThis)
            {
                STATIC_CONTRACT_NOTHROW;
                STATIC_CONTRACT_GC_NOTRIGGER;
                STATIC_CONTRACT_ENTRY_POINT;

                HRESULT hr = S_OK;

                hr = pThis->HeadT::QueryInterface(riid, ppvObject);

                if (hr == E_NOINTERFACE)
                    hr = QIHelper<TailT>::QI(riid, ppvObject, pThis);

                return hr;
            }
        };

        // This is the termination case. In this case, we check if the
        // requested interface is IUnknown (which is common to all interfaces).
        template <>
        struct QIHelper< null_type >
        {
            template <typename IUnknownCommonT>
            static inline HRESULT QI(
                REFIID           riid,
                void           **ppvObject,
                IUnknownCommonT *pThis)
            {
                STATIC_CONTRACT_NOTHROW;
                STATIC_CONTRACT_GC_NOTRIGGER;
                STATIC_CONTRACT_ENTRY_POINT;

                HRESULT hr = S_OK;

                // If the request was for IUnknown, cast and return success.
                if (riid == COMUTIL_IIDOF(IUnknown))
                {
                    typedef typename detail::GetFirstInterface<
                        typename IUnknownCommonT::InterfaceListT>::type IUnknownCastHelper;

                    // Cast to first interface type to then cast to IUnknown unambiguously.
                    IUnknown *pItf = static_cast<IUnknown *>(
                        static_cast<IUnknownCastHelper *>(pThis));
                    pItf->AddRef();
                    *ppvObject = pItf;
                }
                // Otherwise none of the interfaces match the requested IID,
                // so return E_NOINTERFACE.
                else
                {
                    *ppvObject = nullptr;
                    hr = E_NOINTERFACE;
                }

                return hr;
            }
        };

        //-----------------------------------------------------------------------------------------
        // Is used as a virtual base to ensure that there is a single reference count field.
        struct IUnknownCommonRef
        {
            inline
            IUnknownCommonRef()
                : m_cRef(0)
            {}

            Volatile<LONG> m_cRef;
        };
    }

    //---------------------------------------------------------------------------------------------
    // IUnknownCommon
    //
    //   T0-T9 - the list of interfaces to implement.
    template
    <
        typename T0 = mpl::null_type,
        typename T1 = mpl::null_type,
        typename T2 = mpl::null_type,
        typename T3 = mpl::null_type,
        typename T4 = mpl::null_type,
        typename T5 = mpl::null_type,
        typename T6 = mpl::null_type,
        typename T7 = mpl::null_type,
        typename T8 = mpl::null_type,
        typename T9 = mpl::null_type
    >
    class IUnknownCommon :
        virtual protected detail::IUnknownCommonRef,
        public detail::DeriveTypeList< typename mpl::make_type_list<
            T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>::type >
    {
    public:
        typedef typename mpl::make_type_list<
            T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>::type InterfaceListT;

        // Add a virtual destructor to force derived types to also have virtual destructors.
        virtual ~IUnknownCommon()
        {
            WRAPPER_NO_CONTRACT;
            clr::dbg::PoisonMem(*this);
        }

        // Standard AddRef implementation
        STDMETHOD_(ULONG, AddRef())
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            STATIC_CONTRACT_ENTRY_POINT;

            return InterlockedIncrement(&m_cRef);
        }

        // Standard Release implementation.
        STDMETHOD_(ULONG, Release())
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            STATIC_CONTRACT_ENTRY_POINT;

            _ASSERTE(m_cRef > 0);

            ULONG cRef = InterlockedDecrement(&m_cRef);
            
            if (cRef == 0)
                delete this; // Relies on virtual dtor to work properly.

            return cRef;
        }

        // Uses detail::QIHelper for implementation.
        STDMETHOD(QueryInterface(REFIID riid, void **ppvObject))
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            STATIC_CONTRACT_ENTRY_POINT;

            if (ppvObject == nullptr)
                return E_INVALIDARG;

            *ppvObject = nullptr;

            return detail::QIHelper<InterfaceListT>::QI(
                riid, ppvObject, this);
        }

        template <typename ItfT>
        HRESULT QueryInterface(ItfT **ppItf)
        {
            return QueryInterface(__uuidof(ItfT), reinterpret_cast<void**>(ppItf));
        }

    protected:
        // May only be constructed as a base type.
        inline IUnknownCommon() :
            IUnknownCommonRef()
        { WRAPPER_NO_CONTRACT; }
    };

    //---------------------------------------------------------------------------------------------
    // IUnknownCommonExternal
    //
    //   T0-T9 - the list of interfaces to implement.
    template
    <
        typename T0 = mpl::null_type,
        typename T1 = mpl::null_type,
        typename T2 = mpl::null_type,
        typename T3 = mpl::null_type,
        typename T4 = mpl::null_type,
        typename T5 = mpl::null_type,
        typename T6 = mpl::null_type,
        typename T7 = mpl::null_type,
        typename T8 = mpl::null_type,
        typename T9 = mpl::null_type
    >
    class IUnknownCommonExternal :
        virtual protected detail::IUnknownCommonRef,
        public detail::DeriveTypeList< typename mpl::make_type_list<
            T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>::type >
    {
    public:
        typedef typename mpl::make_type_list<
            T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>::type InterfaceListT;

        // Standard AddRef implementation
        STDMETHOD_(ULONG, AddRef())
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            STATIC_CONTRACT_ENTRY_POINT;

            return InterlockedIncrement(&m_cRef);
        }

        // Standard Release implementation.
        // Should be called outside VM only
        STDMETHOD_(ULONG, Release())
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            STATIC_CONTRACT_ENTRY_POINT;

            _ASSERTE(m_cRef > 0);

            ULONG cRef = InterlockedDecrement(&m_cRef);
            
            if (cRef == 0)
            {
                Cleanup();          // Cleans up the object
                delete this;
            }
            
            return cRef;
        }

        // Internal release
        // Should be called inside VM only
        STDMETHOD_(ULONG, InternalRelease())
        {
            LIMITED_METHOD_CONTRACT;
            
            _ASSERTE(m_cRef > 0);

            ULONG cRef = InterlockedDecrement(&m_cRef);
            
            if (cRef == 0)
            {
                InternalCleanup();  // Cleans up the object, internal version
                delete this;
            }
            
            return cRef;
        }
        
        // Uses detail::QIHelper for implementation.
        STDMETHOD(QueryInterface(REFIID riid, void **ppvObject))
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            STATIC_CONTRACT_ENTRY_POINT;

            if (ppvObject == nullptr)
                return E_INVALIDARG;

            *ppvObject = nullptr;

            return detail::QIHelper<InterfaceListT>::QI(
                riid, ppvObject, this);
        }

        template <typename ItfT>
        HRESULT QueryInterface(ItfT **ppItf)
        {
            return QueryInterface(__uuidof(ItfT), reinterpret_cast<void**>(ppItf));
        }

    protected:
        // May only be constructed as a base type.
        inline IUnknownCommonExternal() :
            IUnknownCommonRef()
        { WRAPPER_NO_CONTRACT; }

        // Internal version of cleanup
        virtual void InternalCleanup() = 0;

        // External version of cleanup
        // Not surprisingly, this should call InternalCleanup to avoid duplicate code
        // Not implemented here to avoid bringing too much into this header file
        virtual void Cleanup() = 0;
    };
}

#undef COMUTIL_IIDOF

using ComUtil::NoDerive;
using ComUtil::ItfBase;
using ComUtil::IUnknownCommon;
using ComUtil::IUnknownCommonExternal;

#endif // __InternalUnknownImpl_h__
