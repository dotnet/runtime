// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: memberload.h
//


//

//
// ============================================================================

#ifndef _MEMBERLOAD_H
#define _MEMBERLOAD_H


/*
 *  Include Files
 */
#include "eecontract.h"
#include "argslot.h"
#include "vars.hpp"
#include "cor.h"
#include "clrex.h"
#include "hash.h"
#include "crst.h"
#include "slist.h"
#include "typehandle.h"
#include "methodtable.h"
#include "typectxt.h"

//
// This enum represents the property methods that can be passed to FindPropertyMethod().
//

enum EnumPropertyMethods
{
    PropertyGet = 0,
    PropertySet = 1,
};


//
// This enum represents the event methods that can be passed to FindEventMethod().
//

enum EnumEventMethods
{
    EventAdd = 0,
    EventRemove = 1,
    EventRaise = 2,
};

// The MemberLoader logic is analogous to the ClassLoader logic, i.e. it turn
// tokens into internal EE descriptors.
//
// The implementations of these functions currently lies in class.cpp.
class MemberLoader
{


public:
    static void DECLSPEC_NORETURN ThrowMissingMethodException(MethodTable* pMT,
                                            LPCSTR szMember,
                                            Module *pModule,
                                            PCCOR_SIGNATURE pSig,
                                            DWORD cSig,
                                            const SigTypeContext *pTypeContext);

    static void DECLSPEC_NORETURN ThrowMissingFieldException( MethodTable *pMT,
                                            LPCSTR szMember);

    static  MethodDesc* GetMethodDescFromMemberDefOrRefOrSpec(Module *pModule,
                                                              mdToken MemberRefOrDefOrSpec,
                                                              const SigTypeContext *pTypeContext, // Context for type parameters in any parent TypeSpec and in the instantiation in a MethodSpec
                                                              BOOL strictMetadataChecks,  // Normally true - the zapper is one exception.  Throw an exception if no generic method args given for a generic method, otherwise return the 'generic' instantiation
                                                              BOOL allowInstParam,
                                                              ClassLoadLevel owningTypeLoadLevel = CLASS_LOADED);

    static  FieldDesc* GetFieldDescFromMemberDefOrRef(Module *pModule,
                                                      mdMemberRef MemberDefOrRef,
                                                      const SigTypeContext *pTypeContext,
                                                      BOOL strictMetadataChecks);

    static MethodDesc *GetMethodDescFromMethodDef(Module *pModule,
                                                  mdMethodDef MethodDef,            // MethodDef token
                                                  Instantiation classInst,          // Generic arguments for declaring class
                                                  Instantiation methodInst,         // Generic arguments for declaring method
                                                  BOOL forceRemotable = FALSE);     // force remotable MethodDesc
    //
    // Methods that actually do the work
    //

    static MethodDesc* GetMethodDescFromMethodDef(Module *pModule,
                                                  mdToken MethodDef,
                                                  BOOL strictMetadataChecks,
                                                  ClassLoadLevel owningTypeLoadLevel = CLASS_LOADED);

    static FieldDesc* GetFieldDescFromFieldDef(Module *pModule,
                                               mdToken FieldDef,
                                               BOOL strictMetadataChecks);

    static void GetDescFromMemberRef(Module * pModule,
                                     mdToken MemberRef,
                                     MethodDesc ** ppMD,
                                     FieldDesc ** ppFD,
                                     const SigTypeContext *pTypeContext,
                                     BOOL strictMetadataChecks,
                                     TypeHandle *ppTH,
                                     // Because of inheritance, the actual type stored in metadata may be sub-class of the
                                     // class that defines the member. The semantics (verification, security checks, etc.) is based on
                                     // the actual type in metadata. This JIT-EE interface passes in TRUE here to get the actual type.
                                     // If actualTypeRequired is false, returned *ppTH will be the MethodDesc::GetMethodTable/FieldDesc::GetEnclosingMethodTable
                                     // except when generics are involved. The actual type will be still returned for generics since it is required
                                     // for instantiation.
                                     // If actualTypeRequired is true, returned *ppTH will always be the actual type defined in metadata.
                                     BOOL actualTypeRequired = FALSE,
                                     PCCOR_SIGNATURE * ppTypeSig = NULL,    // Optionally, return generic signatures fetched from metadata during loading.
                                     ULONG * pcbTypeSig = NULL);

    static MethodDesc * GetMethodDescFromMemberRefAndType(Module * pModule,
                                                          mdToken MemberRef,
                                                          MethodTable * pMT);

    static FieldDesc * GetFieldDescFromMemberRefAndType(Module * pModule,
                                                        mdToken MemberRef,
                                                        MethodTable * pMT);

    static MethodDesc * GetMethodDescFromMethodSpec(Module * pModule,
                                                    mdToken MethodSpec,
                                                    const SigTypeContext *pTypeContext,
                                                    BOOL strictMetadataChecks,
                                                    BOOL allowInstParam,
                                                    TypeHandle *ppTH,
                                                    BOOL actualTypeRequired = FALSE,    // See comment for GetDescFromMemberRef
                                                    PCCOR_SIGNATURE * ppTypeSig = NULL, // Optionally, return generic signatures fetched from metadata during loading.
                                                    ULONG * pcbTypeSig = NULL,
                                                    PCCOR_SIGNATURE * ppMethodSig = NULL,
                                                    ULONG * pcbMethodSig = NULL);

    //-------------------------------------------------------------------
    // METHOD AND FIELD LOOKUP BY NAME AND SIGNATURE
    //

    // Used by FindMethod and varieties
    enum FM_Flags
    {
        // Default behaviour is to scan all methods, virtual and non-virtual, of the current type
        // and all non-virtual methods of all parent types.

        // Default set of flags - this must always be zero.
        FM_Default             = 0x0000,

        // Case sensitivity
        FM_IgnoreCase          = 0x0001,                        // Name matching is case insensitive
        FM_IgnoreName          = (FM_IgnoreCase          << 1), // Ignore the name altogether

        // USE THE FOLLOWING WITH EXTREME CAUTION. We do not want to inadvertently
        // change binding semantics by using this without a really good reason.

        // Virtuals
        FM_ExcludeNonVirtual   = (FM_IgnoreName          << 1), // has mdVirtual set
        FM_ExcludeVirtual      = (FM_ExcludeNonVirtual   << 1), // does not have mdVirtual set.

        // Accessibility.
        // NOTE: These appear in the exact same order as mdPrivateScope ... mdPublic in corhdr.h. This enables some
        //       bit masking to quickly determine if a method qualifies in FM_ShouldSkipMethod.
        FM_ExcludePrivateScope = (FM_ExcludeVirtual      << 1), // Member not referenceable.
        FM_ExcludePrivate      = (FM_ExcludePrivateScope << 1), // Accessible only by the parent type.
        FM_ExcludeFamANDAssem  = (FM_ExcludePrivate      << 1), // Accessible by sub-types only in this Assembly.
        FM_ExcludeAssem        = (FM_ExcludeFamANDAssem  << 1), // Accessibly by anyone in the Assembly.
        FM_ExcludeFamily       = (FM_ExcludeAssem        << 1), // Accessible only by type and sub-types.
        FM_ExcludeFamORAssem   = (FM_ExcludeFamily       << 1), // Accessibly by sub-types anywhere, plus anyone in assembly.
        FM_ExcludePublic       = (FM_ExcludeFamORAssem   << 1), // Accessibly by anyone who has visibility to this scope.
        FM_Unique = (FM_ExcludePublic   << 1),  // Make sure the method is unique for the class

        // This means that FindMethod will only consider mdPublic mdVirtual methods.
        // This is the only time when name/sig lookup will look past the first match.
        FM_ForInterface        = (FM_ExcludeNonVirtual |
                                  FM_ExcludePrivateScope |
                                  FM_ExcludePrivate |
                                  FM_ExcludeFamANDAssem |
                                  FM_ExcludeAssem |
                                  FM_ExcludeFamily |
                                  FM_ExcludeFamORAssem),
    };

private:
    // A mask to indicate that some filtering needs to be done.
    static const FM_Flags FM_SpecialAccessMask = (FM_Flags) (FM_ExcludePrivateScope |
                                                             FM_ExcludePrivate |
                                                             FM_ExcludeFamANDAssem |
                                                             FM_ExcludeAssem |
                                                             FM_ExcludeFamily |
                                                             FM_ExcludeFamORAssem |
                                                             FM_ExcludePublic);

    static const FM_Flags FM_SpecialVirtualMask = (FM_Flags) (FM_ExcludeNonVirtual |
                                                              FM_ExcludeVirtual);

    // Typedef for string comparition functions.
    typedef int (__cdecl *UTF8StringCompareFuncPtr)(const char *, const char *);

    static inline UTF8StringCompareFuncPtr FM_GetStrCompFunc(DWORD dwFlags)
        { LIMITED_METHOD_CONTRACT; return (dwFlags & FM_IgnoreCase) ? stricmpUTF8 : strcmp; }

    static BOOL FM_PossibleToSkipMethod(FM_Flags flags);
    static BOOL FM_ShouldSkipMethod(DWORD dwAttrs, FM_Flags flags);

public:
    static MethodDesc *FindMethod(
       MethodTable * pMT,
       LPCUTF8 pwzName,
       LPHARDCODEDMETASIG pwzSignature,
       FM_Flags flags = FM_Default);

    // typeHnd is the type handle associated with the class being looked up.
    // It has additional information in the case of a domain neutral class (Arrays)
    static MethodDesc *FindMethod(
       MethodTable * pMT,
       LPCUTF8 pszName,
       PCCOR_SIGNATURE pSignature,
       DWORD cSignature,
       Module* pModule,
       FM_Flags flags = FM_Default,
       const Substitution *pDefSubst = NULL);

    static MethodDesc *FindMethod(MethodTable * pMT, mdMethodDef mb);

    static MethodDesc *FindMethodByName(
       MethodTable * pMT,
       LPCUTF8 pszName,
       FM_Flags flags = FM_Default);

    static MethodDesc *FindPropertyMethod(
       MethodTable * pMT,
       LPCUTF8 pszName,
       EnumPropertyMethods Method,
       FM_Flags flags = FM_Default);

    static MethodDesc *FindEventMethod(
       MethodTable * pMT,
       LPCUTF8 pszName,
       EnumEventMethods Method,
       FM_Flags flags = FM_Default);

    static MethodDesc *FindMethodForInterfaceSlot(
       MethodTable * pMT,
       MethodTable *pInterface,
       WORD slotNum);

    // pSignature can be NULL to find any field with the given name
    static FieldDesc *FindField(
       MethodTable * pMT,
       LPCUTF8 pszName,
       PCCOR_SIGNATURE pSignature,
       DWORD cSignature,
       Module* pModule,
       BOOL bCaseSensitive = TRUE);

    static MethodDesc *FindConstructor(MethodTable * pMT, LPHARDCODEDMETASIG pwzSignature);
    static MethodDesc *FindConstructor(MethodTable * pMT, PCCOR_SIGNATURE pSignature,DWORD cSignature, Module* pModule);
};

#endif // MEMBERLOAD_H
