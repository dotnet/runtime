// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// siginfo.hpp
//


#ifndef _H_SIGINFO
#define _H_SIGINFO


#include "util.hpp"
#include "vars.hpp"
#include "clsload.hpp"
#include "sigparser.h"
#include "zapsig.h"
#include "threads.h"

#include "eecontract.h"
#include "typectxt.h"

//---------------------------------------------------------------------------------------
// These macros define how arguments are mapped to the stack in the managed calling convention.
// We assume to be walking a method's signature left-to-right, in the virtual calling convention.
// See MethodDesc::Call for details on this virtual calling convention.
// These macros tell us whether the arguments we see as we proceed with the signature walk are mapped
//   to increasing or decreasing stack addresses. This is valid only for arguments that go on the stack.
//---------------------------------------------------------------------------------------
#if defined(TARGET_X86)
#define STACK_GROWS_DOWN_ON_ARGS_WALK
#else
#define STACK_GROWS_UP_ON_ARGS_WALK
#endif

BOOL IsTypeRefOrDef(LPCSTR szClassName, Module *pModule, mdToken token);

struct ElementTypeInfo {
#ifdef _DEBUG
    int            m_elementType;
#endif
    int            m_cbSize;
    CorInfoGCType  m_gc         : 3;
    int            m_enregister : 1;
};
extern const ElementTypeInfo gElementTypeInfo[];

unsigned GetSizeForCorElementType(CorElementType etyp);
const ElementTypeInfo* GetElementTypeInfo(CorElementType etyp);

class SigBuilder;
class ArgDestination;

typedef const struct HardCodedMetaSig *LPHARDCODEDMETASIG;

//@GENERICS: flags returned from IsPolyType indicating the presence or absence of class and
// method type parameters in a type whose instantiation cannot be determined at JIT-compile time
enum VarKind
{
  hasNoVars = 0x0000,
  hasClassVar = 0x0001,
  hasMethodVar = 0x0002,
  hasSharableClassVar = 0x0004,
  hasSharableMethodVar = 0x0008,
  hasAnyVarsMask = 0x0003,
  hasSharableVarsMask = 0x000c
};

//---------------------------------------------------------------------------------------

struct ScanContext;
typedef void promote_func(PTR_PTR_Object, ScanContext*, uint32_t);
typedef void promote_carefully_func(promote_func*, PTR_PTR_Object, ScanContext*, uint32_t);

void PromoteCarefully(promote_func   fn,
                      PTR_PTR_Object obj,
                      ScanContext*   sc,
                      uint32_t       flags = GC_CALL_INTERIOR);

class LoaderAllocator;
void GcReportLoaderAllocator(promote_func* fn, ScanContext* sc, LoaderAllocator *pLoaderAllocator);

//---------------------------------------------------------------------------------------
//
// Encapsulates how compressed integers and typeref tokens are encoded into
// a bytestream.
//
// As you use this class please understand the implicit normalizations
// on the CorElementType's returned by the various methods, especially
// for variable types (e.g. !0 in generic signatures), string types
// (i.e. E_T_STRING), object types (E_T_OBJECT), constructed types
// (e.g. List<int>) and enums.
//
class SigPointer : public SigParser
{
    friend class MetaSig;

public:
    // Constructor.
    SigPointer() { LIMITED_METHOD_DAC_CONTRACT; }

    // Copy constructor.
    SigPointer(const SigPointer & sig) : SigParser(sig)
    {
        WRAPPER_NO_CONTRACT;
    }

    SigPointer(const SigParser & sig) : SigParser(sig)
    {
        WRAPPER_NO_CONTRACT;
    }

    // Signature from a pointer. INSECURE!!!
    // WARNING: Should not be used as it is insecure, because we do not have size of the signature and
    // therefore we can read behind the end of buffer/file.
    FORCEINLINE
    SigPointer(PCCOR_SIGNATURE ptr) : SigParser(ptr)
    {
        WRAPPER_NO_CONTRACT;
    }

    // Signature from a pointer and size.
    FORCEINLINE
    SigPointer(PCCOR_SIGNATURE ptr, DWORD len) : SigParser(ptr, len)
    {
        WRAPPER_NO_CONTRACT;
    }


    //=========================================================================
    // The RAW interface for reading signatures.  You see exactly the signature,
    // apart from custom modifiers which for historical reasons tend to get eaten.
    //
    // DO NOT USE THESE METHODS UNLESS YOU'RE TOTALLY SURE YOU WANT
    // THE RAW signature.  You nearly always want GetElemTypeClosed() or
    // PeekElemTypeClosed() or one of the MetaSig functions.  See the notes above.
    // These functions will return E_T_INTERNAL, E_T_VAR, E_T_MVAR and such
    // so the caller must be able to deal with those
    //=========================================================================


        void ConvertToInternalExactlyOne(Module* pSigModule, SigTypeContext *pTypeContext, SigBuilder * pSigBuilder, BOOL bSkipCustomModifier = TRUE);
        void ConvertToInternalSignature(Module* pSigModule, SigTypeContext *pTypeContext, SigBuilder * pSigBuilder, BOOL bSkipCustomModifier = TRUE);


    //=========================================================================
    // The CLOSED interface for reading signatures.  With the following
    // methods you see the signature "as if" all type variables are
    // replaced by the given instantiations.  However, no type loads happen.
    //
    // In general this is what you want to use if the signature may include
    // generic type variables.  Even if you know it doesn't you can always
    // pass in NULL for the instantiations and put a comment to that effect.
    //
    // The CLOSED api also hides E_T_INTERNAL by return E_T_CLASS or E_T_VALUETYPE
    // appropriately (as directed by the TypeHandle following E_T_INTERNAL)
    //=========================================================================

        // The CorElementTypes returned correspond
        // to those returned by TypeHandle::GetSignatureCorElementType.
        CorElementType PeekElemTypeClosed(Module *pModule, const SigTypeContext *pTypeContext) const;

        //------------------------------------------------------------------------
        // Fetch the token for a CLASS, VALUETYPE or GENRICINST, or a type
        // variable instantiatied to be one of these, taking into account
        // the given instantiations.
        //
        // SigPointer should be in a position that satisfies
        //  ptr.PeekElemTypeClosed(pTypeContext) = ELEMENT_TYPE_VALUETYPE
        //
        // A type ref or def is returned.  For an instantiated generic struct
        // this will return the token for the generic class, e.g. for a signature
        // for "struct Pair<int,int>" this will return a token for "Pair".
        //
        // The token will only make sense in the context of the module where
        // the signature occurs.
        //
        // WARNING: This api will return a mdTokenNil for a E_T_VALUETYPE obtained
        //          from a E_T_INTERNAL, as the token is meaningless in that case
        //          Users of this api must be prepared to deal with a null token
        //------------------------------------------------------------------------
        mdTypeRef PeekValueTypeTokenClosed(Module *pModule, const SigTypeContext *pTypeContext, Module **ppModuleOfToken) const;


    //=========================================================================
    // The INTERNAL-NORMALIZED interface for reading signatures.  You see
    // information concerning the signature, but taking into account normalizations
    // performed for layout of data, e.g. enums and one-field VCs.
    //=========================================================================

        // The CorElementTypes returned correspond
        // to those returned by TypeHandle::GetInternalCorElementType.
        CorElementType PeekElemTypeNormalized(Module* pModule, const SigTypeContext *pTypeContext, TypeHandle * pthValueType = NULL) const;

        //------------------------------------------------------------------------
        // Assumes that the SigPointer points to the start of an element type.
        // Returns size of that element in bytes. This is the minimum size that a
        // field of this type would occupy inside an object.
        //------------------------------------------------------------------------
        UINT SizeOf(Module* pModule, const SigTypeContext *pTypeContext) const;

private:

        // SigPointer should be just after E_T_VAR or E_T_MVAR
        TypeHandle GetTypeVariable(CorElementType et,const SigTypeContext *pTypeContext);
        TypeHandle GetTypeVariableThrowing(Module *pModule,
                                           CorElementType et,
                                           ClassLoader::LoadTypesFlag fLoadTypes,
                                           const SigTypeContext *pTypeContext);

        // Parse type following E_T_GENERICINST
        TypeHandle GetGenericInstType(Module *        pModule,
                                      ClassLoader::LoadTypesFlag = ClassLoader::LoadTypes,
                                      ClassLoadLevel level = CLASS_LOADED,
                                      const ZapSig::Context *pZapSigContext = NULL);

public:

        //------------------------------------------------------------------------
        // Assuming that the SigPointer points the start if an element type.
        // Use SigTypeContext to fill in any  type parameters
        //
        // Also advance the pointer to after the element type.
        //------------------------------------------------------------------------

        // OBSOLETE - Use GetTypeHandleThrowing()
        TypeHandle GetTypeHandleNT(Module* pModule,
                                   const SigTypeContext *pTypeContext) const;

        // pTypeContext indicates how to instantiate any generic type parameters we come
        // However, first we implicitly apply the substitution pSubst to the metadata if pSubst is supplied.
        // That is, if the metadata contains a type variable "!0" then we first look up
        // !0 in pSubst to produce another item of metdata and continue processing.
        // If pSubst is empty then we look up !0 in the pTypeContext to produce a final
        // type handle.  If any of these are out of range we throw an exception.
        //
        // The level is the level to which the result type will be loaded (see classloadlevel.h)
        // If dropGenericArgumentLevel is TRUE, and the metadata represents an instantiated generic type,
        // then generic arguments to the generic type will be loaded one level lower. (This is used by the
        // class loader to avoid looping on definitions such as class C : D<C>)
        //
        // If dropGenericArgumentLevel is TRUE and
        // level=CLASS_LOAD_APPROXPARENTS, then the instantiated
        // generic type is "approximated" in the following way:
        // - for generic interfaces, the generic type (uninstantiated) is returned
        // - for other generic instantiations, System.Object is used in place of any reference types
        //   occurring in the type arguments
        // This semantics is used by the class loader to load tricky recursive definitions in phases
        // (e.g. class C : D<C>, or struct S : I<S>)
        TypeHandle GetTypeHandleThrowing(Module* pModule,
                                         const SigTypeContext *pTypeContext,
                                         ClassLoader::LoadTypesFlag fLoadTypes = ClassLoader::LoadTypes,
                                         ClassLoadLevel level = CLASS_LOADED,
                                         BOOL dropGenericArgumentLevel = FALSE,
                                         const Substitution *pSubst = NULL,
                                         const ZapSig::Context *pZapSigContext = NULL) const;

public:
        //------------------------------------------------------------------------
        // Does this type contain class or method type parameters whose instantiation cannot
        // be determined at JIT-compile time from the instantiations in the method context?
        // Return a combination of hasClassVar and hasMethodVar flags.
        //
        // Example: class C<A,B> containing instance method m<T,U>
        // Suppose that the method context is C<float,string>::m<double,object>
        // Then the type Dict<!0,!!0> is considered to have *no* "polymorphic" type parameters because
        // !0 is known to be float and !!0 is known to be double
        // But Dict<!1,!!1> has polymorphic class *and* method type parameters because both
        // !1=string and !!1=object are reference types and so code using these can be shared with
        // other reference instantiations.
        //------------------------------------------------------------------------
        VarKind IsPolyType(const SigTypeContext *pTypeContext) const;

        //------------------------------------------------------------------------
        // Tests if the element type is a System.String. Accepts
        // either ELEMENT_TYPE_STRING or ELEMENT_TYPE_CLASS encoding.
        //------------------------------------------------------------------------
        BOOL IsStringType(Module* pModule, const SigTypeContext *pTypeContext) const;
        BOOL IsStringTypeThrowing(Module* pModule, const SigTypeContext *pTypeContext) const;

private:
        BOOL IsStringTypeHelper(Module* pModule, const SigTypeContext* pTypeContext, BOOL fThrow) const;

public:


        //------------------------------------------------------------------------
        // Tests if the element class name is szClassName.
        //------------------------------------------------------------------------
        BOOL IsClass(Module* pModule, LPCUTF8 szClassName, const SigTypeContext *pTypeContext = NULL) const;
        BOOL IsClassThrowing(Module* pModule, LPCUTF8 szClassName, const SigTypeContext *pTypeContext = NULL) const;

private:
        BOOL IsClassHelper(Module* pModule, LPCUTF8 szClassName, const SigTypeContext* pTypeContext, BOOL fThrow) const;

public:
        //------------------------------------------------------------------------
        // Tests for the existence of a custom modifier
        //------------------------------------------------------------------------
        BOOL HasCustomModifier(Module *pModule, LPCSTR szModName, CorElementType cmodtype) const;

        //------------------------------------------------------------------------
        // Tests for ELEMENT_TYPE_CLASS or ELEMENT_TYPE_VALUETYPE followed by a TypeDef,
        // and returns the TypeDef
        //------------------------------------------------------------------------
        BOOL IsTypeDef(mdTypeDef* pTypeDef) const;

};  // class SigPointer

// forward declarations needed for the friends declared in Signature
struct FrameInfo;
struct VASigCookie;
#if defined(DACCESS_COMPILE)
class  DacDbiInterfaceImpl;
#endif // DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Currently, PCCOR_SIGNATURE is used all over the runtime to represent a signature, which is just
// an array of bytes.  The problem with PCCOR_SIGNATURE is that it doesn't tell you the length of
// the signature (i.e. the number of bytes in the array).  This is particularly troublesome for DAC,
// which needs to know how much memory to grab from out of process.  This class is an encapsulation
// over PCCOR_SIGNATURE AND the length of the signature it points to.
//
// Notes:
//    This class is meant to be read-only.  Moreover, preferrably we should never read the raw
//    PCCOR_SIGNATURE pointer directly, but there are likely some cases where it is inevitable.
//    We should keep these to a minimum.
//
//    We should move over to Signature instead of PCCOR_SIGNATURE.
//
//    To get a Signature, you can create one yourself by using a constructor.  However, it's recommended
//    that you check whether the Signature should be constructed at a lower level.  For example, instead of
//    creating a Signature in FramedMethodFrame::PromoteCallerStackWalker(), we should add a member function
//    to MethodDesc to return a Signature.
//

class Signature
{
public:
    // create an empty Signature
    Signature();

    // this is the primary constructor
    Signature(PCCOR_SIGNATURE pSig,
              DWORD           cbSig);

    // check whether the signature is empty, i.e. have a NULL PCCOR_SIGNATURE
    BOOL IsEmpty() const;

    // create a SigParser from the signature
    SigParser CreateSigParser() const;

    // create a SigPointer from the signature
    SigPointer CreateSigPointer() const;

    // pretty print the signature
    void PrettyPrint(const CHAR *        pszMethodName,
                     CQuickBytes *       pqbOut,
                     IMDInternalImport * pIMDI) const;

    // retrieve the raw PCCOR_SIGNATURE pointer
    PCCOR_SIGNATURE GetRawSig() const;

    // retrieve the length of the signature
    DWORD           GetRawSigLen() const;

private:
    PCCOR_SIGNATURE m_pSig;
    DWORD           m_cbSig;
};  // class Signature


#ifdef _DEBUG
#define MAX_CACHED_SIG_SIZE     3       // To excercize non-cached code path
#else
#define MAX_CACHED_SIG_SIZE     15
#endif


//---------------------------------------------------------------------------------------
//
// A substitution represents the composition of several formal type instantiations
// It is used when matching formal signatures across the inheritance hierarchy.
//
// It has the form of a linked list:
//   [mod_1, <inst_1>] ->
//   [mod_2, <inst_2>] ->
//   ...
//   [mod_n, <inst_n>]
//
// Here the types in <inst_1> must be resolved in the scope of module mod_1 but
// may contain type variables instantiated by <inst_2>
// ...
// and the types in <inst_(n-1)> must be resolved in the scope of mould mod_(n-1) but
// may contain type variables instantiated by <inst_n>
//
// Any type variables in <inst_n> are treated as "free".
//
class Substitution
{
private:
    Module *             m_pModule; // Module in which instantiation lives (needed to resolve typerefs)
    SigPointer           m_sigInst;
    const Substitution * m_pNext;

public:
    Substitution()
    {
        LIMITED_METHOD_CONTRACT;
        m_pModule = NULL;
        m_pNext = NULL;
    }

    Substitution(
        Module *             pModuleArg,
        const SigPointer &   sigInst,
        const Substitution * pNextSubstitution)
    {
        LIMITED_METHOD_CONTRACT;
        m_pModule = pModuleArg;
        m_sigInst = sigInst;
        m_pNext = pNextSubstitution;
    }

    Substitution(
        mdToken              parentTypeDefOrRefOrSpec,
        Module *             pModuleArg,
        const Substitution * nextArg);

    Substitution(const Substitution & subst)
    {
        LIMITED_METHOD_CONTRACT;
        m_pModule = subst.m_pModule;
        m_sigInst = subst.m_sigInst;
        m_pNext = subst.m_pNext;
    }
    void DeleteChain();

    Module * GetModule() const { LIMITED_METHOD_DAC_CONTRACT; return m_pModule; }
    const Substitution * GetNext() const { LIMITED_METHOD_DAC_CONTRACT; return m_pNext; }
    const SigPointer & GetInst() const { LIMITED_METHOD_DAC_CONTRACT; return m_sigInst; }
    DWORD GetLength() const;

    void CopyToArray(Substitution * pTarget /* must have type Substitution[GetLength()] */ ) const;

};  // class Substitution

//---------------------------------------------------------------------------------------
//
// Linked list that records what tokens are currently being compared for equivalence. This prevents
// infinite recursion when types refer to each other in a cycle, e.g. a delegate that takes itself as
// a parameter or a struct that declares a field of itself (illegal but we don't know at this point).
//
class TokenPairList
{
public:
    // Chain using this constructor when comparing two typedefs for equivalence.
    TokenPairList(mdToken token1, Module *pModule1, mdToken token2, Module *pModule2, TokenPairList *pNext)
        : m_token1(token1), m_token2(token2),
          m_pModule1(pModule1), m_pModule2(pModule2),
          m_bInTypeEquivalenceForbiddenScope(pNext == NULL ? FALSE : pNext->m_bInTypeEquivalenceForbiddenScope),
          m_pNext(pNext)
    { LIMITED_METHOD_CONTRACT; }

    static BOOL Exists(TokenPairList *pList, mdToken token1, Module *pModule1, mdToken token2, Module *pModule2)
    {
        LIMITED_METHOD_CONTRACT;
        while (pList != NULL)
        {
            if (pList->m_token1 == token1 && pList->m_pModule1 == pModule1 &&
                pList->m_token2 == token2 && pList->m_pModule2 == pModule2)
                return TRUE;

            if (pList->m_token1 == token2 && pList->m_pModule1 == pModule2 &&
                pList->m_token2 == token1 && pList->m_pModule2 == pModule1)
                return TRUE;

            pList = pList->m_pNext;
        }
        return FALSE;
    }

    static BOOL InTypeEquivalenceForbiddenScope(TokenPairList *pList)
    {
        return (pList == NULL ? FALSE : pList->m_bInTypeEquivalenceForbiddenScope);
    }

    // Chain using this method when comparing type specs.
    static TokenPairList AdjustForTypeSpec(TokenPairList *pTemplate, Module *pTypeSpecModule, PCCOR_SIGNATURE pTypeSpecSig, DWORD cbTypeSpecSig);
    static TokenPairList AdjustForTypeEquivalenceForbiddenScope(TokenPairList *pTemplate);

private:
    TokenPairList(TokenPairList *pTemplate)
        : m_token1(pTemplate ? pTemplate->m_token1 : mdTokenNil),
          m_token2(pTemplate ? pTemplate->m_token2 : mdTokenNil),
          m_pModule1(pTemplate ? pTemplate->m_pModule1 : NULL),
          m_pModule2(pTemplate ? pTemplate->m_pModule2 : NULL),
          m_bInTypeEquivalenceForbiddenScope(pTemplate ? pTemplate->m_bInTypeEquivalenceForbiddenScope : FALSE),
          m_pNext(pTemplate ? pTemplate->m_pNext : NULL)
    { LIMITED_METHOD_CONTRACT; }

    mdToken m_token1, m_token2;
    Module *m_pModule1, *m_pModule2;
    BOOL m_bInTypeEquivalenceForbiddenScope;
    TokenPairList *m_pNext;
};  // class TokenPairList

//---------------------------------------------------------------------------------------
//
class MetaSig
{
    public:
        enum MetaSigKind {
            sigMember,
            sigLocalVars,
            sigField,
            };

        //------------------------------------------------------------------
        // Common init used by other constructors
        //------------------------------------------------------------------
        void Init(PCCOR_SIGNATURE szMetaSig,
                DWORD cbMetaSig,
                Module* pModule,
                const SigTypeContext *pTypeContext,
                MetaSigKind kind = sigMember);

        //------------------------------------------------------------------
        // Constructor. Warning: Does NOT make a copy of szMetaSig.
        //
        // The instantiations are used to fill in type variables on calls
        // to PeekArg, GetReturnType, GetNextArg, GetTypeHandle, GetRetTypeHandle and
        // so on.
        //
        // Please make sure you know what you're doing by leaving classInst and methodInst to default NULL
        // Are you sure the signature cannot contain type parameters (E_T_VAR, E_T_MVAR)?
        //------------------------------------------------------------------
        MetaSig(PCCOR_SIGNATURE szMetaSig,
                DWORD cbMetaSig,
                Module* pModule,
                const SigTypeContext *pTypeContext,
                MetaSigKind kind = sigMember)
        {
            WRAPPER_NO_CONTRACT;
            Init(szMetaSig, cbMetaSig, pModule, pTypeContext, kind);
        }

        // this is just a variation of the previous constructor to ease the transition to Signature
        MetaSig(const Signature &      signature,
                Module               * pModule,
                const SigTypeContext * pTypeContext,
                MetaSigKind            kind = sigMember)
        {
            WRAPPER_NO_CONTRACT;
            Init(signature.GetRawSig(), signature.GetRawSigLen(), pModule, pTypeContext, kind);
        }

        // The following create MetaSigs for parsing the signature of the given method.
        // They are identical except that they give slightly different
        // type contexts.  (Note the type context will only be relevant if we
        // are parsing a method on an array type or on a generic type.)
        // See TypeCtxt.h for more details.
        // If declaringType is omitted then a *representative* instantiation may be obtained from pMD or pFD
        MetaSig(MethodDesc *pMD, TypeHandle declaringType = TypeHandle());
        MetaSig(MethodDesc *pMD, Instantiation classInst, Instantiation methodInst);

        MetaSig(FieldDesc *pFD, TypeHandle declaringType = TypeHandle());

        // Used to avoid touching metadata for mscorlib methods.  Nb. only use for non-generic methods.
        MetaSig(BinderMethodID id);

        MetaSig(LPHARDCODEDMETASIG pwzMetaSig);

        //------------------------------------------------------------------
        // Returns type of current argument index. Returns ELEMENT_TYPE_END
        // if already past end of arguments.
        //------------------------------------------------------------------
        CorElementType PeekArg() const;

        //------------------------------------------------------------------
        // Returns type of current argument index. Returns ELEMENT_TYPE_END
        // if already past end of arguments.
        //------------------------------------------------------------------
        CorElementType PeekArgNormalized(TypeHandle * pthValueType = NULL) const;

        //------------------------------------------------------------------
        // Returns type of current argument, then advances the argument
        // index. Returns ELEMENT_TYPE_END if already past end of arguments.
        // This method updates m_pLastType
        //------------------------------------------------------------------
        CorElementType NextArg();

        //------------------------------------------------------------------
        // Advance the argument index. Can be used with GetArgProps() to
        // to iterate when you do not have a valid type context.
        // This method updates m_pLastType
        //------------------------------------------------------------------
        void SkipArg();

        //------------------------------------------------------------------
        // Returns a read-only SigPointer for the m_pLastType set by one
        // of NextArg() or SkipArg()
        // This allows extracting more information for complex types.
        //------------------------------------------------------------------
        const SigPointer & GetArgProps() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_pLastType;
        }

        //------------------------------------------------------------------
        // Returns a read-only SigPointer for the return type.
        // This allows extracting more information for complex types.
        //------------------------------------------------------------------
        const SigPointer & GetReturnProps() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_pRetType;
        }


        //------------------------------------------------------------------------
        // Returns # of arguments. Does not count the return value.
        // Does not count the "this" argument (which is not reflected om the
        // sig.) 64-bit arguments are counted as one argument.
        //------------------------------------------------------------------------
        UINT NumFixedArgs()
        {
            LIMITED_METHOD_DAC_CONTRACT;
            return m_nArgs;
        }

        //----------------------------------------------------------
        // Returns the calling convention (see IMAGE_CEE_CS_CALLCONV_*
        // defines in cor.h) - throws.
        //----------------------------------------------------------
        static BYTE GetCallingConvention(
            Module          *pModule,
            const Signature &signature)
        {
            CONTRACTL
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_ANY;
                SUPPORTS_DAC;
            }
            CONTRACTL_END

            PCCOR_SIGNATURE pSig = signature.GetRawSig();

            if (signature.GetRawSigLen() < 1)
            {
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }
            return (BYTE)(IMAGE_CEE_CS_CALLCONV_MASK & CorSigUncompressCallingConv(/*modifies*/pSig));
        }

        //----------------------------------------------------------
        // Returns the calling convention (see IMAGE_CEE_CS_CALLCONV_*
        // defines in cor.h) - doesn't throw.
        //----------------------------------------------------------
        __checkReturn
        static HRESULT GetCallingConvention_NoThrow(
            Module          *pModule,
            const Signature &signature,
            BYTE            *pbCallingConvention)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                SUPPORTS_DAC;
            }
            CONTRACTL_END

            PCCOR_SIGNATURE pSig = signature.GetRawSig();

            if (signature.GetRawSigLen() < 1)
            {
                *pbCallingConvention = 0;
                return COR_E_BADIMAGEFORMAT;
            }
            *pbCallingConvention = (BYTE)(IMAGE_CEE_CS_CALLCONV_MASK & CorSigUncompressCallingConv(/*modifies*/pSig));
            return S_OK;
        }

        //----------------------------------------------------------
        // Returns the calling convention (see IMAGE_CEE_CS_CALLCONV_*
        // defines in cor.h)
        //----------------------------------------------------------
        BYTE GetCallingConvention()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;
            return m_CallConv & IMAGE_CEE_CS_CALLCONV_MASK;
        }

        //----------------------------------------------------------
        // Returns the calling convention & flags (see IMAGE_CEE_CS_CALLCONV_*
        // defines in cor.h)
        //----------------------------------------------------------
        BYTE GetCallingConventionInfo()
        {
            LIMITED_METHOD_DAC_CONTRACT;

            return m_CallConv;
        }

        //----------------------------------------------------------
        // Has a 'this' pointer?
        //----------------------------------------------------------
        BOOL HasThis()
        {
            LIMITED_METHOD_CONTRACT;

            return m_CallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS;
        }

        //----------------------------------------------------------
        // Has a explicit 'this' pointer?
        //----------------------------------------------------------
        BOOL HasExplicitThis()
        {
            LIMITED_METHOD_CONTRACT;

            return m_CallConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS;
        }

        //----------------------------------------------------------
        // Is a generic method with explicit arity?
        //----------------------------------------------------------
        BOOL IsGenericMethod()
        {
            LIMITED_METHOD_CONTRACT;
            return m_CallConv & IMAGE_CEE_CS_CALLCONV_GENERIC;
        }

        //----------------------------------------------------------
        // Is vararg?
        //----------------------------------------------------------
        BOOL IsVarArg()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;
            return GetCallingConvention() == IMAGE_CEE_CS_CALLCONV_VARARG;
        }

        //----------------------------------------------------------
        // Is vararg?
        //----------------------------------------------------------
        static BOOL IsVarArg(Module *pModule, const Signature &signature)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                SUPPORTS_DAC;
            }
            CONTRACTL_END

            HRESULT hr;
            BYTE    nCallingConvention;

            hr = GetCallingConvention_NoThrow(pModule, signature, &nCallingConvention);
            if (FAILED(hr))
            {   // Invalid signatures are not VarArg
                return FALSE;
            }
            return nCallingConvention == IMAGE_CEE_CS_CALLCONV_VARARG;
        }

        Module* GetModule() const
        {
            LIMITED_METHOD_DAC_CONTRACT;

            return m_pModule;
        }

        //----------------------------------------------------------
        // Gets the unmanaged calling convention by reading any modopts.
        // If there are multiple modopts specifying recognized calling
        // conventions, the first one that is found in the metadata wins.
        // Note: the order in the metadata is the reverse of that in IL.
        //
        // Returns:
        //   E_FAIL - Signature had an invalid format
        //   S_OK - Calling convention was read from modopt
        //   S_FALSE - Calling convention was not read from modopt
        //----------------------------------------------------------
        static HRESULT TryGetUnmanagedCallingConventionFromModOpt(
            _In_ Module *pModule,
            _In_ PCCOR_SIGNATURE pSig,
            _In_ ULONG cSig,
            _Out_ CorUnmanagedCallingConvention *callConvOut);

        static CorUnmanagedCallingConvention GetDefaultUnmanagedCallingConvention()
        {
#ifdef TARGET_UNIX
            return IMAGE_CEE_UNMANAGED_CALLCONV_C;
#else // TARGET_UNIX
            return IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL;
#endif // !TARGET_UNIX
        }

        //------------------------------------------------------------------
        // Like NextArg, but return only normalized type (enums flattned to
        // underlying type ...
        //------------------------------------------------------------------
        CorElementType
        NextArgNormalized(TypeHandle * pthValueType = NULL)
        {
            CONTRACTL
            {
                if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
                if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
                if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
                MODE_ANY;
                SUPPORTS_DAC;
            }
            CONTRACTL_END

            m_pLastType = m_pWalk;
            if (m_iCurArg == m_nArgs)
            {
                return ELEMENT_TYPE_END;
            }
            else
            {
                m_iCurArg++;
                CorElementType mt = m_pWalk.PeekElemTypeNormalized(m_pModule, &m_typeContext, pthValueType);
                // We should not hit ELEMENT_TYPE_END in the middle of the signature
                if (mt == ELEMENT_TYPE_END)
                {
                    THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module *)NULL);
                }
                IfFailThrowBF(m_pWalk.SkipExactlyOne(), BFA_BAD_SIGNATURE, (Module *)NULL);
                return mt;
            }
        } // NextArgNormalized

        // Tests if the return type is an object ref.  Loads types
        // if needed (though it shouldn't really need to)
        BOOL IsObjectRefReturnType();

        //------------------------------------------------------------------------
        // Compute element size from CorElementType and optional valuetype.
        //------------------------------------------------------------------------
        static UINT GetElemSize(CorElementType etype, TypeHandle thValueType);

        UINT GetReturnTypeSize()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;
            return m_pRetType.SizeOf(m_pModule, &m_typeContext);
        }

        //------------------------------------------------------------------
        // Perform type-specific GC promotion on the value (based upon the
        // last type retrieved by NextArg()).
        //------------------------------------------------------------------
        VOID GcScanRoots(ArgDestination *pValue, promote_func *fn,
                         ScanContext* sc, promote_carefully_func *fnc = NULL);

        //------------------------------------------------------------------
        // Is the return type 64 bit?
        //------------------------------------------------------------------
        BOOL Is64BitReturn() const
        {
            WRAPPER_NO_CONTRACT;
            CorElementType rt = GetReturnTypeNormalized();
            return (rt == ELEMENT_TYPE_I8 || rt == ELEMENT_TYPE_U8 || rt == ELEMENT_TYPE_R8);
        }

        //------------------------------------------------------------------
        // Is the return type floating point?
        //------------------------------------------------------------------
        BOOL HasFPReturn()
        {
            WRAPPER_NO_CONTRACT;
            CorElementType rt = GetReturnTypeNormalized();
            return (rt == ELEMENT_TYPE_R4 || rt == ELEMENT_TYPE_R8);
        }

        //------------------------------------------------------------------
        // reset: goto start pos
        //------------------------------------------------------------------
        VOID Reset();

        //------------------------------------------------------------------
        // current position of the arg iterator
        //------------------------------------------------------------------
        UINT GetArgNum()
        {
            LIMITED_METHOD_CONTRACT;
            return m_iCurArg;
        }

        //------------------------------------------------------------------
        // Returns CorElementType of return value, taking into account
        // any instantiations due to generics.  Does not load types.
        // Does not return normalized type.
        //------------------------------------------------------------------
        CorElementType GetReturnType() const;

        BOOL IsReturnTypeVoid() const;

        CorElementType GetReturnTypeNormalized(TypeHandle * pthValueType = NULL) const;

        //------------------------------------------------------------------
        // used to treat some sigs as special case vararg
        // used by calli to unmanaged target
        //------------------------------------------------------------------
        BOOL IsTreatAsVarArg()
        {
            LIMITED_METHOD_DAC_CONTRACT;

            return (m_flags & TREAT_AS_VARARG);
        }

        //------------------------------------------------------------------
        // Determines if the current argument is System/String.
        // Caller must determine first that the argument type is
        // ELEMENT_TYPE_CLASS or ELEMENT_TYPE_STRING.  This may be used during
        // GC.
        //------------------------------------------------------------------
        BOOL IsStringType() const;

        //------------------------------------------------------------------
        // Determines if the current argument is a particular class.
        // Caller must determine first that the argument type
        // is ELEMENT_TYPE_CLASS.
        //------------------------------------------------------------------
        BOOL IsClass(LPCUTF8 szClassName) const;


        //------------------------------------------------------------------
        // This method will return a TypeHandle for the last argument
        // examined.
        // If NextArg() returns ELEMENT_TYPE_BYREF, you can also call GetByRefType()
        // to get to the underlying type of the byref
        //------------------------------------------------------------------
        TypeHandle GetLastTypeHandleNT() const
        {
             WRAPPER_NO_CONTRACT;
             return m_pLastType.GetTypeHandleNT(m_pModule, &m_typeContext);
        }

        //------------------------------------------------------------------
        // This method will return a TypeHandle for the last argument
        // examined.
        // If NextArg() returns ELEMENT_TYPE_BYREF, you can also call GetByRefType()
        // to get to the underlying type of the byref
        //------------------------------------------------------------------
        TypeHandle GetLastTypeHandleThrowing(ClassLoader::LoadTypesFlag fLoadTypes = ClassLoader::LoadTypes,
                                             ClassLoadLevel level = CLASS_LOADED,
                                             BOOL dropGenericArgumentLevel = FALSE) const
        {
             WRAPPER_NO_CONTRACT;
             return m_pLastType.GetTypeHandleThrowing(m_pModule, &m_typeContext, fLoadTypes,
                                                      level, dropGenericArgumentLevel);
        }

        //------------------------------------------------------------------
        // Returns the TypeHandle for the return type of the signature
        //------------------------------------------------------------------
        TypeHandle GetRetTypeHandleNT() const
        {
             WRAPPER_NO_CONTRACT;
             return m_pRetType.GetTypeHandleNT(m_pModule, &m_typeContext);
        }

        TypeHandle GetRetTypeHandleThrowing(ClassLoader::LoadTypesFlag fLoadTypes = ClassLoader::LoadTypes,
                                            ClassLoadLevel level = CLASS_LOADED) const
        {
             WRAPPER_NO_CONTRACT;
             return m_pRetType.GetTypeHandleThrowing(m_pModule, &m_typeContext, fLoadTypes, level);
        }

        //------------------------------------------------------------------
        // Returns the base type of the byref type of the last argument examined
        // which needs to have been ELEMENT_TYPE_BYREF.
        // For object references, the class being accessed byref is also returned in *pTy.
        // eg. for "int32 &",            return value = ELEMENT_TYPE_I4,    *pTy= ???
        //     for "System.Exception &", return value = ELEMENT_TYPE_CLASS, *pTy=System.Exception
        // Note that byref to byref is not allowed, and so the return value
        // can never be ELEMENT_TYPE_BYREF.
        //------------------------------------------------------------------
        CorElementType GetByRefType(TypeHandle* pTy) const;

        //------------------------------------------------------------------
        // Compare types in two signatures, first applying
        // - optional substitutions pSubst1 and pSubst2
        //   to class type parameters (E_T_VAR) in the respective signatures
        //------------------------------------------------------------------
        static BOOL CompareElementType(
            PCCOR_SIGNATURE &    pSig1,
            PCCOR_SIGNATURE &    pSig2,
            PCCOR_SIGNATURE      pEndSig1,
            PCCOR_SIGNATURE      pEndSig2,
            Module *             pModule1,
            Module *             pModule2,
            const Substitution * pSubst1,
            const Substitution * pSubst2,
            TokenPairList *      pVisited = NULL);



        // If pTypeDef1 is C<...> and pTypeDef2 is C<...> (for possibly different instantiations)
        // then check C<!0, ... !n> @ pSubst1 == C<!0, ..., !n> @ pSubst2, i.e.
        // that the head type (C) is the same and that when the head type is treated
        // as an uninstantiated type definition and we apply each of the substitutions
        // then the same type results.  This effectively checks that the two substitutions
        // are equivalent.
        static BOOL CompareTypeDefsUnderSubstitutions(MethodTable *pTypeDef1,          MethodTable *pTypeDef2,
                                                      const Substitution*   pSubst1,   const Substitution*   pSubst2,
                                                      TokenPairList *pVisited = NULL);


        // Compare two complete method signatures, first applying optional substitutions pSubst1 and pSubst2
        // to class type parameters (E_T_VAR) in the respective signatures
        static BOOL CompareMethodSigs(
            PCCOR_SIGNATURE     pSig1,
            DWORD               cSig1,
            Module*             pModule1,
            const Substitution* pSubst1,
            PCCOR_SIGNATURE     pSig2,
            DWORD               cSig2,
            Module*             pModule2,
            const Substitution* pSubst2,
            BOOL                skipReturnTypeSig,
            TokenPairList*      pVisited = NULL
        );

        // Nonthrowing version of CompareMethodSigs
        //
        //   Return S_OK if they match
        //          S_FALSE if they don't match
        //          FAILED  if OOM or some other blocking error
        //
        static HRESULT CompareMethodSigsNT(
            PCCOR_SIGNATURE pSig1,
            DWORD       cSig1,
            Module*     pModule1,
            const Substitution* pSubst1,
            PCCOR_SIGNATURE pSig2,
            DWORD       cSig2,
            Module*     pModule2,
            const Substitution* pSubst2,
            TokenPairList *pVisited = NULL
        );

        static BOOL CompareFieldSigs(
            PCCOR_SIGNATURE pSig1,
            DWORD       cSig1,
            Module*     pModule1,
            PCCOR_SIGNATURE pSig2,
            DWORD       cSig2,
            Module*     pModule2,
            TokenPairList *pVisited = NULL
        );

        static BOOL CompareMethodSigs(MetaSig &msig1,
                                      MetaSig &msig2,
                                      BOOL ignoreCallconv);

        // Is each set of constraints on the implementing method's type parameters a subset
        // of the corresponding set of constraints on the declared method's type parameters,
        // given a subsitution for the latter's (class) type parameters.
        // This is used by the class loader to verify type safety of method overriding and interface implementation.
        static BOOL CompareMethodConstraints(const Substitution *pSubst1,
                                             Module *pModule1,
                                             mdMethodDef tok1, //implementing method
                                             const Substitution *pSubst2,
                                             Module *pModule2,
                                             mdMethodDef tok2); //declared method

private:
        static BOOL CompareVariableConstraints(const Substitution *pSubst1,
                                               Module *pModule1, mdGenericParam tok1, //overriding
                                               const Substitution *pSubst2,
                                               Module *pModule2, mdGenericParam tok2); //overridden

        static BOOL CompareTypeDefOrRefOrSpec(Module *pModule1, mdToken tok1,
                                              const Substitution *pSubst1,
                                              Module *pModule2, mdToken tok2,
                                              const Substitution *pSubst2,
                                              TokenPairList *pVisited);
        static BOOL CompareTypeSpecToToken(mdTypeSpec tk1,
                                           mdToken tk2,
                                           Module *pModule1,
                                           Module *pModule2,
                                           const Substitution *pSubst1,
                                           TokenPairList *pVisited);

        static BOOL CompareElementTypeToToken(PCCOR_SIGNATURE &pSig1,
                                             PCCOR_SIGNATURE pEndSig1, // end of sig1
                                             mdToken         tk2,
                                             Module*         pModule1,
                                             Module*         pModule2,
                                             const Substitution*   pSubst1,
                                             TokenPairList *pVisited);

public:

        //------------------------------------------------------------------
        // Ensures that all the value types in the sig are loaded. This
        // should be called on sig's that have value types before they
        // are passed to Call(). This ensures that value classes will not
        // be loaded during the operation to determine the size of the
        // stack. Thus preventing the resulting GC hole.
        //------------------------------------------------------------------
        static void EnsureSigValueTypesLoaded(MethodDesc *pMD);

        // this walks the sig and checks to see if all  types in the sig can be loaded
        static void CheckSigTypesCanBeLoaded(MethodDesc *pMD);

        const SigTypeContext *GetSigTypeContext() const { LIMITED_METHOD_CONTRACT; return &m_typeContext; }

        // Disallow copy constructor.
        MetaSig(MetaSig *pSig);

        void SetHasParamTypeArg()
        {
            LIMITED_METHOD_CONTRACT;
            m_CallConv |= CORINFO_CALLCONV_PARAMTYPE;
        }

        void SetTreatAsVarArg()
        {
            LIMITED_METHOD_CONTRACT;
            m_flags |= TREAT_AS_VARARG;
        }


    // These are protected because Reflection subclasses Metasig
    protected:

        enum MetaSigFlags
        {
            SIG_RET_TYPE_INITTED    = 0x01,
            TREAT_AS_VARARG         = 0x02,     // used to treat some sigs as special case vararg
                                                // used by calli to unmanaged target
        };

        Module*      m_pModule;
        SigTypeContext m_typeContext;   // Instantiation for type parameters

        SigPointer   m_pStart;
        SigPointer   m_pWalk;
        SigPointer   m_pLastType;
        SigPointer   m_pRetType;
        UINT32       m_nArgs;
        UINT32       m_iCurArg;

        // The following are cached so we don't the signature
        //  multiple times

        CorElementType  m_corNormalizedRetType;
        BYTE            m_flags;
        BYTE            m_CallConv;
};  // class MetaSig


BOOL IsTypeRefOrDef(LPCSTR szClassName, Module *pModule, mdToken token);

#if defined(FEATURE_TYPEEQUIVALENCE) && !defined(DACCESS_COMPILE)

// A helper struct representing data stored in the TypeIdentifierAttribute.
struct TypeIdentifierData
{
    TypeIdentifierData() : m_cbScope(0), m_pchScope(NULL), m_cbIdentifierNamespace(0), m_pchIdentifierNamespace(NULL),
                           m_cbIdentifierName(0), m_pchIdentifierName(NULL) {}

    HRESULT Init(Module *pModule, mdToken tk);
    BOOL IsEqual(const TypeIdentifierData & data) const;

private:
    SIZE_T  m_cbScope;
    LPCUTF8 m_pchScope;
    SIZE_T  m_cbIdentifierNamespace;
    LPCUTF8 m_pchIdentifierNamespace;
    SIZE_T  m_cbIdentifierName;
    LPCUTF8 m_pchIdentifierName;
};

#endif // FEATURE_TYPEEQUIVALENCE && !DACCESS_COMPILE

// fResolved is TRUE when one of the tokens is a resolved TypeRef. This is used to restrict
// type equivalence checks for value types.
BOOL CompareTypeTokens(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2, TokenPairList *pVisited = NULL);

// Nonthrowing version of CompareTypeTokens.
//
//   Return S_OK if they match
//          S_FALSE if they don't match
//          FAILED  if OOM or some other blocking error
//
HRESULT  CompareTypeTokensNT(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2);

// TRUE if the two TypeDefs have the same layout and field marshal information.
BOOL CompareTypeLayout(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2);
BOOL CompareTypeDefsForEquivalence(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2, TokenPairList *pVisited);
BOOL IsTypeDefEquivalent(mdToken tk, Module *pModule);
BOOL IsTypeDefExternallyVisible(mdToken tk, Module *pModule, DWORD dwAttrs);

void ReportPointersFromValueType(promote_func *fn, ScanContext *sc, PTR_MethodTable pMT, PTR_VOID pSrc);

#endif /* _H_SIGINFO */
