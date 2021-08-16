// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// File: genmeth.cpp
//
// Most functionality for generic methods is put here
//



#include "common.h"
#include "method.hpp"
#include "field.h"
#include "eeconfig.h"
#include "crst.h"
#include "generics.h"
#include "genericdict.h"
#include "instmethhash.h"
#include "typestring.h"
#include "typedesc.h"
#include "comdelegate.h"

// Instantiated generic methods
//
// Method descriptors for instantiated generic methods are allocated on demand and inserted
// into the InstMethodHashTable for the LoaderModule of the descriptor. (See ceeload.h for more
// information about loader modules).
//
// For non-shared instantiations, entering the prestub for such a method descriptor causes the method to
// be JIT-compiled, specialized to that instantiation.
//
// For shared instantiations, entering the prestub generates a piece of stub code that passes the
// method descriptor as an extra argument and then jumps to code shared between compatible
// instantiations. This code has its own method descriptor whose instantiation is *canonical*
// (with reference-type type parameters replaced by Object).
//
// Thus for example the shared method descriptor for m<object> is different to the
// exact-instantiation method descriptor for m<object>.
//
// Complete example:
//
// class C<T> { public void m<S>(S x, T y) { ... } }
//
// Suppose that code sharing is turned on.
//
// Upon compiling calls to C<string>.m<string>, C<string>.m<Type>, C<Type>.m<string> and C<Type>.m<Type>

// Given a generic method descriptor and an instantiation, create a new instantiated method
// descriptor and chain it into the list attached to the generic method descriptor
//
// pMT is the owner method table.  If looking for a shared MD this should be
// the MT for the shared class.
//
// pGenericMD is the generic method descriptor (owner may be instantiated)
// pWrappedMD is the corresponding shared  md for use when creating stubs
// nGenericMethodArgs/genericMethodArgs is the instantiation
// getWrappedCode=TRUE if you want a shared instantiated md whose code expects an extra argument.  In this
// case pWrappedMD should be NULL.
//
// The result is put in ppMD
//
// If getWrappedCode.  In thise case the genericMethodArgs
// should be the normalized representative genericMethodArgs (see typehandle.h)
//


// Helper method that creates a method-desc off a template method desc
static MethodDesc* CreateMethodDesc(LoaderAllocator *pAllocator,
                                    MethodTable *pMT,
                                    MethodDesc *pTemplateMD,
                                    DWORD classification,
                                    BOOL fNativeCodeSlot,
                                    BOOL fComPlusCallInfo,
                                    AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pAllocator));
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pTemplateMD));
        PRECONDITION(pTemplateMD->IsRestored());
        PRECONDITION(pMT->IsRestored_NoLogging());
        PRECONDITION(pTemplateMD->GetMethodTable()->GetCanonicalMethodTable() == pMT->GetCanonicalMethodTable());
    }
    CONTRACTL_END

    mdMethodDef token = pTemplateMD->GetMemberDef();

    // Create a singleton chunk for the method desc
    MethodDescChunk *pChunk =
        MethodDescChunk::CreateChunk(pAllocator->GetHighFrequencyHeap(),
                                     1,
                                     classification,
                                     TRUE /* fNonVtableSlot*/,
                                     fNativeCodeSlot,
                                     fComPlusCallInfo,
                                     pMT,
                                     pamTracker);

    // Now initialize the MDesc at the single method descriptor in
    // the new chunk
    MethodDesc *pMD = pChunk->GetFirstMethodDesc();

    //We copy over the flags one by one.  This is fragile w.r.t. adding
    // new flags, but other techniques are also fragile.  <NICE>We should move
    // to using constructors on MethodDesc</NICE>
    if (pTemplateMD->IsStatic())
    {
        pMD->SetStatic();
    }
    if (pTemplateMD->IsNotInline())
    {
        pMD->SetNotInline(true);
    }
    if (pTemplateMD->IsSynchronized())
    {
        pMD->SetSynchronized();
    }
    if (pTemplateMD->IsJitIntrinsic())
    {
        pMD->SetIsJitIntrinsic();
    }

    pMD->SetMemberDef(token);
    pMD->SetSlot(pTemplateMD->GetSlot());

#ifdef _DEBUG
    pMD->m_pszDebugMethodName = pTemplateMD->m_pszDebugMethodName;
    //<NICE> more info here</NICE>
    pMD->m_pszDebugMethodSignature = "<generic method signature>";
    pMD->m_pszDebugClassName  = "<generic method class name>";
    pMD->m_pszDebugMethodName = "<generic method name>";
    pMD->m_pDebugMethodTable.SetValue(pMT);
#endif // _DEBUG

    return pMD;
}

//
// The following methods map between tightly bound boxing and unboxing MethodDesc.
// We always layout boxing and unboxing MethodDescs next to each other in same
// MethodDescChunk. It allows us to avoid brute-force iteration over all methods
// on the type to perform the mapping.
//

//
// Find matching tightly-bound methoddesc
//
static MethodDesc * FindTightlyBoundWrappedMethodDesc(MethodDesc * pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END

    if (pMD->IsUnboxingStub() && pMD->GetClassification() == mcInstantiated)
        pMD = pMD->AsInstantiatedMethodDesc()->IMD_GetWrappedMethodDesc();

    // Find matching MethodDesc in the MethodTable
    if (!pMD->IsTightlyBoundToMethodTable())
        pMD = pMD->GetCanonicalMethodTable()->GetParallelMethodDesc(pMD);
    _ASSERTE(pMD->IsTightlyBoundToMethodTable());

    // Real MethodDesc immediately follows unboxing stub
    if (pMD->IsUnboxingStub())
        pMD = MethodTable::IntroducedMethodIterator::GetNext(pMD);
    _ASSERTE(!pMD->IsUnboxingStub());

    return pMD;
}

//
// Find matching tightly-bound unboxing stub if there is one
//
static MethodDesc * FindTightlyBoundUnboxingStub(MethodDesc * pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END

    // Find matching MethodDesc in the MethodTable
    if (!pMD->IsTightlyBoundToMethodTable())
        pMD = pMD->GetCanonicalMethodTable()->GetParallelMethodDesc(pMD);
    _ASSERTE(pMD->IsTightlyBoundToMethodTable());

    // We are done if we have unboxing stub already
    if (pMD->IsUnboxingStub())
        return pMD;

    //
    // Unboxing stub immediately precedes real methoddesc
    //
    MethodDesc * pCurMD = pMD->GetMethodDescChunk()->GetFirstMethodDesc();

    if (pCurMD == pMD)
        return NULL;

    for (;;)
    {
        MethodDesc * pNextMD = MethodTable::IntroducedMethodIterator::GetNext(pCurMD);
        if (pNextMD == pMD)
            break;
        pCurMD = pNextMD;
    }

    return pCurMD->IsUnboxingStub() ? pCurMD : NULL;
}

#ifdef _DEBUG
//
// Alternative brute-force implementation of FindTightlyBoundWrappedMethodDesc for debug-only check.
//
// Please note that this does not do the same up-front checks as the non-debug version to
// see whether or not the input pMD is even an unboxing stub in the first place.
//
static MethodDesc * FindTightlyBoundWrappedMethodDesc_DEBUG(MethodDesc * pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END

    mdMethodDef methodDef = pMD->GetMemberDef();
    Module *pModule = pMD->GetModule();

    MethodTable::MethodIterator it(pMD->GetCanonicalMethodTable());
    it.MoveToEnd();
    for (; it.IsValid(); it.Prev()) {
        if (!it.IsVirtual()) {
            // Get the MethodDesc for current method
            MethodDesc* pCurMethod = it.GetMethodDesc();

            if (pCurMethod && !pCurMethod->IsUnboxingStub()) {
                if ((pCurMethod->GetMemberDef() == methodDef)  &&
                    (pCurMethod->GetModule() == pModule))
                {
                    return pCurMethod;
                }
            }
        }
    }
    return NULL;
}

//
// Alternative brute-force implementation of FindTightlyBoundUnboxingStub for debug-only check
//
// Please note that this does not do the same up-front checks as the non-debug version to
// see whether or not the input pMD even qualifies to have a corresponding unboxing stub.
//
static MethodDesc * FindTightlyBoundUnboxingStub_DEBUG(MethodDesc * pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END

    mdMethodDef methodDef = pMD->GetMemberDef();
    Module *pModule = pMD->GetModule();

    MethodTable::MethodIterator it(pMD->GetCanonicalMethodTable());
    it.MoveToEnd();
    for (; it.IsValid(); it.Prev()) {
        if (it.IsVirtual()) {
            MethodDesc* pCurMethod = it.GetMethodDesc();
            if (pCurMethod && pCurMethod->IsUnboxingStub()) {
                if ((pCurMethod->GetMemberDef() == methodDef) &&
                    (pCurMethod->GetModule() == pModule)) {
                    return pCurMethod;
                }
            }
        }
    }
    return NULL;
}
#endif // _DEBUG

/* static */
InstantiatedMethodDesc *
InstantiatedMethodDesc::NewInstantiatedMethodDesc(MethodTable *pExactMT,
                                                  MethodDesc* pGenericMDescInRepMT,
                                                  MethodDesc* pWrappedMD,
                                                  Instantiation methodInst,
                                                  BOOL getWrappedCode)
{
    CONTRACT(InstantiatedMethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pExactMT));
        PRECONDITION(CheckPointer(pGenericMDescInRepMT));
        PRECONDITION(pGenericMDescInRepMT->IsRestored());
        PRECONDITION(pWrappedMD == NULL || pWrappedMD->IsRestored());
        PRECONDITION(methodInst.IsEmpty() || pGenericMDescInRepMT->IsGenericMethodDefinition());
        PRECONDITION(methodInst.GetNumArgs() == pGenericMDescInRepMT->GetNumGenericMethodArgs());
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->IsRestored());
        POSTCONDITION(getWrappedCode == RETVAL->IsSharedByGenericInstantiations());
        POSTCONDITION(methodInst.IsEmpty() || RETVAL->HasMethodInstantiation());
    }
    CONTRACT_END;

    // All instantiated method descs live off the RepMT for the
    // instantiated class they live in.
    INDEBUG(MethodTable * pCanonMT = pExactMT->GetCanonicalMethodTable();)

    _ASSERTE(pGenericMDescInRepMT->GetMethodTable() == pCanonMT);

    if (getWrappedCode)
    {
        _ASSERTE(pWrappedMD == NULL);
        _ASSERTE(pExactMT->IsCanonicalMethodTable());
        _ASSERTE(pCanonMT == pExactMT);
        _ASSERTE(pExactMT->IsSharedByGenericInstantiations() || ClassLoader::IsSharableInstantiation(methodInst));

    }

    InstantiatedMethodDesc *pNewMD;
    //@todo : move this into the domain
    Module * pExactMDLoaderModule = ClassLoader::ComputeLoaderModule(pExactMT, pGenericMDescInRepMT->GetMemberDef(), methodInst);

    LoaderAllocator * pAllocator = pExactMDLoaderModule->GetLoaderAllocator();

    // Create LoaderAllocator to LoaderAllocator links for members of the instantiations of this method
    pAllocator->EnsureInstantiation(pExactMT->GetLoaderModule(), pExactMT->GetInstantiation());
    pAllocator->EnsureInstantiation(pGenericMDescInRepMT->GetLoaderModule(), methodInst);

    {
        // Acquire crst to prevent tripping up other threads searching in the same hashtable
        CrstHolder ch(&pExactMDLoaderModule->m_InstMethodHashTableCrst);

        // Check whether another thread beat us to it!
        pNewMD = FindLoadedInstantiatedMethodDesc(pExactMT,
                                                  pGenericMDescInRepMT->GetMemberDef(),
                                                  methodInst,
                                                  getWrappedCode);

        // Crst goes out of scope here
        // We don't need to hold the crst while we build the MethodDesc, but we reacquire it later
    }

    if (pNewMD != NULL)
    {
        pNewMD->CheckRestore();
    }
    else
    {
        TypeHandle *pInstOrPerInstInfo = NULL;
        DictionaryLayout *pDL = NULL;
        DWORD infoSize = 0;
        IBCLoggerAwareAllocMemTracker amt;

        if (!methodInst.IsEmpty())
        {
            if (pWrappedMD)
            {
                if (pWrappedMD->IsSharedByGenericMethodInstantiations())
                {
                    // Note that it is possible for the dictionary layout to be expanded in size by other threads while we're still
                    // creating this method. In other words: this method will have a smaller dictionary that its layout. This is not a
                    // problem however because whenever we need to load a value from the dictionary of this method beyond its size, we
                    // will expand the dictionary at that point.
                    pDL = pWrappedMD->AsInstantiatedMethodDesc()->GetDictLayoutRaw();
                }
            }
            else if (getWrappedCode)
            {
                pDL = DictionaryLayout::Allocate(NUM_DICTIONARY_SLOTS, pAllocator, &amt);
#ifdef _DEBUG
                {
                    SString name;
                    TypeString::AppendMethodDebug(name, pGenericMDescInRepMT);
                    DWORD dictionarySlotSize;
                    DWORD dictionaryAllocSize = DictionaryLayout::GetDictionarySizeFromLayout(pGenericMDescInRepMT->GetNumGenericMethodArgs(), pDL, &dictionarySlotSize);
                    LOG((LF_JIT, LL_INFO1000, "GENERICS: Created new dictionary layout for dictionary of slot size %d / alloc size %d for %S\n",
                        dictionarySlotSize, dictionaryAllocSize, name.GetUnicode()));
                }
#endif // _DEBUG
            }

            // Allocate space for the instantiation and dictionary
            DWORD allocSize = DictionaryLayout::GetDictionarySizeFromLayout(methodInst.GetNumArgs(), pDL, &infoSize);
            pInstOrPerInstInfo = (TypeHandle*)(void*)amt.Track(pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(allocSize)));
            for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
                pInstOrPerInstInfo[i] = methodInst[i];

            if (pDL != NULL)
            {
                _ASSERTE(pDL->GetMaxSlots() > 0);

                // Has to be at least larger than the first slots containing the instantiation arguments,
                // and the slot with size information. Otherwise, we shouldn't really have a size slot
                _ASSERTE(infoSize > sizeof(TypeHandle*) * (methodInst.GetNumArgs() + 1));

                DWORD* pDictSizeSlot = (DWORD*)(pInstOrPerInstInfo + methodInst.GetNumArgs());
                *pDictSizeSlot = infoSize;
            }
        }

        BOOL forComInterop = FALSE;

        // Create a new singleton chunk for the new instantiated method descriptor
        // Notice that we've passed in the method table pointer; this gets
        // used in some of the subsequent setup methods for method descs.
        //
        pNewMD = (InstantiatedMethodDesc*) (CreateMethodDesc(pAllocator,
                                                             pExactMT,
                                                             pGenericMDescInRepMT,
                                                             mcInstantiated,
                                                             !pWrappedMD, // This is pesimistic estimate for fNativeCodeSlot
                                                             forComInterop,
                                                             &amt));

        // Initialize the MD the way it needs to be
        if (pWrappedMD)
        {
            pNewMD->SetupWrapperStubWithInstantiations(pWrappedMD, methodInst.GetNumArgs(), pInstOrPerInstInfo);
            _ASSERTE(pNewMD->IsInstantiatingStub());
        }
        else if (getWrappedCode)
        {
            pNewMD->SetupSharedMethodInstantiation(methodInst.GetNumArgs(), pInstOrPerInstInfo, pDL);
            _ASSERTE(!pNewMD->IsInstantiatingStub());
        }
        else
        {
            pNewMD->SetupUnsharedMethodInstantiation(methodInst.GetNumArgs(), pInstOrPerInstInfo);
        }

        // Check that whichever field holds the inst. got setup correctly
        _ASSERTE((PVOID)pNewMD->GetMethodInstantiation().GetRawArgs() == (PVOID)pInstOrPerInstInfo);

        pNewMD->SetTemporaryEntryPoint(pAllocator, &amt);

        {
            // The canonical instantiation is exempt from constraint checks. It's used as the basis
            // for all other reference instantiations so we can't not load it. The Canon type is
            // not visible to users so it can't be abused.

            BOOL fExempt =
                TypeHandle::IsCanonicalSubtypeInstantiation(methodInst) ||
                TypeHandle::IsCanonicalSubtypeInstantiation(pNewMD->GetClassInstantiation());

            if (!fExempt)
            {
                pNewMD->SatisfiesMethodConstraints(TypeHandle(pExactMT), TRUE);
            }
        }

        // OK, now we have a candidate MethodDesc.
        {
            CrstHolder ch(&pExactMDLoaderModule->m_InstMethodHashTableCrst);

            // We checked before, but make sure again that another thread didn't beat us to it!
            InstantiatedMethodDesc *pOldMD = FindLoadedInstantiatedMethodDesc(pExactMT,
                                                      pGenericMDescInRepMT->GetMemberDef(),
                                                      methodInst,
                                                      getWrappedCode);

            if (pOldMD == NULL)
            {
                // No one else got there first, our MethodDesc wins.
                amt.SuppressRelease();

#ifdef _DEBUG
                SString name(SString::Utf8);
                TypeString::AppendMethodDebug(name, pNewMD);
                StackScratchBuffer buff;
                const char* pDebugNameUTF8 = name.GetUTF8(buff);
                const char* verb = "Created";
                if (pWrappedMD)
                    LOG((LF_CLASSLOADER, LL_INFO1000,
                        "GENERICS: %s instantiating-stub method desc %s with dictionary size %d\n",
                        verb, pDebugNameUTF8, infoSize));
                else
                    LOG((LF_CLASSLOADER, LL_INFO1000,
                         "GENERICS: %s instantiated method desc %s\n",
                         verb, pDebugNameUTF8));

                S_SIZE_T safeLen = S_SIZE_T(strlen(pDebugNameUTF8))+S_SIZE_T(1);
                if(safeLen.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);

                size_t len = safeLen.Value();
                pNewMD->m_pszDebugMethodName = (char*) (void*)pAllocator->GetLowFrequencyHeap()->AllocMem(safeLen);
                _ASSERTE(pNewMD->m_pszDebugMethodName);
                strcpy_s((char *) pNewMD->m_pszDebugMethodName, len, pDebugNameUTF8);
                pNewMD->m_pszDebugClassName = pExactMT->GetDebugClassName();
                pNewMD->m_pszDebugMethodSignature = (LPUTF8)pNewMD->m_pszDebugMethodName;
#endif // _DEBUG

                // Generic methods can't be varargs. code:MethodTableBuilder::ValidateMethods should have checked it.
                _ASSERTE(!pNewMD->IsVarArg());

                // Verify that we are not creating redundant MethodDescs
                _ASSERTE(!pNewMD->IsTightlyBoundToMethodTable());

                // The method desc is fully set up; now add to the table
                InstMethodHashTable* pTable = pExactMDLoaderModule->GetInstMethodHashTable();
                pTable->InsertMethodDesc(pNewMD);
            }
            else
                pNewMD = pOldMD;
            // CrstHolder goes out of scope here
        }

    }

    RETURN pNewMD;
}

// Calling this method is equivalent to
// FindOrCreateAssociatedMethodDesc(pCanonicalMD, pExactMT, FALSE, Instantiation(), FALSE, TRUE)
// except that it also creates InstantiatedMethodDescs based on shared class methods. This is
// convenient for interop where, unlike ordinary managed methods, marshaling stubs for say Foo<string>
// and Foo<object> look very different and need separate representation.
InstantiatedMethodDesc*
InstantiatedMethodDesc::FindOrCreateExactClassMethod(MethodTable *pExactMT,
                                                     MethodDesc *pCanonicalMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!pExactMT->IsSharedByGenericInstantiations());
        PRECONDITION(pCanonicalMD->IsSharedByGenericInstantiations());
    }
    CONTRACTL_END;

    InstantiatedMethodDesc *pInstMD = FindLoadedInstantiatedMethodDesc(pExactMT,
                                                                       pCanonicalMD->GetMemberDef(),
                                                                       Instantiation(),
                                                                       FALSE);

    if (pInstMD == NULL)
    {
        // create a new MD if not found
        pInstMD = NewInstantiatedMethodDesc(pExactMT,
                                            pCanonicalMD,
                                            pCanonicalMD,
                                            Instantiation(),
                                            FALSE);
    }

    return pInstMD;
}

// N.B. it is not guarantee that the returned InstantiatedMethodDesc is restored.
// It is the caller's responsibility to call CheckRestore on the returned value.
/* static */
InstantiatedMethodDesc*
InstantiatedMethodDesc::FindLoadedInstantiatedMethodDesc(MethodTable *pExactOrRepMT,
                                                         mdMethodDef methodDef,
                                                         Instantiation methodInst,
                                                         BOOL getWrappedCode)
{
    CONTRACT(InstantiatedMethodDesc *)
    {
        THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pExactOrRepMT));

        // All wrapped method descriptors (except BoxedEntryPointStubs, which don't use this path) are
        // canonical and exhibit some kind of code sharing.
        PRECONDITION(!getWrappedCode || pExactOrRepMT->IsCanonicalMethodTable());
        PRECONDITION(!getWrappedCode || pExactOrRepMT->IsSharedByGenericInstantiations() || ClassLoader::IsSharableInstantiation(methodInst));

        // Unboxing stubs are dealt with separately in FindOrCreateAssociatedMethodDesc.  This should
        // probably be streamlined...
        POSTCONDITION(!RETVAL || !RETVAL->IsUnboxingStub());

        // All wrapped method descriptors (except BoxedEntryPointStubs, which don't use this path) take an inst arg.
        // The only ones that don't should have been found in the type's meth table.
        POSTCONDITION(!getWrappedCode || !RETVAL || !RETVAL->IsRestored() || RETVAL->RequiresInstArg());
    }
    CONTRACT_END


    // First look in the table for the runtime loader module in case someone created it before any
    // zap modules got loaded
    Module *pLoaderModule = ClassLoader::ComputeLoaderModule(pExactOrRepMT, methodDef, methodInst);

    InstMethodHashTable* pTable = pLoaderModule->GetInstMethodHashTable();
    MethodDesc *resultMD = pTable->FindMethodDesc(TypeHandle(pExactOrRepMT),
                                                  methodDef,
                                                  FALSE /* not forceBoxedEntryPoint */,
                                                  methodInst,
                                                  getWrappedCode);

    if (resultMD != NULL)
       RETURN((InstantiatedMethodDesc*) resultMD);

#ifdef FEATURE_PREJIT
    // Next look in the preferred zap module
    Module *pPreferredZapModule = Module::ComputePreferredZapModule(pExactOrRepMT->GetModule(),
                                                                    pExactOrRepMT->GetInstantiation(),
                                                                    methodInst);
    if (pPreferredZapModule->HasNativeImage())
    {
        resultMD = pPreferredZapModule->GetInstMethodHashTable()->FindMethodDesc(TypeHandle(pExactOrRepMT),
                                                                                 methodDef,
                                                                                 FALSE /* not forceBoxedEntryPoint */,
                                                                                 methodInst,
                                                                                 getWrappedCode);

       if (resultMD != NULL)
           RETURN((InstantiatedMethodDesc*) resultMD);
    }
#endif // FEATURE_PREJIT

    RETURN(NULL);
}


// Given a method descriptor, find (or create) an instantiated
// method descriptor or BoxedEntryPointStub associated with that method
// and a particular instantiation of any generic method arguments.  Also check
// the method instantiation is valid.
//
// This routine also works for non-generic methods - it will be fast
// in most cases.  In this case nothing in particular
// occurs except for static methods in shared generic classes, where an
// instantiating stub is needed.
//
// The generic parameters provided are only those for the generic method.
// pExactMT should be used to specify any class parameters.
//
// Unboxing stubs
// --------------
//
// These are required to provide callable addresses with a uniform calling convention
// for all methods on a boxed value class object.  There are a wide range of possible
// methods:
//     1 virtual, non-generic instance methods
//     2 non-virtual, non-generic instance methods
//     3 virtual, generic instance methods
//     4 non-virtual, generic instance methods
//  There is no substantial difference between case 3 and case 4: the only times
// when BoxedEntryPointStubs are used for non-virtual methods are when calling a delegate or
// making a reflection call.
//
// The only substantial difference between 1 and 2 is that the stubs are stored in
// different places - we are forced to create the BoxedEntryPointStubs for (1) at class
// creation time (they form part of the vtable and dispatch maps).  Hence these
// stubs are "owned" by method tables.  We store all other stubs in the AssociatedMethTable.
//
// Unboxing stubs and generics
// ---------------------------
//
// Generics code sharing complicates matters.  The typical cases are where the struct
// is in a shared-codegenerics struct such as
//
//    struct Pair<string,object>
//
// which shares code with other types such as Pair<object,object>.  All the code that ends up
// being run for all the methods in such a struct takes an instantiation parameter, i.e.
// is RequiresInstArg(), a non-uniform calling convention.  We obviously can't give these out as
// targets of delegate calls.  Hence we have to wrap this shared code in various stubs in
// order to get the right type context parameter provided to the shared code.
//
// Unboxing stubs on shared-code generic structs, e.g. Pair<object,string>,
// acquire the class-portion of their type context from the "this" pointer.
//
// Thus there are two flavours of BoxedEntryPointStubs:
//
//    - Methods that are not themselves generic:
//
//      These wrap possibly-shared code (hence allowInstParam == TRUE).
//
//      These directly call the possible-shared code for the instance method.  This code
//      can have the following calling conventions:
//           - RequiresMethodTableInstArg() (if pMT->SharedByGenericInstantiations())
//           - Uniform                      (if !pMT->SharedByGenericInstantiations())
//
//      Thus if the code they are
//
//    - Methods that are themselves generic:
//
//      These wrap unshared code (hence allowInstParam == FALSE):
//
//      These are always invoked by slow paths (how often do you use a generic method in a struct?),
//      such as JIT_VirtualFunctionPointer or a reflection call.  These paths eventually
//      use FindOrCreateAssociatedMethodDesc to piece together the exact instantiation provided by the "this"
//      pointer with the exact instantiation provided by the wrapped method pointer.
//
//      These call a stub for the instance method which provides the instantiation
//      context, possibly in turn calling further shared code.  This stub will
//      always be !RequiresInstArg()
//
//      If the method being called is aMethod calls via BoxedEntryPointStubs
//
// Remotable methods
// -----------------
//
// Remoting has high requirements for method descs passed to it (i.e. the method desc that represents the client "view" of the
// method to be called on the real server object). Since the identity of the method call is serialized and passed on the wire before
// be resolved into the real target method on the server remoting needs to be able to extract exact instantiation information from
// its inputs (a method desc and a this pointer).
//
// To that end generic methods should always be passed via an instantiating stub (i.e. set allowInstParam to FALSE when calling
// FindOrCreateAssociatedMethodDesc).
//
// There's a more subtle problem though. If the client method call is via a non-generic method on a generic interface we won't have
// enough information to serialize the call. That's because such methods don't have instantiated method descs by default (these are
// weighty structures and most of the runtime would never use the extra information). The this pointer doesn't help provide the
// additional information in this case (consider the case of a class that implements both IFoo<String> and IFoo<Random>).
//
// So instead we create instantiated interface method descs on demand (i.e. during stub-based interface dispatch). Setting the
// forceRemotableMethod predicate to TRUE below will ensure this (it's a no-op for methods that don't match this pattern, so can be
// freely set to true for all calls intended to produce a remotable ready method). This characteristic of a methoddesc that is fully
// descriptive of the method and class used is also necessary in certain places in reflection. In particular, it is known to be needed
// for the Delegate.CreateDelegate logic.
//
// allowCreate may be set to FALSE to enforce that the method searched
// should already be in existence - thus preventing creation and GCs during
// inappropriate times.

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
/* static */
MethodDesc*
MethodDesc::FindOrCreateAssociatedMethodDesc(MethodDesc* pDefMD,
                                             MethodTable *pExactMT,
                                             BOOL forceBoxedEntryPoint,
                                             Instantiation methodInst,
                                             BOOL allowInstParam,
                                             BOOL forceRemotableMethod,
                                             BOOL allowCreate,
                                             ClassLoadLevel level)
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        if (allowCreate) { GC_TRIGGERS; } else { GC_NOTRIGGER; }
        INJECT_FAULT(COMPlusThrowOM(););

        PRECONDITION(CheckPointer(pDefMD));
        PRECONDITION(CheckPointer(pExactMT));
        PRECONDITION(pDefMD->IsRestored_NoLogging());
        PRECONDITION(pExactMT->IsRestored_NoLogging());

        // If the method descriptor belongs to a generic type then
        // the input exact type must be an instantiation of that type.
        // DISABLED PRECONDITION - too strict - the classes may be in
        // a subtype relation to each other.
        //
        // PRECONDITION(!pDefMD->HasClassInstantiation() || pDefMD->GetMethodTable()->HasSameTypeDefAs(pExactMT));

        // You may only request an BoxedEntryPointStub for an instance method on a value type
        PRECONDITION(!forceBoxedEntryPoint || pExactMT->IsValueType());
        PRECONDITION(!forceBoxedEntryPoint || !pDefMD->IsStatic());

        // For remotable methods we better not be allowing instantiation parameters.
        PRECONDITION(!forceRemotableMethod || !allowInstParam);

        POSTCONDITION(((RETVAL == NULL) && !allowCreate) || CheckPointer(RETVAL));
        POSTCONDITION(((RETVAL == NULL) && !allowCreate) || RETVAL->IsRestored());
        POSTCONDITION(((RETVAL == NULL) && !allowCreate) || forceBoxedEntryPoint || !RETVAL->IsUnboxingStub());
        POSTCONDITION(((RETVAL == NULL) && !allowCreate) || allowInstParam || !RETVAL->RequiresInstArg());
    }
    CONTRACT_END;

    // Quick exit for the common cases where the result is the same as the primary MD we are given
    if (!pDefMD->HasClassOrMethodInstantiation() &&
        methodInst.IsEmpty() &&
        !forceBoxedEntryPoint &&
        !pDefMD->IsUnboxingStub())
    {
        // Make sure that pDefMD->GetMethodTable() and pExactMT are related types even
        // if we took the fast path.
        _ASSERTE(pDefMD->IsArray() || pDefMD->GetExactDeclaringType(pExactMT) != NULL);

        RETURN pDefMD;
    }

    // Get the version of the method desc. for the instantiated shared class, e.g.
    //  e.g. if pDefMD == List<T>.m()
    //          pExactMT = List<string>
    //     then pMDescInCanonMT = List<object>.m()
    // or
    //  e.g. if pDefMD == List<T>.m<U>()
    //          pExactMT = List<string>
    //     then pMDescInCanonMT = List<object>.m<U>()

    MethodDesc * pMDescInCanonMT = pDefMD;

    // Some callers pass a pExactMT that is a subtype of a parent type of pDefMD.
    // Find the actual exact parent of pDefMD.
    pExactMT = pDefMD->GetExactDeclaringType(pExactMT);
    if (pExactMT == NULL)
    {
        _ASSERTE(false);
        COMPlusThrowHR(COR_E_TYPELOAD);
    }

    if (pDefMD->HasClassOrMethodInstantiation() || !methodInst.IsEmpty())
    {
        // General checks related to generics: arity (if any) must match and generic method
        // instantiation (if any) must be well-formed.
        if (pDefMD->GetNumGenericMethodArgs() != methodInst.GetNumArgs() ||
            !Generics::CheckInstantiation(methodInst))
        {
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }

        pMDescInCanonMT = pExactMT->GetCanonicalMethodTable()->GetParallelMethodDesc(pDefMD);

        if (!allowCreate && (!pMDescInCanonMT->IsRestored() ||
                              !pMDescInCanonMT->GetMethodTable()->IsFullyLoaded()))

        {
            RETURN(NULL);
        }

        pMDescInCanonMT->CheckRestore(level);
    }

    // This case covers nearly all "normal" (i.e. non-associate) MethodDescs.  Just return
    // the MethodDesc in the canonical method table.
    //
    // Also, it will be taken for methods which acquire their type context from the "this" parameter
    // - we don't need instantiating stubs for these.
    if (    methodInst.IsEmpty()
        && (allowInstParam || !pMDescInCanonMT->RequiresInstArg())
        && (forceBoxedEntryPoint == pMDescInCanonMT->IsUnboxingStub())
        && (!forceRemotableMethod || !pMDescInCanonMT->IsInterface()
                || !pMDescInCanonMT->GetMethodTable()->IsSharedByGenericInstantiations()) )
    {
        RETURN(pMDescInCanonMT);
    }

    // Unboxing stubs
    else if (forceBoxedEntryPoint)
    {

        // This assert isn't quite right, for example the repro from NDPWhidbey 18737
        // fires it, because we fetch an BoxedEntryPointStub for a virtual method on a struct
        // when the uninstantiated MethodDesc for the generic virtual method actually
        // qualifies as an BoxedEntryPointStub...  Hence we weaken the assert a little.
        //
        // _ASSERTE(!pDefMD->IsUnboxingStub());
        //        _ASSERTE(pDefMD->IsGenericMethodDefinition() || !pDefMD->IsUnboxingStub());

        //Unboxing stubs for non-generic methods and generic methods are
        // subtly different... For non-generic methods we can look in the
        // shared vtable, and then go to the hash table only if needed.
        // Furthermore even if we have to go to hash table we still base
        // the BoxedEntryPointStub on an unerlying _shared_ method descriptor.
        //
        // For generic methods we must build an BoxedEntryPointStub that calls an
        // underlying instantiating stub.  The underlying instantiating stub
        // will be an _exact_ method descriptor.
        MethodDesc *pResultMD;
        if (methodInst.IsEmpty())
        {
            // First search for the unboxing MD in the shared vtable for the value type
            pResultMD = FindTightlyBoundUnboxingStub(pMDescInCanonMT);

            // Verify that we get the same result by alternative method. There is a possibility
            // that there is no associated unboxing stub, and FindTightlyBoundUnboxingStub takes
            // this into account but the _DEBUG version does not, so only use it if the method
            // returned is actually different.
            _ASSERTE(pResultMD == pMDescInCanonMT ||
                     pResultMD == FindTightlyBoundUnboxingStub_DEBUG(pMDescInCanonMT));

            if (pResultMD != NULL)
            {
                _ASSERTE(pResultMD->IsRestored() && pResultMD->GetMethodTable()->IsFullyLoaded());
                g_IBCLogger.LogMethodDescAccess(pResultMD);
                RETURN(pResultMD);
            }

            MethodTable *pRepMT = pMDescInCanonMT->GetMethodTable();
            mdMethodDef methodDef = pDefMD->GetMemberDef();

            Module *pLoaderModule = ClassLoader::ComputeLoaderModule(pRepMT, methodDef, methodInst);
            LoaderAllocator* pAllocator=pLoaderModule->GetLoaderAllocator();

            InstMethodHashTable* pTable = pLoaderModule->GetInstMethodHashTable();
            // If we didn't find it there then go to the hash table
            pResultMD = pTable->FindMethodDesc(TypeHandle(pRepMT),
                                               methodDef,
                                               TRUE /* forceBoxedEntryPoint */,
                                               Instantiation(),
                                               FALSE /* no inst param */);

            // If we didn't find it then create it...
            if (!pResultMD)
            {
                // !allowCreate ==> GC_NOTRIGGER ==> no entering Crst
                if (!allowCreate)
                {
                    RETURN(NULL);
                }

                CrstHolder ch(&pLoaderModule->m_InstMethodHashTableCrst);

                // Check whether another thread beat us to it!
                pResultMD = pTable->FindMethodDesc(TypeHandle(pRepMT),
                                                   methodDef,
                                                   TRUE,
                                                   Instantiation(),
                                                   FALSE);
                if (pResultMD == NULL)
                {
                    IBCLoggerAwareAllocMemTracker amt;

                    pResultMD = CreateMethodDesc(pAllocator,
                                                 pRepMT,
                                                 pMDescInCanonMT,
                                                 mcInstantiated,
                                                 FALSE /* fNativeCodeSlot */,
                                                 FALSE /* fComPlusCallInfo */,
                                                 &amt);

                    // Indicate that this is a stub method which takes a BOXed this pointer.
                    // An BoxedEntryPointStub may still be an InstantiatedMethodDesc
                    pResultMD->SetIsUnboxingStub();
                    pResultMD->AsInstantiatedMethodDesc()->SetupWrapperStubWithInstantiations(pMDescInCanonMT, NULL, NULL);

                    pResultMD->SetTemporaryEntryPoint(pAllocator, &amt);

                    amt.SuppressRelease();

                    // Verify that we are not creating redundant MethodDescs
                    _ASSERTE(!pResultMD->IsTightlyBoundToMethodTable());

                    // Add it to the table
                    pTable->InsertMethodDesc(pResultMD);
                }

                // CrstHolder goes out of scope here
            }

        }
        else
        {
            mdMethodDef methodDef = pDefMD->GetMemberDef();

            Module *pLoaderModule = ClassLoader::ComputeLoaderModule(pExactMT, methodDef, methodInst);
            LoaderAllocator* pAllocator = pLoaderModule->GetLoaderAllocator();

            InstMethodHashTable* pTable = pLoaderModule->GetInstMethodHashTable();
            // First check the hash table...
            pResultMD = pTable->FindMethodDesc(TypeHandle(pExactMT),
                                               methodDef,
                                               TRUE, /* forceBoxedEntryPoint */
                                               methodInst,
                                               FALSE /* no inst param */);

            if (!pResultMD)
            {
                // !allowCreate ==> GC_NOTRIGGER ==> no entering Crst
                if (!allowCreate)
                {
                    RETURN(NULL);
                }

                // Enter the critical section *after* we've found or created the non-unboxing instantiating stub (else we'd have a race)
                CrstHolder ch(&pLoaderModule->m_InstMethodHashTableCrst);

                // Check whether another thread beat us to it!
                pResultMD = pTable->FindMethodDesc(TypeHandle(pExactMT),
                                                   methodDef,
                                                   TRUE, /* forceBoxedEntryPoint */
                                                   methodInst,
                                                   FALSE /* no inst param */);

                if (pResultMD == NULL)
                {
                    // Recursively get the non-unboxing instantiating stub.  Thus we chain an unboxing
                    // stub with an instantiating stub.
                    MethodDesc* pNonUnboxingStub=
                        MethodDesc::FindOrCreateAssociatedMethodDesc(pDefMD,
                                                                     pExactMT,
                                                                     FALSE /* not Unboxing */,
                                                                     methodInst,
                                                                     FALSE);

                    _ASSERTE(pNonUnboxingStub->GetClassification() == mcInstantiated);
                    _ASSERTE(!pNonUnboxingStub->RequiresInstArg());
                    _ASSERTE(!pNonUnboxingStub->IsUnboxingStub());

                    IBCLoggerAwareAllocMemTracker amt;

                    _ASSERTE(pDefMD->GetClassification() == mcInstantiated);

                    pResultMD = CreateMethodDesc(pAllocator,
                                                 pExactMT,
                                                 pNonUnboxingStub,
                                                 mcInstantiated,
                                                 FALSE /* fNativeCodeSlot */,
                                                 FALSE /* fComPlusCallInfo */,
                                                 &amt);

                    pResultMD->SetIsUnboxingStub();
                    pResultMD->AsInstantiatedMethodDesc()->SetupWrapperStubWithInstantiations(pNonUnboxingStub,
                                                                                              pNonUnboxingStub->GetNumGenericMethodArgs(),
                                                                                              (TypeHandle *)pNonUnboxingStub->GetMethodInstantiation().GetRawArgs());

                    pResultMD->SetTemporaryEntryPoint(pAllocator, &amt);

                    amt.SuppressRelease();

                    // Verify that we are not creating redundant MethodDescs
                    _ASSERTE(!pResultMD->IsTightlyBoundToMethodTable());

                    pTable->InsertMethodDesc(pResultMD);
                }

                // CrstHolder goes out of scope here
            }
        }
        _ASSERTE(pResultMD);

        if (!allowCreate && (!pResultMD->IsRestored() || !pResultMD->GetMethodTable()->IsFullyLoaded()))
        {
            RETURN(NULL);
        }

        pResultMD->CheckRestore(level);
        _ASSERTE(pResultMD->IsUnboxingStub());
        _ASSERTE(!pResultMD->IsInstantiatingStub());
        RETURN(pResultMD);
    }


    // Now all generic method instantiations and static/shared-struct-instance-method wrappers...
    else
    {
        _ASSERTE(!forceBoxedEntryPoint);

        mdMethodDef methodDef = pDefMD->GetMemberDef();
        Module *pModule = pDefMD->GetModule();

        // Some unboxed entry points are attached to canonical method tables.  This is because
        // we have to fill in vtables and/or dispatch maps at load time,
        // and boxed entry points are created to do this. (note vtables and dispatch maps
        // are only created for canonical instantiations).  These boxed entry points
        // in turn refer to unboxed entry points.

        if (// Check if we're looking for something at the canonical instantiation
            (allowInstParam || pExactMT->IsCanonicalMethodTable()) &&
            // Only value types have BoxedEntryPointStubs in the canonical method table
            pExactMT->IsValueType() &&
            // The only generic methods whose BoxedEntryPointStubs are in the canonical method table
            // are those open MethodDescs at the "typical" isntantiation, e.g.
            // VC<int>.m<T>
            // <NICE> This is probably actually not needed </NICE>
            ClassLoader::IsTypicalInstantiation(pModule, methodDef, methodInst)

            )
        {
            MethodDesc * pResultMD = FindTightlyBoundWrappedMethodDesc(pMDescInCanonMT);

            // Verify that we get the same result by alternative method. There is a possibility
            // that this is not an unboxing stub, and FindTightlyBoundWrappedMethodDesc takes
            // this into account but the _DEBUG version does not, so only use it if the method
            // returned is actually different.
            _ASSERTE(pResultMD == pMDescInCanonMT ||
                     pResultMD == FindTightlyBoundWrappedMethodDesc_DEBUG(pMDescInCanonMT));

            if (pResultMD != NULL)
                            {
                _ASSERTE(pResultMD->IsRestored() && pResultMD->GetMethodTable()->IsFullyLoaded());

                g_IBCLogger.LogMethodDescAccess(pResultMD);

                if (allowInstParam || !pResultMD->RequiresInstArg())
                {
                    RETURN(pResultMD);
                }
            }
        }

        // Are either the generic type arguments or the generic method arguments shared?
        BOOL sharedInst =
            pExactMT->GetCanonicalMethodTable()->IsSharedByGenericInstantiations()
            || ClassLoader::IsSharableInstantiation(methodInst);

        // Is it the "typical" instantiation in the correct type that does not require wrapper?
        if (!sharedInst &&
            pExactMT == pMDescInCanonMT->GetMethodTable() &&
            ClassLoader::IsTypicalInstantiation(pModule, methodDef, methodInst))
        {
            _ASSERTE(!pMDescInCanonMT->IsUnboxingStub());
            RETURN(pMDescInCanonMT);
        }

        // OK, so we now know the thing we're looking for can only be found in the MethodDesc table.

        // If getWrappedCode == true, we are looking for a wrapped MethodDesc

        BOOL getWrappedCode = allowInstParam && sharedInst;
        BOOL getWrappedThenStub = !allowInstParam && sharedInst;

        CQuickBytes qbRepInst;
        TypeHandle *repInst = NULL;
        if (getWrappedCode || getWrappedThenStub)
        {
            // Canonicalize the type arguments.
            DWORD cbAllocaSize = 0;
            if (!ClrSafeInt<DWORD>::multiply(methodInst.GetNumArgs(), sizeof(TypeHandle), cbAllocaSize))
                ThrowHR(COR_E_OVERFLOW);

            repInst = reinterpret_cast<TypeHandle *>(qbRepInst.AllocThrows(cbAllocaSize));

            for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
            {
                repInst[i] = ClassLoader::CanonicalizeGenericArg(methodInst[i]);
            }
        }

        // <NICE> These paths can probably be merged together more nicely, and the lookup-lock-lookup pattern made much
        // more obvious </NICE>
        InstantiatedMethodDesc *pInstMD;
        if (getWrappedCode)
        {
            // Get the underlying shared code using the canonical instantiations
            pInstMD =
                InstantiatedMethodDesc::FindLoadedInstantiatedMethodDesc(pExactMT->GetCanonicalMethodTable(),
                                                                         methodDef,
                                                                         Instantiation(repInst, methodInst.GetNumArgs()),
                                                                         TRUE);

            // No - so create one.
            if (pInstMD == NULL)
            {
                if (!allowCreate)
                {
                    RETURN(NULL);
                }

                pInstMD = InstantiatedMethodDesc::NewInstantiatedMethodDesc(pExactMT->GetCanonicalMethodTable(),
                                                                            pMDescInCanonMT,
                                                                            NULL,
                                                                            Instantiation(repInst, methodInst.GetNumArgs()),
                                                                            TRUE);
            }
        }
        else if (getWrappedThenStub)
        {
            // See if we've already got the instantiated method desc for this one.
            pInstMD =
                InstantiatedMethodDesc::FindLoadedInstantiatedMethodDesc(pExactMT,
                                                                         methodDef,
                                                                         methodInst,
                                                                         FALSE);

            // No - so create one.  Go fetch the shared one first
            if (pInstMD == NULL)
            {
                if (!allowCreate)
                {
                    RETURN(NULL);
                }

                // This always returns the shared code.  Repeat the original call except with
                // approximate params and allowInstParam=true
                MethodDesc* pWrappedMD = FindOrCreateAssociatedMethodDesc(pDefMD,
                                                                          pExactMT->GetCanonicalMethodTable(),
                                                                          FALSE,
                                                                          Instantiation(repInst, methodInst.GetNumArgs()),
                                                                          /* allowInstParam */ TRUE,
                                                                          /* forceRemotableMethod */ FALSE,
                                                                          /* allowCreate */ TRUE,
                                                                          /* level */ level);

                _ASSERTE(pWrappedMD->IsSharedByGenericInstantiations());
                _ASSERTE(!methodInst.IsEmpty() || !pWrappedMD->IsSharedByGenericMethodInstantiations());

                pInstMD = InstantiatedMethodDesc::NewInstantiatedMethodDesc(pExactMT,
                                                                            pMDescInCanonMT,
                                                                            pWrappedMD,
                                                                            methodInst,
                                                                            FALSE);
            }
        }
        else
        {
            // See if we've already got the instantiated method desc for this one.
            // If looking for shared code use the representative inst.
            pInstMD =
                InstantiatedMethodDesc::FindLoadedInstantiatedMethodDesc(pExactMT,
                                                                         methodDef,
                                                                         methodInst,
                                                                         FALSE);

            // No - so create one.
            if (pInstMD == NULL)
            {
                if (!allowCreate)
                {
                    RETURN(NULL);
                }

                pInstMD = InstantiatedMethodDesc::NewInstantiatedMethodDesc(pExactMT,
                                                                            pMDescInCanonMT,
                                                                            NULL,
                                                                            methodInst,
                                                                            FALSE);
            }
        }
        _ASSERTE(pInstMD);

        if (!allowCreate && (!pInstMD->IsRestored() || !pInstMD->GetMethodTable()->IsFullyLoaded()))
        {
            RETURN(NULL);
        }

        pInstMD->CheckRestore(level);

        RETURN(pInstMD);
    }
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

// Normalize the methoddesc for reflection
/*static*/ MethodDesc* MethodDesc::FindOrCreateAssociatedMethodDescForReflection(
    MethodDesc *pMethod,
    TypeHandle instType,
    Instantiation methodInst)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;    // Because allowCreate is TRUE
        PRECONDITION(CheckPointer(pMethod));
    }
    CONTRACTL_END;

    MethodDesc *pInstMD = pMethod;

    // no stubs for TypeDesc
    if (instType.IsTypeDesc())
        return pInstMD;

    MethodTable* pMT = instType.AsMethodTable();

    if (!methodInst.IsEmpty())
    {
        // method.BindGenericParameters() was called and we need to retrieve an instantiating stub

        // pMethod is not necessarily a generic method definition, ResolveMethod could pass in an
        // instantiated generic method.
        _ASSERTE(pMethod->HasMethodInstantiation());

        if (methodInst.GetNumArgs() != pMethod->GetNumGenericMethodArgs())
            COMPlusThrow(kArgumentException);

        // we base the creation of an unboxing stub on whether the original method was one already
        // that keeps the reflection logic the same for value types
        pInstMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
            pMethod,
            pMT,
            pMethod->IsUnboxingStub(),
            methodInst,
            FALSE,      /* no allowInstParam */
            TRUE   /* force remotable method (i.e. inst wrappers for non-generic methods on generic interfaces) */);
    }
    else if ( !pMethod->HasMethodInstantiation() &&
              ( instType.IsValueType() ||
                ( instType.HasInstantiation() &&
                  !instType.IsGenericTypeDefinition() &&
                  ( instType.IsInterface() || pMethod->IsStatic() ) ) ) )
    {
        //
        // Called at MethodInfos cache creation
        //   the method is either a normal method or a generic method definition
        // Also called at MethodBase.GetMethodBaseFromHandle
        //   the method is either a normal method, a generic method definition, or an instantiated generic method
        // Needs an instantiating stub if
        // - non generic static method on a generic class
        // - non generic instance method on a struct
        // - non generic method on a generic interface
        //

        // we base the creation of an unboxing stub on whether the original method was one already
        // that keeps the reflection logic the same for value types

        // we need unboxing stubs for virtual methods on value types unless the method is generic
        BOOL fNeedUnboxingStub = pMethod->IsUnboxingStub() ||
            ( instType.IsValueType() && pMethod->IsVirtual() );

        pInstMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
            pMethod,            /* the original MD          */
            pMT,                /* the method table         */
            fNeedUnboxingStub,  /* create boxing stub       */
            Instantiation(),    /* no generic instantiation */
            FALSE,              /* no allowInstParam        */
            TRUE   /* force remotable method (i.e. inst wrappers for non-generic methods on generic interfaces) */);
    }

    return pInstMD;
}

// Given a typical method desc (i.e. instantiated at formal type
// parameters if it is a generic method or lives in a generic class),
// instantiate any type parameters at <__Canon>
//
// NOTE: If allowCreate is FALSE, typically you must also set ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE()
// allowCreate may be set to FALSE to enforce that the method searched
// should already be in existence - thus preventing creation and GCs during
// inappropriate times.
//
MethodDesc * MethodDesc::FindOrCreateTypicalSharedInstantiation(BOOL allowCreate /* = TRUE */)
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(IsTypicalMethodDefinition());
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->IsTypicalSharedInstantiation());
    }
    CONTRACT_END

    MethodDesc *pMD = this;
    MethodTable *pMT = pMD->GetMethodTable();

    // First instantiate the declaring type at <__Canon,...,__Canon>
    DWORD nGenericClassArgs = pMT->GetNumGenericArgs();
    DWORD dwAllocSize = 0;
    if (!ClrSafeInt<DWORD>::multiply(sizeof(TypeHandle), nGenericClassArgs, dwAllocSize))
        ThrowHR(COR_E_OVERFLOW);

    CQuickBytes qbGenericClassArgs;
    TypeHandle* pGenericClassArgs = reinterpret_cast<TypeHandle*>(qbGenericClassArgs.AllocThrows(dwAllocSize));

    for (DWORD i = 0; i < nGenericClassArgs; i++)
    {
        pGenericClassArgs[i] = TypeHandle(g_pCanonMethodTableClass);
    }

    pMT = ClassLoader::LoadGenericInstantiationThrowing(pMT->GetModule(),
                                                        pMT->GetCl(),
                                                        Instantiation(pGenericClassArgs, nGenericClassArgs),
                                                        allowCreate ? ClassLoader::LoadTypes : ClassLoader::DontLoadTypes
                                                        ).GetMethodTable();

    if (pMT == NULL)
    {
        _ASSERTE(!allowCreate);
        return NULL;
    }

    // Now instantiate the method at <__Canon,...,__Canon>, creating the shared code.
    // This will not create an instantiating stub just yet.
    DWORD nGenericMethodArgs = pMD->GetNumGenericMethodArgs();
    CQuickBytes qbGenericMethodArgs;
    TypeHandle *genericMethodArgs = NULL;

    // The rest of this method instantiates a generic method
    // Instantiate at "__Canon" if a NULL "genericMethodArgs" is given
    if (nGenericMethodArgs)
    {
        dwAllocSize = 0;
        if (!ClrSafeInt<DWORD>::multiply(sizeof(TypeHandle), nGenericMethodArgs, dwAllocSize))
            ThrowHR(COR_E_OVERFLOW);

        genericMethodArgs = reinterpret_cast<TypeHandle*>(qbGenericMethodArgs.AllocThrows(dwAllocSize));

        for (DWORD i =0; i < nGenericMethodArgs; i++)
            genericMethodArgs[i] = TypeHandle(g_pCanonMethodTableClass);
    }

    RETURN(MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                                                        pMT,
                                                        FALSE, /* don't get unboxing entry point */
                                                        Instantiation(genericMethodArgs, nGenericMethodArgs),
                                                        TRUE,
                                                        FALSE,
                                                        allowCreate));
}

//@GENERICSVER: Set the typical (ie. formal) instantiation
void InstantiatedMethodDesc::SetupGenericMethodDefinition(IMDInternalImport *pIMDII,
                                                          LoaderAllocator* pAllocator,
                                                          AllocMemTracker *pamTracker,
                                                          Module *pModule,
                                                          mdMethodDef tok)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pIMDII));
    }
    CONTRACTL_END;

    // The first field is never used
    m_wFlags2 = GenericMethodDefinition | (m_wFlags2 & ~KindMask);

    //@GENERICSVER: allocate space for and initialize the typical instantiation
    //we share the typical instantiation among all instantiations by placing it in the generic method desc
    LOG((LF_JIT, LL_INFO10000, "GENERICSVER: Initializing typical method instantiation with type handles\n"));
    mdGenericParam    tkTyPar;
    HENUMInternalHolder hEnumTyPars(pIMDII);
    hEnumTyPars.EnumInit(mdtGenericParam, tok);

    // Initialize the typical instantiation
    DWORD numTyPars = hEnumTyPars.EnumGetCount();
    if (!FitsIn<WORD>(numTyPars))
    {
        LPCSTR szMethodName;
        if (FAILED(pIMDII->GetNameOfMethodDef(tok, &szMethodName)))
        {
            szMethodName = "Invalid MethodDef record";
        }
        pModule->GetAssembly()->ThrowTypeLoadException(szMethodName, IDS_CLASSLOAD_TOOMANYGENERICARGS);
    }
    m_wNumGenericArgs = static_cast<WORD>(numTyPars);
    _ASSERTE(m_wNumGenericArgs > 0);

    S_SIZE_T dwAllocSize = S_SIZE_T(numTyPars) * S_SIZE_T(sizeof(TypeHandle));

    // the memory allocated for m_pMethInst will be freed if the declaring type fails to load
    m_pPerInstInfo.SetValue((Dictionary *) pamTracker->Track(pAllocator->GetLowFrequencyHeap()->AllocMem(dwAllocSize)));

    TypeHandle * pInstDest = (TypeHandle *) IMD_GetMethodDictionaryNonNull();

    {
        // Protect multi-threaded access to Module.m_GenericParamToDescMap. Other threads may be loading the same type
        // to break type recursion dead-locks

        // m_AvailableTypesLock has to be taken in cooperative mode to avoid deadlocks during GC
        GCX_COOP();
        CrstHolder ch(&pModule->GetClassLoader()->m_AvailableTypesLock);

        for (unsigned int i = 0; i < numTyPars; i++)
        {
            hEnumTyPars.EnumNext(&tkTyPar);

            // code:Module.m_GenericParamToDescMap maps generic parameter RIDs to TypeVarTypeDesc
            // instances so that we do not leak by allocating them all over again, if the declaring
            // type repeatedly fails to load.
            TypeVarTypeDesc* pTypeVarTypeDesc = pModule->LookupGenericParam(tkTyPar);
            if (pTypeVarTypeDesc == NULL)
            {
                // Do NOT use pamTracker for this memory as we need it stay allocated even if the load fails.
                void* mem = (void*)pAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(TypeVarTypeDesc)));
                pTypeVarTypeDesc = new (mem) TypeVarTypeDesc(pModule, tok, i, tkTyPar);

                pModule->StoreGenericParamThrowing(tkTyPar, pTypeVarTypeDesc);
            }
            pInstDest[i] = TypeHandle(pTypeVarTypeDesc);
        }
    }
    LOG((LF_JIT, LL_INFO10000, "GENERICSVER: Initialized typical  method instantiation with %d type handles\n",numTyPars));
}

void InstantiatedMethodDesc::SetupWrapperStubWithInstantiations(MethodDesc* wrappedMD,DWORD numGenericArgs, TypeHandle *pInst)
{
    WRAPPER_NO_CONTRACT;

    //_ASSERTE(sharedMD->IMD_IsSharedByGenericMethodInstantiations());

    m_pWrappedMethodDesc.SetValue(wrappedMD);
    m_wFlags2 = WrapperStubWithInstantiations | (m_wFlags2 & ~KindMask);
    m_pPerInstInfo.SetValueMaybeNull((Dictionary*)pInst);

    _ASSERTE(FitsIn<WORD>(numGenericArgs));
    m_wNumGenericArgs = static_cast<WORD>(numGenericArgs);

    _ASSERTE(IMD_IsWrapperStubWithInstantiations());
    _ASSERTE(((MethodDesc *) this)->IsInstantiatingStub() || ((MethodDesc *) this)->IsUnboxingStub());
}


// Set the instantiation in the per-inst section (this is actually a dictionary)
void InstantiatedMethodDesc::SetupSharedMethodInstantiation(DWORD numGenericArgs, TypeHandle *pPerInstInfo, DictionaryLayout *pDL)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(numGenericArgs != 0);
    // Initially the dictionary layout is empty
    m_wFlags2 = SharedMethodInstantiation | (m_wFlags2 & ~KindMask);
    m_pPerInstInfo.SetValueMaybeNull((Dictionary *)pPerInstInfo);

    _ASSERTE(FitsIn<WORD>(numGenericArgs));
    m_wNumGenericArgs = static_cast<WORD>(numGenericArgs);

    m_pDictLayout.SetValueMaybeNull(pDL);


    _ASSERTE(IMD_IsSharedByGenericMethodInstantiations());
}

// Set the instantiation in the per-inst section (this is actually a dictionary)
void InstantiatedMethodDesc::SetupUnsharedMethodInstantiation(DWORD numGenericArgs, TypeHandle *pInst)
{
    LIMITED_METHOD_CONTRACT;

    // The first field is never used
    m_wFlags2 = UnsharedMethodInstantiation | (m_wFlags2 & ~KindMask);
    m_pPerInstInfo.SetValueMaybeNull((Dictionary *)pInst);

    _ASSERTE(FitsIn<WORD>(numGenericArgs));
    m_wNumGenericArgs = static_cast<WORD>(numGenericArgs);

    _ASSERTE(!IsUnboxingStub());
    _ASSERTE(!IsInstantiatingStub());
    _ASSERTE(!IMD_IsWrapperStubWithInstantiations());
    _ASSERTE(!IMD_IsSharedByGenericMethodInstantiations());
    _ASSERTE(!IMD_IsGenericMethodDefinition());
}


// A type variable is bounded to some depth iff it
// has no chain of type variable bounds of that depth.
// We use this is a simple test for circularity among class and method type parameter constraints:
// the constraints on a set of n variables are well-founded iff every variable is bounded by n.
// The test is cheap for the common case that few, if any, constraints are variables.
BOOL Bounded(TypeVarTypeDesc *tyvar, DWORD depth) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(tyvar));
    } CONTRACTL_END;

    if (depth == 0)
    {
        return FALSE;
    }

    DWORD numConstraints;
    TypeHandle *constraints = tyvar->GetConstraints(&numConstraints, CLASS_DEPENDENCIES_LOADED);
    for (unsigned i = 0; i < numConstraints; i++)
    {
        TypeHandle constraint = constraints[i];
        if (constraint.IsGenericVariable())
        {
            TypeVarTypeDesc* constraintVar = (TypeVarTypeDesc*) constraint.AsTypeDesc();
            //only consider bounds between same sort of variables (VAR or MVAR)
            if (tyvar->GetInternalCorElementType() == constraintVar->GetInternalCorElementType())
            {
                if (!Bounded(constraintVar, depth - 1))
                    return FALSE;
            }
        }
    }
    return TRUE;
}

void MethodDesc::LoadConstraintsForTypicalMethodDefinition(BOOL *pfHasCircularClassConstraints, BOOL *pfHasCircularMethodConstraints, ClassLoadLevel level/* = CLASS_LOADED*/)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(IsTypicalMethodDefinition());
        PRECONDITION(CheckPointer(pfHasCircularClassConstraints));
        PRECONDITION(CheckPointer(pfHasCircularMethodConstraints));
    } CONTRACTL_END;

    *pfHasCircularClassConstraints = FALSE;
    *pfHasCircularMethodConstraints = FALSE;

    // Force a load of the constraints on the type parameters
    Instantiation classInst = GetClassInstantiation();
    for (DWORD i = 0; i < classInst.GetNumArgs(); i++)
    {
        TypeVarTypeDesc* tyvar = classInst[i].AsGenericVariable();
        _ASSERTE(tyvar != NULL);
        tyvar->LoadConstraints(level);
    }

    Instantiation methodInst = GetMethodInstantiation();
    for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
    {
        TypeVarTypeDesc* tyvar = methodInst[i].AsGenericVariable();
        _ASSERTE(tyvar != NULL);
        tyvar->LoadConstraints(level);

        VOID DoAccessibilityCheckForConstraints(MethodTable *pAskingMT, TypeVarTypeDesc *pTyVar, UINT resIDWhy);
        DoAccessibilityCheckForConstraints(GetMethodTable(), tyvar, E_ACCESSDENIED);
    }

    // reject circular class constraints
    for (DWORD i = 0; i < classInst.GetNumArgs(); i++)
    {
        TypeVarTypeDesc* tyvar = classInst[i].AsGenericVariable();
        _ASSERTE(tyvar != NULL);
        if(!Bounded(tyvar, classInst.GetNumArgs()))
        {
            *pfHasCircularClassConstraints = TRUE;
        }
    }

    // reject circular method constraints
    for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
    {
        TypeVarTypeDesc* tyvar = methodInst[i].AsGenericVariable();
        _ASSERTE(tyvar != NULL);
        if(!Bounded(tyvar, methodInst.GetNumArgs()))
        {
            *pfHasCircularMethodConstraints = TRUE;
        }
    }

    return;
}


#ifdef FEATURE_PREJIT

void MethodDesc::PrepopulateDictionary(DataImage * image, BOOL nonExpansive)
{
    STANDARD_VM_CONTRACT;

     // Note the strong similarity to MethodTable::PrepopulateDictionary
     if (GetMethodDictionary())
     {
         LOG((LF_JIT, LL_INFO10000, "GENERICS: Prepopulating dictionary for MD %s\n",  this));
         GetMethodDictionary()->PrepopulateDictionary(this, NULL, nonExpansive);
     }
}

#endif // FEATURE_PREJIT

#ifndef DACCESS_COMPILE

BOOL MethodDesc::SatisfiesMethodConstraints(TypeHandle thParent, BOOL fThrowIfNotSatisfied/* = FALSE*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // nice: cache (positive?) result in (instantiated) methoddesc
    // caveat: this would be unsafe for instantiated method desc living in generic,
    // hence possibly shared classes (with varying class instantiations).

    if (!HasMethodInstantiation())
       return TRUE;

    Instantiation methodInst = LoadMethodInstantiation();
    Instantiation typicalInst = LoadTypicalMethodDefinition()->GetMethodInstantiation();

    //NB: according to the constructor's signature, thParent should be the declaring type,
    // but the code appears to admit derived types too.
    SigTypeContext typeContext(this,thParent);
    InstantiationContext instContext(&typeContext, NULL);

    bool typicalInstMatchesMethodInst = true;
    for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
    {
        if (typicalInst[i] != methodInst[i])
        {
            typicalInstMatchesMethodInst = false;
            break;
        }
    }

    for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
    {
        TypeHandle thArg = methodInst[i];
        _ASSERTE(!thArg.IsNull());

        TypeVarTypeDesc* tyvar = (TypeVarTypeDesc*) (typicalInst[i].AsTypeDesc());
        _ASSERTE(tyvar != NULL);
        _ASSERTE(TypeFromToken(tyvar->GetTypeOrMethodDef()) == mdtMethodDef);

        tyvar->LoadConstraints(); //TODO: is this necessary for anything but the typical method?

        // Pass in the InstatiationContext so contraints can be correctly evaluated
        // if this is an instantiation where the type variable is in its open position
        if (!tyvar->SatisfiesConstraints(&typeContext,thArg, typicalInstMatchesMethodInst ? &instContext : NULL))
        {
            if (fThrowIfNotSatisfied)
            {
                SString sParentName;
                TypeString::AppendType(sParentName, thParent);

                SString sMethodName(SString::Utf8, GetName());

                SString sActualParamName;
                TypeString::AppendType(sActualParamName, methodInst[i]);

                SString sFormalParamName;
                TypeString::AppendType(sFormalParamName, typicalInst[i]);

                COMPlusThrow(kVerificationException,
                             IDS_EE_METHOD_CONSTRAINTS_VIOLATION,
                             sParentName.GetUnicode(),
                             sMethodName.GetUnicode(),
                             sActualParamName.GetUnicode(),
                             sFormalParamName.GetUnicode()
                            );


            }
            return FALSE;
        }

    }
    return TRUE;
}

#endif // !DACCESS_COMPILE
