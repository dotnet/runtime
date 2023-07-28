// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// typectxt.h
//

//

#ifndef _H_TYPECTXT
#define _H_TYPECTXT

//------------------------------------------------------------------------
// A signature type context gives the information necessary to interpret
// the ELEMENT_TYPE_VAR and ELEMENT_TYPE_MVAR elements of a regular
// metadata signature.  These are usually stack allocated at appropriate
// points where the SigPointer objects are created, or are allocated
// inside a MetaSig (which are themselves normally stack allocated)
//
// They are normally passed as "const SigTypeContext *".
//------------------------------------------------------------------------

class SigTypeContext
{
public:
    // Store pointers first and DWORDs second to ensure good packing on 64-bit
    Instantiation m_classInst;
    Instantiation m_methodInst;

    // Default constructor for non-generic code
    inline SigTypeContext()
    { WRAPPER_NO_CONTRACT; InitTypeContext(this); }


    // Initialize a type context given instantiations.
    inline SigTypeContext(Instantiation classInst, Instantiation methodInst)
    { WRAPPER_NO_CONTRACT; InitTypeContext(classInst, methodInst, this); }


    // Initialize a type context from a MethodDesc.  If this is a MethodDesc that gets
    // shared between generic instantiations (e.g. one being jitted by a code-sharing JIT)
    // and a null declaring Type is passed then the type context will
    // be a representative context, not an exact one.
    // This is sufficient for most purposes, e.g. GC and field layout, because
    // these operations are "parameteric", i.e. behave the same for all shared types.
    //
    // If declaringType is non-null, then the MethodDesc is assumed to be
    // shared between generic classes, and the type handle is used to give the
    // exact type context.  The method should be one of the methods supported by the
    // given type handle.
    //
    // If the method is a method in an array type then the type context will
    // contain one item in the class instantiation corresponding to the
    // element type of the array.
    //
    // Finally, exactMethodInst should be specified if md might represent a generic method definition,
    // as type parameters are not always available off the method desc for generic method definitions without
    // forcing a load. Typically the caller will use MethodDesc::LoadMethodInstantiation.
    inline SigTypeContext(MethodDesc *md)
    { WRAPPER_NO_CONTRACT; InitTypeContext(md,this); }

    inline SigTypeContext(MethodDesc *md, TypeHandle declaringType)
    { WRAPPER_NO_CONTRACT; InitTypeContext(md,declaringType,this); }

    inline SigTypeContext(MethodDesc *md, TypeHandle declaringType, Instantiation exactMethodInst)
    { WRAPPER_NO_CONTRACT; InitTypeContext(md,declaringType,exactMethodInst,this); }

    // This is similar to the one above except that exact
    // instantiations are provided explicitly.
    // This will only normally be used when the code is shared
    // between generic instantiations and after fetching the
    // exact instantiations from the stack.
    //
    inline SigTypeContext(MethodDesc *md, Instantiation exactClassInst, Instantiation exactMethodInst)
    { WRAPPER_NO_CONTRACT; InitTypeContext(md,exactClassInst,exactMethodInst,this); }

    // Initialize a type context from a type handle.  This is used when
    // generating the type context for a
    // any of the metadata in the class covered by the type handle apart from
    // the metadata for any generic methods in the class.
    // If the type handle satisfies th.IsNull() then the created type context
    // will be empty.
    inline SigTypeContext(TypeHandle th)
    { WRAPPER_NO_CONTRACT; InitTypeContext(th,this); }

    inline SigTypeContext(FieldDesc *pFD, TypeHandle declaringType = TypeHandle())
    { WRAPPER_NO_CONTRACT; InitTypeContext(pFD,declaringType,this); }

    // Copy constructor - try not to use this.  The C++ compiler is not doing a good job
    // of copy-constructor based code, and we've had perf regressions when using this too
    // much for this simple objects.  Use an explicit call to InitTypeContext instead,
    // or use GetOptionalTypeContext.
    inline SigTypeContext(const SigTypeContext &c)
    { WRAPPER_NO_CONTRACT; InitTypeContext(&c,this); }

    // Copy constructor from a possibly-NULL pointer.
    inline SigTypeContext(const SigTypeContext *c)
    { WRAPPER_NO_CONTRACT; InitTypeContext(c,this); }

    static void InitTypeContext(MethodDesc *md, SigTypeContext *pRes);
    static void InitTypeContext(MethodDesc *md, TypeHandle declaringType, SigTypeContext *pRes);
    static void InitTypeContext(MethodDesc *md, TypeHandle declaringType, Instantiation exactMethodInst, SigTypeContext *pRes);
    static void InitTypeContext(MethodDesc *md, Instantiation exactClassInst, Instantiation exactMethodInst, SigTypeContext *pRes);
    static void InitTypeContext(TypeHandle th, SigTypeContext *pRes);
    static void InitTypeContext(FieldDesc *pFD, TypeHandle declaringType, SigTypeContext *pRes);
    inline static void InitTypeContext(Instantiation classInst, Instantiation methodInst, SigTypeContext *pRes);
    inline static void InitTypeContext(SigTypeContext *);
    inline static void InitTypeContext(const SigTypeContext *c, SigTypeContext *pRes);

    // These are allowed to return NULL if an empty type context is generated.  The NULL value
    // can then be passed around to represent the empty type context.
    // pRes must be non-null.
    // pRes must initially be zero-initialized, e.g. by the default SigTypeContext constructor.
    static const SigTypeContext * GetOptionalTypeContext(MethodDesc *md, TypeHandle declaringType, SigTypeContext *pRes);
    static const SigTypeContext * GetOptionalTypeContext(TypeHandle th, SigTypeContext *pRes);

    // SigTypeContexts are used as part of keys for various data structures indiexed by instantiation
    static BOOL Equal(const SigTypeContext *pCtx1, const SigTypeContext *pCtx2);
    static BOOL IsValidTypeOnlyInstantiationOf(const SigTypeContext *pCtxTypicalMethodInstantiation, const SigTypeContext *pCtxTypeOnlyInstantiation);
    BOOL IsEmpty() const { LIMITED_METHOD_CONTRACT; return m_classInst.IsEmpty() && m_methodInst.IsEmpty(); }

};

inline void SigTypeContext::InitTypeContext(SigTypeContext *pRes)
{
    LIMITED_METHOD_DAC_CONTRACT;
}

inline void SigTypeContext::InitTypeContext(Instantiation classInst,
                                            Instantiation methodInst,
                                            SigTypeContext *pRes)
{
    LIMITED_METHOD_CONTRACT;
    pRes->m_classInst = classInst;
    pRes->m_methodInst = methodInst;
}


// Copy constructor from a possibly-NULL pointer.
inline void SigTypeContext::InitTypeContext(const SigTypeContext *c,SigTypeContext *pRes)
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (c)
    {
        pRes->m_classInst = c->m_classInst;
        pRes->m_methodInst = c->m_methodInst;
    }
    else
    {
        pRes->m_classInst = Instantiation();
        pRes->m_methodInst = Instantiation();
    }
}

//------------------------------------------------------------------------
// Encapsulates pointers to code:SigTypeContext and code:Substitution chain
// that have been used to instantiate a generic type. The context is passed
// as "const InstantiationContext *" from code:SigPointer.GetTypeHandleThrowing
// down to code:TypeVarTypeDesc.SatisfiesConstraints where it is needed for
// instantiating constraints attached to type arguments.
//
// The reason why we need to pass these pointers down to the code that verifies
// that constraints are satisified is the case when another type variable is
// substituted for a type variable and this argument is constrained by a generic
// type. In order to verify that constraints of the argument satisfy constraints
// of the parameter, the argument constraints must be instantiated in the same
// "instantiation context" as the original signature - and unfortunately this
// context cannot be extracted from the rest of the information that we have
// about the type that is being loaded.
//
// See code:TypeVarTypeDesc.SatisfiesConstraints for more details and an
// example scenario in which we are unable to verify constraints without this
// context.
//------------------------------------------------------------------------

class InstantiationContext
{
public:
    const SigTypeContext *m_pArgContext;
    const Substitution *m_pSubstChain;

    inline InstantiationContext(const SigTypeContext *pArgContext = NULL, const Substitution *pSubstChain = NULL)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        m_pArgContext = pArgContext;
        m_pSubstChain = pSubstChain;
    }
};

#endif
