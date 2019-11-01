// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// QCall.H



#ifndef __QCall_h__
#define __QCall_h__

#include "clr_std/type_traits"

//
// QCALLS
//

// QCalls are internal calls from managed code in mscorlib.dll to unmanaged code in mscorwks.dll. QCalls are very much like 
// a normal P/Invoke from mscorlib.dll to mscorwks.dll.
//
// Unlike FCalls, QCalls will marshal all arguments as unmanaged types like a normal P/Invoke. QCall also switch to preemptive
// GC mode like a normal P/Invoke. These two features should make QCalls easier to write reliably compared to FCalls.
// QCalls are not prone to GC holes and GC starvation bugs that are common with FCalls.
//
// QCalls perform better compared to FCalls w/ HelperMethodFrame. The QCall overhead is about 1.4x less compared to 
// FCall w/ HelperMethodFrame overhead on x86. The performance is about the same on x64. However, the implementation 
// of P/Invoke marshaling on x64 is not tuned for performance yet. The QCalls should become significantly faster compared 
// to FCalls w/ HelperMethodFrame on x64 as we do performance tuning of P/Invoke marshaling on x64.
//
//
// The preferred type of QCall arguments is primitive types that efficiently handled by the P/Invoke marshaler (INT32, LPCWSTR, BOOL).
// (Notice that BOOL is the correct boolean flavor for QCall arguments. CLR_BOOL is the correct boolean flavor for FCall arguments.)
//
// The pointers to common unmanaged EE structures should be wrapped into helper handle types. This is to make the managed implementation
// type safe and avoid falling into unsafe C# everywhere. See the AssemblyHandle below for a good example.
//
// There is a way to pass raw object references in and out of QCalls. It is done by wrapping a pointer to 
// a local variable in a handle. It is intentionally cumbersome and should be avoided if reasonably possible.  
// See the StringHandleOnStack in the example below. String arguments will get marshaled in as LPCWSTR. 
// Returning objects, especially strings, from QCalls is the only common pattern 
// where returning the raw objects (as an OUT argument) is widely acceptable.
//
//
// QCall example - managed part (do not replicate the comments into your actual QCall implementation):
// ---------------------------------------------------------------------------------------------------
//
// class Foo {
//
//  // All QCalls should have the following DllImport and SuppressUnmanagedCodeSecurity attributes
//  [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
//  [SuppressUnmanagedCodeSecurity]
//  // QCalls should always be static extern.
//  private static extern bool Bar(int flags, string inString, StringHandleOnStack retString);
//
//  // Many QCalls have a thin managed wrapper around them to expose them to the world in more meaningful way.
//  public string Bar(int flags)
//  {
//      string retString = null;
//
//      // The strings are returned from QCalls by taking address
//      // of a local variable using JitHelpers.GetStringHandleOnStack method 
//      if (!Bar(flags, this.Id, JitHelpers.GetStringHandleOnStack(ref retString)))
//          FatalError();
//
//      return retString;
//  }
//
//  Every QCall produces a couple of bogus FXCop warnings currently. Just add them to the FXCop exlusion list for now.
//
//
// QCall example - unmanaged part (do not replicate the comments into your actual QCall implementation):
// -----------------------------------------------------------------------------------------------------
//
// The entrypoints of all QCalls has to be registered in tables in vm\ecall.cpp using QCFuncEntry macro, 
// For example: QCFuncElement("Bar", FooNative::Bar)
//
// class FooNative {
// public:
//      // All QCalls should be static and should be tagged with QCALLTYPE
//      static
//      BOOL QCALLTYPE Bar(int flags, LPCWSTR wszString, QCall::StringHandleOnStack retString);
// };
//
// BOOL QCALLTYPE FooNative::Bar(int flags, LPCWSTR wszString, QCall::StringHandleOnStack retString)
// {
//      // All QCalls should have QCALL_CONTRACT. It is alias for THROWS; GC_TRIGGERS; MODE_PREEMPTIVE.
//      QCALL_CONTRACT;
//
//      // Optionally, use QCALL_CHECK instead and the expanded form of the contract if you want to specify preconditions:
//      // CONTRACTL { 
//      //     QCALL_CHECK; 
//      //     PRECONDITION(wszString != NULL);
//      // } CONTRACTL_END;
//        
//      // The only line between QCALL_CONTRACT and BEGIN_QCALL
//      // should be the return value declaration if there is one.
//      BOOL retVal = FALSE;
//
//      // The body has to be enclosed in BEGIN_QCALL/END_QCALL macro. It is necessary to make the exception handling work.
//      BEGIN_QCALL;
//
//      // Validate arguments if necessary and throw exceptions like anywhere else in the EE. There is no convention currently 
//      // on whether the argument validation should be done in managed or unmanaged code.
//      if (flags != 0)
//          COMPlusThrow(kArgumentException, L"InvalidFlags");
//
//      // No need to worry about GC moving strings passed into QCall. Marshaling pins them for us.
//      printf("%S", wszString);
//
//      // This is the most efficient way to return strings back to managed code. No need to use StringBuilder.
//      retString.Set(L"Hello");
//
//      // You can not return from inside of BEGIN_QCALL/END_QCALL. The return value has to be passed out in helper variable.
//      retVal = TRUE;
//
//      END_QCALL;
//
//      return retVal;
// }


#ifdef PLATFORM_UNIX
#define QCALLTYPE __cdecl
#else // PLATFORM_UNIX
#define QCALLTYPE __stdcall
#endif // !PLATFORM_UNIX

#define BEGIN_QCALL                      \
    INSTALL_MANAGED_EXCEPTION_DISPATCHER \
    INSTALL_UNWIND_AND_CONTINUE_HANDLER

#define END_QCALL                         \
    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER \
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER

#define QCALL_CHECK             \
    THROWS;                     \
    GC_TRIGGERS;                \
    MODE_PREEMPTIVE;            \

#define QCALL_CONTRACT CONTRACTL { QCALL_CHECK; } CONTRACTL_END;

//
// Scope class for QCall helper methods and types
// 
class QCall
{
public:

    //
    // Helper types to aid marshaling of QCall arguments in type-safe manner
    // 
    // The C/C++ compiler has to treat these types as POD (plain old data) to generate
    // a calling convention compatible with P/Invoke marshaling. This means that:
    // NONE OF THESE HELPER TYPES CAN HAVE A CONSTRUCTOR OR DESTRUCTOR!
    // THESE HELPER TYPES CAN NOT BE IMPLEMENTED USING INHERITANCE OR TEMPLATES!
    //

    //
    // StringHandleOnStack is used for managed strings
    //
    struct StringHandleOnStack
    {
        StringObject ** m_ppStringObject;

#ifndef DACCESS_COMPILE
        //
        // Helpers for returning managed string from QCall
        //

        // Raw setter - note that you need to be in cooperative mode
        void Set(STRINGREF s)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;
            }
            CONTRACTL_END;

            // The space for the return value has to be on the stack
            _ASSERTE(Thread::IsAddressInCurrentStack(m_ppStringObject));

            *m_ppStringObject = STRINGREFToObject(s);
        }

        void Set(const SString& value);
        void Set(LPCWSTR pwzValue);
        void Set(LPCUTF8 pszValue);
#endif // !DACCESS_COMPILE
    };

    //
    // ObjectHandleOnStack type is used for managed objects
    //
    struct ObjectHandleOnStack
    {
        Object ** m_ppObject;

#ifndef DACCESS_COMPILE
        //
        // Helpers for returning common managed types from QCall
        //
        void Set(OBJECTREF o)
        {
            LIMITED_METHOD_CONTRACT;

            // The space for the return value has to be on the stack
            _ASSERTE(Thread::IsAddressInCurrentStack(m_ppObject));

            *m_ppObject = OBJECTREFToObject(o);
        }

        void SetByteArray(const BYTE * p, COUNT_T length);
        void SetIntPtrArray(const PVOID * p, COUNT_T length);
        void SetGuidArray(const GUID * p, COUNT_T length);

       // Do not add operator overloads to convert this object into a stack reference to a specific object type
       // such as OBJECTREF *. While such things are correct, our debug checking logic is unable to verify that
       // the object reference is actually protected from access and therefore will assert.
       // See bug 254159 for details.

#endif // !DACCESS_COMPILE
    };

    //
    // StackCrawlMarkHandle is used for passing StackCrawlMark into QCalls
    //
    struct StackCrawlMarkHandle
    {
        StackCrawlMark * m_pMark;

        operator StackCrawlMark * ()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pMark;
        }
    };

    struct AssemblyHandle
    {
        Object ** m_ppObject;
        DomainAssembly * m_pAssembly;

        operator DomainAssembly * ()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pAssembly;
        }

        DomainAssembly * operator->() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_pAssembly;
        }
    };

    struct ModuleHandle
    {
        Object ** m_ppObject;
        Module * m_pModule;

        operator Module * ()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pModule;
        }

        Module * operator->() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_pModule;
        }
    };

    struct TypeHandle
    {
        Object ** m_ppObject;
        PTR_VOID m_pTypeHandle;

        ::TypeHandle AsTypeHandle()
        {
            LIMITED_METHOD_CONTRACT;
            return ::TypeHandle::FromPtr(m_pTypeHandle);
        }
    };

    struct LoaderAllocatorHandle
    {
        LoaderAllocator * m_pLoaderAllocator;

        operator LoaderAllocator * ()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pLoaderAllocator;
        }

        LoaderAllocator * operator -> () const
        {
            LIMITED_METHOD_CONTRACT;
            return m_pLoaderAllocator;
        }

        static LoaderAllocatorHandle From(LoaderAllocator * pLoaderAllocator)
        {
            LoaderAllocatorHandle h;
            h.m_pLoaderAllocator = pLoaderAllocator;
            return h;
        }
    };

    // The lifetime management between managed and native Thread objects is broken. There is a resurrection 
    // race where one can get a dangling pointer to the unmanaged Thread object. Once this race is fixed 
    // we may need to revisit how the unmanaged thread handles are passed around.
    struct ThreadHandle
    {
        Thread * m_pThread;

        operator Thread * ()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pThread;
        }

        Thread * operator->() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_pThread;
        }
    };
};

typedef void* EnregisteredTypeHandle;

#endif //__QCall_h__
