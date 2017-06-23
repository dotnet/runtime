//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _ICorJitInfoImpl
#define _ICorJitInfoImpl

// ICorJitInfoImpl: declare for implementation all the members of the ICorJitInfo interface (which are
// specified as pure virtual methods). This is done once, here, and all implementations share it,
// to avoid duplicated declarations. This file is #include'd within all the ICorJitInfo implementation
// classes.
//
// NOTE: this file is in exactly the same order, with exactly the same whitespace, as the ICorJitInfo
// interface declaration (with the "virtual" and "= 0" syntax removed). This is to make it easy to compare
// against the interface declaration.

public:
/**********************************************************************************/
//
// ICorMethodInfo
//
/**********************************************************************************/

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
DWORD getMethodAttribs(CORINFO_METHOD_HANDLE ftn /* IN */
                       );

// sets private JIT flags, which can be, retrieved using getAttrib.
void setMethodAttribs(CORINFO_METHOD_HANDLE     ftn,    /* IN */
                      CorInfoMethodRuntimeFlags attribs /* IN */
                      );

// Given a method descriptor ftnHnd, extract signature information into sigInfo
//
// 'memberParent' is typically only set when verifying.  It should be the
// result of calling getMemberParent.
void getMethodSig(CORINFO_METHOD_HANDLE ftn,                /* IN  */
                  CORINFO_SIG_INFO*     sig,                /* OUT */
                  CORINFO_CLASS_HANDLE  memberParent = NULL /* IN */
                  );

/*********************************************************************
 * Note the following methods can only be used on functions known
 * to be IL.  This includes the method being compiled and any method
 * that 'getMethodInfo' returns true for
 *********************************************************************/

// return information about a method private to the implementation
//      returns false if method is not IL, or is otherwise unavailable.
//      This method is used to fetch data needed to inline functions
bool getMethodInfo(CORINFO_METHOD_HANDLE ftn, /* IN  */
                   CORINFO_METHOD_INFO*  info /* OUT */
                   );

// Decides if you have any limitations for inlining. If everything's OK, it will return
// INLINE_PASS and will fill out pRestrictions with a mask of restrictions the caller of this
// function must respect. If caller passes pRestrictions = NULL, if there are any restrictions
// INLINE_FAIL will be returned
//
// The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
//
// The inlined method need not be verified

CorInfoInline canInline(CORINFO_METHOD_HANDLE callerHnd,    /* IN  */
                        CORINFO_METHOD_HANDLE calleeHnd,    /* IN  */
                        DWORD*                pRestrictions /* OUT */
                        );

// Reports whether or not a method can be inlined, and why.  canInline is responsible for reporting all
// inlining results when it returns INLINE_FAIL and INLINE_NEVER.  All other results are reported by the
// JIT.
void reportInliningDecision(CORINFO_METHOD_HANDLE inlinerHnd,
                            CORINFO_METHOD_HANDLE inlineeHnd,
                            CorInfoInline         inlineResult,
                            const char*           reason);

// Returns false if the call is across security boundaries thus we cannot tailcall
//
// The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
bool canTailCall(CORINFO_METHOD_HANDLE callerHnd,         /* IN */
                 CORINFO_METHOD_HANDLE declaredCalleeHnd, /* IN */
                 CORINFO_METHOD_HANDLE exactCalleeHnd,    /* IN */
                 bool                  fIsTailPrefix      /* IN */
                 );

// Reports whether or not a method can be tail called, and why.
// canTailCall is responsible for reporting all results when it returns
// false.  All other results are reported by the JIT.
void reportTailCallDecision(CORINFO_METHOD_HANDLE callerHnd,
                            CORINFO_METHOD_HANDLE calleeHnd,
                            bool                  fIsTailPrefix,
                            CorInfoTailCall       tailCallResult,
                            const char*           reason);

// get individual exception handler
void getEHinfo(CORINFO_METHOD_HANDLE ftn,      /* IN  */
               unsigned              EHnumber, /* IN */
               CORINFO_EH_CLAUSE*    clause    /* OUT */
               );

// return class it belongs to
CORINFO_CLASS_HANDLE getMethodClass(CORINFO_METHOD_HANDLE method);

// return module it belongs to
CORINFO_MODULE_HANDLE getMethodModule(CORINFO_METHOD_HANDLE method);

// This function returns the offset of the specified method in the
// vtable of it's owning class or interface.
void getMethodVTableOffset(CORINFO_METHOD_HANDLE method,                /* IN */
                           unsigned*             offsetOfIndirection,   /* OUT */
                           unsigned*             offsetAfterIndirection,/* OUT */
                           unsigned*             isRelative             /* OUT */
                           );

// Find the virtual method in implementingClass that overrides virtualMethod.
// Return null if devirtualization is not possible.
CORINFO_METHOD_HANDLE resolveVirtualMethod(CORINFO_METHOD_HANDLE  virtualMethod,
                                           CORINFO_CLASS_HANDLE   implementingClass,
                                           CORINFO_CONTEXT_HANDLE ownerType);

void expandRawHandleIntrinsic(
    CORINFO_RESOLVED_TOKEN *        pResolvedToken,
    CORINFO_GENERICHANDLE_RESULT *  pResult);

// If a method's attributes have (getMethodAttribs) CORINFO_FLG_INTRINSIC set,
// getIntrinsicID() returns the intrinsic ID.
// *pMustExpand tells whether or not JIT must expand the intrinsic.
CorInfoIntrinsics getIntrinsicID(CORINFO_METHOD_HANDLE method, bool* pMustExpand = NULL /* OUT */
                                 );

// Is the given module the System.Numerics.Vectors module?
// This defaults to false.
bool isInSIMDModule(CORINFO_CLASS_HANDLE classHnd); /* { return false; } */

// return the unmanaged calling convention for a PInvoke
CorInfoUnmanagedCallConv getUnmanagedCallConv(CORINFO_METHOD_HANDLE method);

// return if any marshaling is required for PInvoke methods.  Note that
// method == 0 => calli.  The call site sig is only needed for the varargs or calli case
BOOL pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig);

// Check constraints on method type arguments (only).
// The parent class should be checked separately using satisfiesClassConstraints(parent).
BOOL satisfiesMethodConstraints(CORINFO_CLASS_HANDLE  parent, // the exact parent of the method
                                CORINFO_METHOD_HANDLE method);

// Given a delegate target class, a target method parent class,  a  target method,
// a delegate class, check if the method signature is compatible with the Invoke method of the delegate
// (under the typical instantiation of any free type variables in the memberref signatures).
BOOL isCompatibleDelegate(CORINFO_CLASS_HANDLE  objCls,          /* type of the delegate target, if any */
                          CORINFO_CLASS_HANDLE  methodParentCls, /* exact parent of the target method, if any */
                          CORINFO_METHOD_HANDLE method,          /* (representative) target method, if any */
                          CORINFO_CLASS_HANDLE  delegateCls,     /* exact type of the delegate */
                          BOOL*                 pfIsOpenDelegate /* is the delegate open */
                          );

// Indicates if the method is an instance of the generic
// method that passes (or has passed) verification
CorInfoInstantiationVerification isInstantiationOfVerifiedGeneric(CORINFO_METHOD_HANDLE method /* IN  */
                                                                  );

// Loads the constraints on a typical method definition, detecting cycles;
// for use in verification.
void initConstraintsForVerification(CORINFO_METHOD_HANDLE method,                        /* IN */
                                    BOOL*                 pfHasCircularClassConstraints, /* OUT */
                                    BOOL*                 pfHasCircularMethodConstraint  /* OUT */
                                    );

// Returns enum whether the method does not require verification
// Also see ICorModuleInfo::canSkipVerification
CorInfoCanSkipVerificationResult canSkipMethodVerification(CORINFO_METHOD_HANDLE ftnHandle);

// load and restore the method
void methodMustBeLoadedBeforeCodeIsRun(CORINFO_METHOD_HANDLE method);

CORINFO_METHOD_HANDLE mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE method);

// Returns the global cookie for the /GS unsafe buffer checks
// The cookie might be a constant value (JIT), or a handle to memory location (Ngen)
void getGSCookie(GSCookie*  pCookieVal, // OUT
                 GSCookie** ppCookieVal // OUT
                 );

/**********************************************************************************/
//
// ICorModuleInfo
//
/**********************************************************************************/

// Resolve metadata token into runtime method handles. This function may not
// return normally (e.g. it may throw) if it encounters invalid metadata or other
// failures during token resolution.
void resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken);

// Attempt to resolve a metadata token into a runtime method handle. Returns true
// if resolution succeeded and false otherwise (e.g. if it encounters invalid metadata
// during token reoslution). This method should be used instead of `resolveToken` in
// situations that need to be resilient to invalid metadata.
bool tryResolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken);

// Signature information about the call sig
void findSig(CORINFO_MODULE_HANDLE  module,  /* IN */
             unsigned               sigTOK,  /* IN */
             CORINFO_CONTEXT_HANDLE context, /* IN */
             CORINFO_SIG_INFO*      sig      /* OUT */
             );

// for Varargs, the signature at the call site may differ from
// the signature at the definition.  Thus we need a way of
// fetching the call site information
void findCallSiteSig(CORINFO_MODULE_HANDLE  module,  /* IN */
                     unsigned               methTOK, /* IN */
                     CORINFO_CONTEXT_HANDLE context, /* IN */
                     CORINFO_SIG_INFO*      sig      /* OUT */
                     );

CORINFO_CLASS_HANDLE getTokenTypeAsHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken /* IN  */);

// Returns true if the module does not require verification
//
// If fQuickCheckOnlyWithoutCommit=TRUE, the function only checks that the
// module does not currently require verification in the current AppDomain.
// This decision could change in the future, and so should not be cached.
// If it is cached, it should only be used as a hint.
// This is only used by ngen for calculating certain hints.
//

// Returns enum whether the module does not require verification
// Also see ICorMethodInfo::canSkipMethodVerification();
CorInfoCanSkipVerificationResult canSkipVerification(CORINFO_MODULE_HANDLE module /* IN  */
                                                     );

// Checks if the given metadata token is valid
BOOL isValidToken(CORINFO_MODULE_HANDLE module, /* IN  */
                  unsigned              metaTOK /* IN  */
                  );

// Checks if the given metadata token is valid StringRef
BOOL isValidStringRef(CORINFO_MODULE_HANDLE module, /* IN  */
                      unsigned              metaTOK /* IN  */
                      );

BOOL shouldEnforceCallvirtRestriction(CORINFO_MODULE_HANDLE scope);

/**********************************************************************************/
//
// ICorClassInfo
//
/**********************************************************************************/

// If the value class 'cls' is isomorphic to a primitive type it will
// return that type, otherwise it will return CORINFO_TYPE_VALUECLASS
CorInfoType asCorInfoType(CORINFO_CLASS_HANDLE cls);

// for completeness
const char* getClassName(CORINFO_CLASS_HANDLE cls);

// Append a (possibly truncated) representation of the type cls to the preallocated buffer ppBuf of length pnBufLen
// If fNamespace=TRUE, include the namespace/enclosing classes
// If fFullInst=TRUE (regardless of fNamespace and fAssembly), include namespace and assembly for any type parameters
// If fAssembly=TRUE, suffix with a comma and the full assembly qualification
// return size of representation
int appendClassName(__deref_inout_ecount(*pnBufLen) WCHAR** ppBuf,
                    int*                                    pnBufLen,
                    CORINFO_CLASS_HANDLE                    cls,
                    BOOL                                    fNamespace,
                    BOOL                                    fFullInst,
                    BOOL                                    fAssembly);

// Quick check whether the type is a value class. Returns the same value as getClassAttribs(cls) &
// CORINFO_FLG_VALUECLASS, except faster.
BOOL isValueClass(CORINFO_CLASS_HANDLE cls);

// If this method returns true, JIT will do optimization to inline the check for
//     GetTypeFromHandle(handle) == obj.GetType()
BOOL canInlineTypeCheckWithObjectVTable(CORINFO_CLASS_HANDLE cls);

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
DWORD getClassAttribs(CORINFO_CLASS_HANDLE cls);

// Returns "TRUE" iff "cls" is a struct type such that return buffers used for returning a value
// of this type must be stack-allocated.  This will generally be true only if the struct
// contains GC pointers, and does not exceed some size limit.  Maintaining this as an invariant allows
// an optimization: the JIT may assume that return buffer pointers for return types for which this predicate
// returns TRUE are always stack allocated, and thus, that stores to the GC-pointer fields of such return
// buffers do not require GC write barriers.
BOOL isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls);

CORINFO_MODULE_HANDLE getClassModule(CORINFO_CLASS_HANDLE cls);

// Returns the assembly that contains the module "mod".
CORINFO_ASSEMBLY_HANDLE getModuleAssembly(CORINFO_MODULE_HANDLE mod);

// Returns the name of the assembly "assem".
const char* getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem);

// Allocate and delete process-lifetime objects.  Should only be
// referred to from static fields, lest a leak occur.
// Note that "LongLifetimeFree" does not execute destructors, if "obj"
// is an array of a struct type with a destructor.
void* LongLifetimeMalloc(size_t sz);
void LongLifetimeFree(void* obj);

size_t getClassModuleIdForStatics(CORINFO_CLASS_HANDLE cls, CORINFO_MODULE_HANDLE* pModule, void** ppIndirection);

// return the number of bytes needed by an instance of the class
unsigned getClassSize(CORINFO_CLASS_HANDLE cls);

unsigned getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, BOOL fDoubleAlignHint = FALSE);

// This is only called for Value classes.  It returns a boolean array
// in representing of 'cls' from a GC perspective.  The class is
// assumed to be an array of machine words
// (of length // getClassSize(cls) / sizeof(void*)),
// 'gcPtrs' is a pointer to an array of BYTEs of this length.
// getClassGClayout fills in this array so that gcPtrs[i] is set
// to one of the CorInfoGCType values which is the GC type of
// the i-th machine word of an object of type 'cls'
// returns the number of GC pointers in the array
unsigned getClassGClayout(CORINFO_CLASS_HANDLE cls,   /* IN */
                          BYTE*                gcPtrs /* OUT */
                          );

// returns the number of instance fields in a class
unsigned getClassNumInstanceFields(CORINFO_CLASS_HANDLE cls /* IN */
                                   );

CORINFO_FIELD_HANDLE getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num);

BOOL checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, BOOL fOptional);

// returns the "NEW" helper optimized for "newCls."
CorInfoHelpFunc getNewHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_METHOD_HANDLE callerHandle);

// returns the newArr (1-Dim array) helper optimized for "arrayCls."
CorInfoHelpFunc getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls);

// returns the optimized "IsInstanceOf" or "ChkCast" helper
CorInfoHelpFunc getCastingHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool fThrowing);

// returns helper to trigger static constructor
CorInfoHelpFunc getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd);

CorInfoHelpFunc getSecurityPrologHelper(CORINFO_METHOD_HANDLE ftn);

// This is not pretty.  Boxing nullable<T> actually returns
// a boxed<T> not a boxed Nullable<T>.  This call allows the verifier
// to call back to the EE on the 'box' instruction and get the transformed
// type to use for verification.
CORINFO_CLASS_HANDLE getTypeForBox(CORINFO_CLASS_HANDLE cls);

// returns the correct box helper for a particular class.  Note
// that if this returns CORINFO_HELP_BOX, the JIT can assume
// 'standard' boxing (allocate object and copy), and optimize
CorInfoHelpFunc getBoxHelper(CORINFO_CLASS_HANDLE cls);

// returns the unbox helper.  If 'helperCopies' points to a true
// value it means the JIT is requesting a helper that unboxes the
// value into a particular location and thus has the signature
//     void unboxHelper(void* dest, CORINFO_CLASS_HANDLE cls, Object* obj)
// Otherwise (it is null or points at a FALSE value) it is requesting
// a helper that returns a pointer to the unboxed data
//     void* unboxHelper(CORINFO_CLASS_HANDLE cls, Object* obj)
// The EE has the option of NOT returning the copy style helper
// (But must be able to always honor the non-copy style helper)
// The EE set 'helperCopies' on return to indicate what kind of
// helper has been created.

CorInfoHelpFunc getUnBoxHelper(CORINFO_CLASS_HANDLE cls);

bool getReadyToRunHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                         CORINFO_LOOKUP_KIND*    pGenericLookupKind,
                         CorInfoHelpFunc         id,
                         CORINFO_CONST_LOOKUP*   pLookup);

void getReadyToRunDelegateCtorHelper(CORINFO_RESOLVED_TOKEN* pTargetMethod,
                                     CORINFO_CLASS_HANDLE    delegateType,
                                     CORINFO_LOOKUP*         pLookup);

const char* getHelperName(CorInfoHelpFunc);

// This function tries to initialize the class (run the class constructor).
// this function returns whether the JIT must insert helper calls before
// accessing static field or method.
//
// See code:ICorClassInfo#ClassConstruction.
CorInfoInitClassResult initClass(CORINFO_FIELD_HANDLE field, // Non-NULL - inquire about cctor trigger before static
                                                             // field access NULL - inquire about cctor trigger in
                                                             // method prolog
                                 CORINFO_METHOD_HANDLE  method,             // Method referencing the field or prolog
                                 CORINFO_CONTEXT_HANDLE context,            // Exact context of method
                                 BOOL                   speculative = FALSE // TRUE means don't actually run it
                                 );

// This used to be called "loadClass".  This records the fact
// that the class must be loaded (including restored if necessary) before we execute the
// code that we are currently generating.  When jitting code
// the function loads the class immediately.  When zapping code
// the zapper will if necessary use the call to record the fact that we have
// to do a fixup/restore before running the method currently being generated.
//
// This is typically used to ensure value types are loaded before zapped
// code that manipulates them is executed, so that the GC can access information
// about those value types.
void classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE cls);

// returns the class handle for the special builtin classes
CORINFO_CLASS_HANDLE getBuiltinClass(CorInfoClassId classId);

// "System.Int32" ==> CORINFO_TYPE_INT..
CorInfoType getTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE cls);

// TRUE if child is a subtype of parent
// if parent is an interface, then does child implement / extend parent
BOOL canCast(CORINFO_CLASS_HANDLE child, // subtype (extends parent)
             CORINFO_CLASS_HANDLE parent // base type
             );

// TRUE if cls1 and cls2 are considered equivalent types.
BOOL areTypesEquivalent(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2);

// returns is the intersection of cls1 and cls2.
CORINFO_CLASS_HANDLE mergeClasses(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2);

// Given a class handle, returns the Parent type.
// For COMObjectType, it returns Class Handle of System.Object.
// Returns 0 if System.Object is passed in.
CORINFO_CLASS_HANDLE getParentType(CORINFO_CLASS_HANDLE cls);

// Returns the CorInfoType of the "child type". If the child type is
// not a primitive type, *clsRet will be set.
// Given an Array of Type Foo, returns Foo.
// Given BYREF Foo, returns Foo
CorInfoType getChildType(CORINFO_CLASS_HANDLE clsHnd, CORINFO_CLASS_HANDLE* clsRet);

// Check constraints on type arguments of this class and parent classes
BOOL satisfiesClassConstraints(CORINFO_CLASS_HANDLE cls);

// Check if this is a single dimensional array type
BOOL isSDArray(CORINFO_CLASS_HANDLE cls);

// Get the numbmer of dimensions in an array
unsigned getArrayRank(CORINFO_CLASS_HANDLE cls);

// Get static field data for an array
void* getArrayInitializationData(CORINFO_FIELD_HANDLE field, DWORD size);

// Check Visibility rules.
CorInfoIsAccessAllowedResult canAccessClass(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                            CORINFO_METHOD_HANDLE   callerHandle,
                                            CORINFO_HELPER_DESC* pAccessHelper /* If canAccessMethod returns something
                                                                                  other than ALLOWED, then this is
                                                                                  filled in. */
                                            );

/**********************************************************************************/
//
// ICorFieldInfo
//
/**********************************************************************************/

// this function is for debugging only.  It returns the field name
// and if 'moduleName' is non-null, it sets it to something that will
// says which method (a class name, or a module name)
const char* getFieldName(CORINFO_FIELD_HANDLE ftn,       /* IN */
                         const char**         moduleName /* OUT */
                         );

// return class it belongs to
CORINFO_CLASS_HANDLE getFieldClass(CORINFO_FIELD_HANDLE field);

// Return the field's type, if it is CORINFO_TYPE_VALUECLASS 'structType' is set
// the field's value class (if 'structType' == 0, then don't bother
// the structure info).
//
// 'memberParent' is typically only set when verifying.  It should be the
// result of calling getMemberParent.
CorInfoType getFieldType(CORINFO_FIELD_HANDLE  field,
                         CORINFO_CLASS_HANDLE* structType,
                         CORINFO_CLASS_HANDLE  memberParent = NULL /* IN */
                         );

// return the data member's instance offset
unsigned getFieldOffset(CORINFO_FIELD_HANDLE field);

// TODO: jit64 should be switched to the same plan as the i386 jits - use
// getClassGClayout to figure out the need for writebarrier helper, and inline the copying.
// The interpretted value class copy is slow. Once this happens, USE_WRITE_BARRIER_HELPERS
bool isWriteBarrierHelperRequired(CORINFO_FIELD_HANDLE field);

void getFieldInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                  CORINFO_METHOD_HANDLE   callerHandle,
                  CORINFO_ACCESS_FLAGS    flags,
                  CORINFO_FIELD_INFO*     pResult);

// Returns true iff "fldHnd" represents a static field.
bool isFieldStatic(CORINFO_FIELD_HANDLE fldHnd);

/*********************************************************************************/
//
// ICorDebugInfo
//
/*********************************************************************************/

// Query the EE to find out where interesting break points
// in the code are.  The native compiler will ensure that these places
// have a corresponding break point in native code.
//
// Note that unless CORJIT_FLAG_DEBUG_CODE is specified, this function will
// be used only as a hint and the native compiler should not change its
// code generation.
void getBoundaries(CORINFO_METHOD_HANDLE ftn,                      // [IN] method of interest
                   unsigned int*         cILOffsets,               // [OUT] size of pILOffsets
                   DWORD**               pILOffsets,               // [OUT] IL offsets of interest
                                                                   //       jit MUST free with freeArray!
                   ICorDebugInfo::BoundaryTypes* implictBoundaries // [OUT] tell jit, all boundries of this type
                   );

// Report back the mapping from IL to native code,
// this map should include all boundaries that 'getBoundaries'
// reported as interesting to the debugger.

// Note that debugger (and profiler) is assuming that all of the
// offsets form a contiguous block of memory, and that the
// OffsetMapping is sorted in order of increasing native offset.
void setBoundaries(CORINFO_METHOD_HANDLE         ftn,  // [IN] method of interest
                   ULONG32                       cMap, // [IN] size of pMap
                   ICorDebugInfo::OffsetMapping* pMap  // [IN] map including all points of interest.
                                                       //      jit allocated with allocateArray, EE frees
                   );

// Query the EE to find out the scope of local varables.
// normally the JIT would trash variables after last use, but
// under debugging, the JIT needs to keep them live over their
// entire scope so that they can be inspected.
//
// Note that unless CORJIT_FLAG_DEBUG_CODE is specified, this function will
// be used only as a hint and the native compiler should not change its
// code generation.
void getVars(CORINFO_METHOD_HANDLE      ftn,   // [IN]  method of interest
             ULONG32*                   cVars, // [OUT] size of 'vars'
             ICorDebugInfo::ILVarInfo** vars,  // [OUT] scopes of variables of interest
                                               //       jit MUST free with freeArray!
             bool* extendOthers                // [OUT] it TRUE, then assume the scope
                                               //       of unmentioned vars is entire method
             );

// Report back to the EE the location of every variable.
// note that the JIT might split lifetimes into different
// locations etc.

void setVars(CORINFO_METHOD_HANDLE         ftn,   // [IN] method of interest
             ULONG32                       cVars, // [IN] size of 'vars'
             ICorDebugInfo::NativeVarInfo* vars   // [IN] map telling where local vars are stored at what points
                                                  //      jit allocated with allocateArray, EE frees
             );

/*-------------------------- Misc ---------------------------------------*/

// Used to allocate memory that needs to handed to the EE.
// For eg, use this to allocated memory for reporting debug info,
// which will be handed to the EE by setVars() and setBoundaries()
void* allocateArray(ULONG cBytes);

// JitCompiler will free arrays passed by the EE using this
// For eg, The EE returns memory in getVars() and getBoundaries()
// to the JitCompiler, which the JitCompiler should release using
// freeArray()
void freeArray(void* array);

/*********************************************************************************/
//
// ICorArgInfo
//
/*********************************************************************************/

// advance the pointer to the argument list.
// a ptr of 0, is special and always means the first argument
CORINFO_ARG_LIST_HANDLE getArgNext(CORINFO_ARG_LIST_HANDLE args /* IN */
                                   );

// Get the type of a particular argument
// CORINFO_TYPE_UNDEF is returned when there are no more arguments
// If the type returned is a primitive type (or an enum) *vcTypeRet set to NULL
// otherwise it is set to the TypeHandle associted with the type
// Enumerations will always look their underlying type (probably should fix this)
// Otherwise vcTypeRet is the type as would be seen by the IL,
// The return value is the type that is used for calling convention purposes
// (Thus if the EE wants a value class to be passed like an int, then it will
// return CORINFO_TYPE_INT
CorInfoTypeWithMod getArgType(CORINFO_SIG_INFO*       sig,      /* IN */
                              CORINFO_ARG_LIST_HANDLE args,     /* IN */
                              CORINFO_CLASS_HANDLE*   vcTypeRet /* OUT */
                              );

// If the Arg is a CORINFO_TYPE_CLASS fetch the class handle associated with it
CORINFO_CLASS_HANDLE getArgClass(CORINFO_SIG_INFO*       sig, /* IN */
                                 CORINFO_ARG_LIST_HANDLE args /* IN */
                                 );

// Returns type of HFA for valuetype
CorInfoType getHFAType(CORINFO_CLASS_HANDLE hClass);

/*****************************************************************************
* ICorErrorInfo contains methods to deal with SEH exceptions being thrown
* from the corinfo interface.  These methods may be called when an exception
* with code EXCEPTION_COMPLUS is caught.
*****************************************************************************/

// Returns the HRESULT of the current exception
HRESULT GetErrorHRESULT(struct _EXCEPTION_POINTERS* pExceptionPointers);

// Fetches the message of the current exception
// Returns the size of the message (including terminating null). This can be
// greater than bufferLength if the buffer is insufficient.
ULONG GetErrorMessage(__inout_ecount(bufferLength) LPWSTR buffer, ULONG bufferLength);

// returns EXCEPTION_EXECUTE_HANDLER if it is OK for the compile to handle the
//                        exception, abort some work (like the inlining) and continue compilation
// returns EXCEPTION_CONTINUE_SEARCH if exception must always be handled by the EE
//                    things like ThreadStoppedException ...
// returns EXCEPTION_CONTINUE_EXECUTION if exception is fixed up by the EE

int FilterException(struct _EXCEPTION_POINTERS* pExceptionPointers);

// Cleans up internal EE tracking when an exception is caught.
void HandleException(struct _EXCEPTION_POINTERS* pExceptionPointers);

void ThrowExceptionForJitResult(HRESULT result);

// Throws an exception defined by the given throw helper.
void ThrowExceptionForHelper(const CORINFO_HELPER_DESC* throwHelper);

// Runs the given function under an error trap. This allows the JIT to make calls
// to interface functions that may throw exceptions without needing to be aware of
// the EH ABI, exception types, etc. Returns true if the given function completed
// successfully and false otherwise.
bool runWithErrorTrap(void (*function)(void*), // The function to run
                      void* parameter // The context parameter that will be passed to the function and the handler
                      );

/*****************************************************************************
 * ICorStaticInfo contains EE interface methods which return values that are
 * constant from invocation to invocation.  Thus they may be embedded in
 * persisted information like statically generated code. (This is of course
 * assuming that all code versions are identical each time.)
 *****************************************************************************/

// Return details about EE internal data structures
void getEEInfo(CORINFO_EE_INFO* pEEInfoOut);

// Returns name of the JIT timer log
LPCWSTR getJitTimeLogFilename();

/*********************************************************************************/
//
// Diagnostic methods
//
/*********************************************************************************/

// this function is for debugging only. Returns method token.
// Returns mdMethodDefNil for dynamic methods.
mdMethodDef getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod);

// this function is for debugging only.  It returns the method name
// and if 'moduleName' is non-null, it sets it to something that will
// says which method (a class name, or a module name)
const char* getMethodName(CORINFO_METHOD_HANDLE ftn,       /* IN */
                          const char**          moduleName /* OUT */
                          );

// this function is for debugging only.  It returns a value that
// is will always be the same for a given method.  It is used
// to implement the 'jitRange' functionality
unsigned getMethodHash(CORINFO_METHOD_HANDLE ftn /* IN */
                       );

// this function is for debugging only.
size_t findNameOfToken(CORINFO_MODULE_HANDLE              module,        /* IN  */
                       mdToken                            metaTOK,       /* IN  */
                       __out_ecount(FQNameCapacity) char* szFQName,      /* OUT */
                       size_t                             FQNameCapacity /* IN */
                       );

// returns whether the struct is enregisterable. Only valid on a System V VM. Returns true on success, false on failure.
bool getSystemVAmd64PassStructInRegisterDescriptor(
    /* IN */ CORINFO_CLASS_HANDLE                                  structHnd,
    /* OUT */ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr);

/*****************************************************************************
 * ICorDynamicInfo contains EE interface methods which return values that may
 * change from invocation to invocation.  They cannot be embedded in persisted
 * data; they must be requeried each time the EE is run.
 *****************************************************************************/

//
// These methods return values to the JIT which are not constant
// from session to session.
//
// These methods take an extra parameter : void **ppIndirection.
// If a JIT supports generation of prejit code (install-o-jit), it
// must pass a non-null value for this parameter, and check the
// resulting value.  If *ppIndirection is NULL, code should be
// generated normally.  If non-null, then the value of
// *ppIndirection is an address in the cookie table, and the code
// generator needs to generate an indirection through the table to
// get the resulting value.  In this case, the return result of the
// function must NOT be directly embedded in the generated code.
//
// Note that if a JIT does not support prejit code generation, it
// may ignore the extra parameter & pass the default of NULL - the
// prejit ICorDynamicInfo implementation will see this & generate
// an error if the jitter is used in a prejit scenario.
//

// Return details about EE internal data structures

DWORD getThreadTLSIndex(void** ppIndirection = NULL);

const void* getInlinedCallFrameVptr(void** ppIndirection = NULL);

LONG* getAddrOfCaptureThreadGlobal(void** ppIndirection = NULL);

// return the native entry point to an EE helper (see CorInfoHelpFunc)
void* getHelperFtn(CorInfoHelpFunc ftnNum, void** ppIndirection = NULL);

// return a callable address of the function (native code). This function
// may return a different value (depending on whether the method has
// been JITed or not.
void getFunctionEntryPoint(CORINFO_METHOD_HANDLE ftn,     /* IN  */
                           CORINFO_CONST_LOOKUP* pResult, /* OUT */
                           CORINFO_ACCESS_FLAGS  accessFlags = CORINFO_ACCESS_ANY);

// return a directly callable address. This can be used similarly to the
// value returned by getFunctionEntryPoint() except that it is
// guaranteed to be multi callable entrypoint.
void getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE ftn, CORINFO_CONST_LOOKUP* pResult);

// get the synchronization handle that is passed to monXstatic function
void* getMethodSync(CORINFO_METHOD_HANDLE ftn, void** ppIndirection = NULL);

// get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*).
// Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
CorInfoHelpFunc getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle);

CORINFO_MODULE_HANDLE embedModuleHandle(CORINFO_MODULE_HANDLE handle, void** ppIndirection = NULL);

CORINFO_CLASS_HANDLE embedClassHandle(CORINFO_CLASS_HANDLE handle, void** ppIndirection = NULL);

CORINFO_METHOD_HANDLE embedMethodHandle(CORINFO_METHOD_HANDLE handle, void** ppIndirection = NULL);

CORINFO_FIELD_HANDLE embedFieldHandle(CORINFO_FIELD_HANDLE handle, void** ppIndirection = NULL);

// Given a module scope (module), a method handle (context) and
// a metadata token (metaTOK), fetch the handle
// (type, field or method) associated with the token.
// If this is not possible at compile-time (because the current method's
// code is shared and the token contains generic parameters)
// then indicate how the handle should be looked up at run-time.
//
void embedGenericHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                        BOOL fEmbedParent, // TRUE - embeds parent type handle of the field/method handle
                        CORINFO_GENERICHANDLE_RESULT* pResult);

// Return information used to locate the exact enclosing type of the current method.
// Used only to invoke .cctor method from code shared across generic instantiations
//   !needsRuntimeLookup       statically known (enclosing type of method itself)
//   needsRuntimeLookup:
//      CORINFO_LOOKUP_THISOBJ     use vtable pointer of 'this' param
//      CORINFO_LOOKUP_CLASSPARAM  use vtable hidden param
//      CORINFO_LOOKUP_METHODPARAM use enclosing type of method-desc hidden param
CORINFO_LOOKUP_KIND getLocationOfThisType(CORINFO_METHOD_HANDLE context);

// NOTE: the two methods below--getPInvokeUnmanagedTarget and getAddressOfPInvokeFixup--are
//       deprecated. New code should instead use getAddressOfPInvokeTarget, which subsumes the
//       functionality of these methods.

// return the unmanaged target *if method has already been prelinked.*
void* getPInvokeUnmanagedTarget(CORINFO_METHOD_HANDLE method, void** ppIndirection = NULL);

// return address of fixup area for late-bound PInvoke calls.
void* getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method, void** ppIndirection = NULL);

// return the address of the PInvoke target. May be a fixup area in the
// case of late-bound PInvoke calls.
void getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method, CORINFO_CONST_LOOKUP* pLookup);

// Generate a cookie based on the signature that would needs to be passed
// to CORINFO_HELP_PINVOKE_CALLI
LPVOID GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig, void** ppIndirection = NULL);

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig);

// Gets a handle that is checked to see if the current method is
// included in "JustMyCode"
CORINFO_JUST_MY_CODE_HANDLE getJustMyCodeHandle(CORINFO_METHOD_HANDLE         method,
                                                CORINFO_JUST_MY_CODE_HANDLE** ppIndirection = NULL);

// Gets a method handle that can be used to correlate profiling data.
// This is the IP of a native method, or the address of the descriptor struct
// for IL.  Always guaranteed to be unique per process, and not to move. */
void GetProfilingHandle(BOOL* pbHookFunction, void** pProfilerHandle, BOOL* pbIndirectedHandles);

// Returns instructions on how to make the call. See code:CORINFO_CALL_INFO for possible return values.
void getCallInfo(
    // Token info
    CORINFO_RESOLVED_TOKEN* pResolvedToken,

    // Generics info
    CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,

    // Security info
    CORINFO_METHOD_HANDLE callerHandle,

    // Jit info
    CORINFO_CALLINFO_FLAGS flags,

    // out params
    CORINFO_CALL_INFO* pResult);

BOOL canAccessFamily(CORINFO_METHOD_HANDLE hCaller, CORINFO_CLASS_HANDLE hInstanceType);

// Returns TRUE if the Class Domain ID is the RID of the class (currently true for every class
// except reflection emitted classes and generics)
BOOL isRIDClassDomainID(CORINFO_CLASS_HANDLE cls);

// returns the class's domain ID for accessing shared statics
unsigned getClassDomainID(CORINFO_CLASS_HANDLE cls, void** ppIndirection = NULL);

// return the data's address (for static fields only)
void* getFieldAddress(CORINFO_FIELD_HANDLE field, void** ppIndirection = NULL);

// registers a vararg sig & returns a VM cookie for it (which can contain other stuff)
CORINFO_VARARGS_HANDLE getVarArgsHandle(CORINFO_SIG_INFO* pSig, void** ppIndirection = NULL);

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool canGetVarArgsHandle(CORINFO_SIG_INFO* pSig);

// Allocate a string literal on the heap and return a handle to it
InfoAccessType constructStringLiteral(CORINFO_MODULE_HANDLE module, mdToken metaTok, void** ppValue);

InfoAccessType emptyStringLiteral(void** ppValue);

// (static fields only) given that 'field' refers to thread local store,
// return the ID (TLS index), which is used to find the begining of the
// TLS data area for the particular DLL 'field' is associated with.
DWORD getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE field, void** ppIndirection = NULL);

// Sets another object to intercept calls to "self" and current method being compiled
void setOverride(ICorDynamicInfo* pOverride, CORINFO_METHOD_HANDLE currentMethod);

// Adds an active dependency from the context method's module to the given module
// This is internal callback for the EE. JIT should not call it directly.
void addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo);

CORINFO_METHOD_HANDLE GetDelegateCtor(CORINFO_METHOD_HANDLE methHnd,
                                      CORINFO_CLASS_HANDLE  clsHnd,
                                      CORINFO_METHOD_HANDLE targetMethodHnd,
                                      DelegateCtorArgs*     pCtorData);

void MethodCompileComplete(CORINFO_METHOD_HANDLE methHnd);

// return a thunk that will copy the arguments for the given signature.
void* getTailCallCopyArgsThunk(CORINFO_SIG_INFO* pSig, CorInfoHelperTailCallSpecialHandling flags);

// return memory manager that the JIT can use to allocate a regular memory
IEEMemoryManager* getMemoryManager();

// get a block of memory for the code, readonly data, and read-write data
void allocMem(ULONG              hotCodeSize,   /* IN */
              ULONG              coldCodeSize,  /* IN */
              ULONG              roDataSize,    /* IN */
              ULONG              xcptnsCount,   /* IN */
              CorJitAllocMemFlag flag,          /* IN */
              void**             hotCodeBlock,  /* OUT */
              void**             coldCodeBlock, /* OUT */
              void**             roDataBlock    /* OUT */
              );

// Reserve memory for the method/funclet's unwind information.
// Note that this must be called before allocMem. It should be
// called once for the main method, once for every funclet, and
// once for every block of cold code for which allocUnwindInfo
// will be called.
//
// This is necessary because jitted code must allocate all the
// memory needed for the unwindInfo at the allocMem call.
// For prejitted code we split up the unwinding information into
// separate sections .rdata and .pdata.
//
void reserveUnwindInfo(BOOL  isFunclet,  /* IN */
                       BOOL  isColdCode, /* IN */
                       ULONG unwindSize  /* IN */
                       );

// Allocate and initialize the .rdata and .pdata for this method or
// funclet, and get the block of memory needed for the machine-specific
// unwind information (the info for crawling the stack frame).
// Note that allocMem must be called first.
//
// Parameters:
//
//    pHotCode        main method code buffer, always filled in
//    pColdCode       cold code buffer, only filled in if this is cold code,
//                      null otherwise
//    startOffset     start of code block, relative to appropriate code buffer
//                      (e.g. pColdCode if cold, pHotCode if hot).
//    endOffset       end of code block, relative to appropriate code buffer
//    unwindSize      size of unwind info pointed to by pUnwindBlock
//    pUnwindBlock    pointer to unwind info
//    funcKind        type of funclet (main method code, handler, filter)
//
void allocUnwindInfo(BYTE*          pHotCode,     /* IN */
                     BYTE*          pColdCode,    /* IN */
                     ULONG          startOffset,  /* IN */
                     ULONG          endOffset,    /* IN */
                     ULONG          unwindSize,   /* IN */
                     BYTE*          pUnwindBlock, /* IN */
                     CorJitFuncKind funcKind      /* IN */
                     );

// Get a block of memory needed for the code manager information,
// (the info for enumerating the GC pointers while crawling the
// stack frame).
// Note that allocMem must be called first
void* allocGCInfo(size_t size /* IN */
                  );

void yieldExecution();

// Indicate how many exception handler blocks are to be returned.
// This is guaranteed to be called before any 'setEHinfo' call.
// Note that allocMem must be called before this method can be called.
void setEHcount(unsigned cEH /* IN */
                );

// Set the values for one particular exception handler block.
//
// Handler regions should be lexically contiguous.
// This is because FinallyIsUnwinding() uses lexicality to
// determine if a "finally" clause is executing.
void setEHinfo(unsigned                 EHnumber, /* IN  */
               const CORINFO_EH_CLAUSE* clause    /* IN */
               );

// Level -> fatalError, Level 2 -> Error, Level 3 -> Warning
// Level 4 means happens 10 times in a run, level 5 means 100, level 6 means 1000 ...
// returns non-zero if the logging succeeded
BOOL logMsg(unsigned level, const char* fmt, va_list args);

// do an assert.  will return true if the code should retry (DebugBreak)
// returns false, if the assert should be igored.
int doAssert(const char* szFile, int iLine, const char* szExpr);

void reportFatalError(CorJitResult result);

/*
struct ProfileBuffer  // Also defined here: code:CORBBTPROF_BLOCK_DATA
{
    ULONG ILOffset;
    ULONG ExecutionCount;
};
*/

// allocate a basic block profile buffer where execution counts will be stored
// for jitted basic blocks.
HRESULT allocBBProfileBuffer(ULONG           count, // The number of basic blocks that we have
                             ProfileBuffer** profileBuffer);

// get profile information to be used for optimizing the current method.  The format
// of the buffer is the same as the format the JIT passes to allocBBProfileBuffer.
HRESULT getBBProfileData(CORINFO_METHOD_HANDLE ftnHnd,
                         ULONG*                count, // The number of basic blocks that we have
                         ProfileBuffer**       profileBuffer,
                         ULONG*                numRuns);

// Associates a native call site, identified by its offset in the native code stream, with
// the signature information and method handle the JIT used to lay out the call site. If
// the call site has no signature information (e.g. a helper call) or has no method handle
// (e.g. a CALLI P/Invoke), then null should be passed instead.
void recordCallSite(ULONG                 instrOffset, /* IN */
                    CORINFO_SIG_INFO*     callSig,     /* IN */
                    CORINFO_METHOD_HANDLE methodHandle /* IN */
                    );

// A relocation is recorded if we are pre-jitting.
// A jump thunk may be inserted if we are jitting
void recordRelocation(void* location,   /* IN  */
                      void* target,     /* IN  */
                      WORD  fRelocType, /* IN  */
                      WORD  slotNum,    /* IN  */
                      INT32 addlDelta   /* IN  */
                      );

WORD getRelocTypeHint(void* target);

// A callback to identify the range of address known to point to
// compiler-generated native entry points that call back into
// MSIL.
void getModuleNativeEntryPointRange(void** pStart, /* OUT */
                                    void** pEnd    /* OUT */
                                    );

// For what machine does the VM expect the JIT to generate code? The VM
// returns one of the IMAGE_FILE_MACHINE_* values. Note that if the VM
// is cross-compiling (such as the case for crossgen), it will return a
// different value than if it was compiling for the host architecture.
//
DWORD getExpectedTargetArchitecture();

// Fetches extended flags for a particular compilation instance. Returns
// the number of bytes written to the provided buffer.
DWORD getJitFlags(CORJIT_FLAGS* flags,      /* IN: Points to a buffer that will hold the extended flags. */
                  DWORD         sizeInBytes /* IN: The size of the buffer. Note that this is effectively a
                                                   version number for the CORJIT_FLAGS value. */
                  );

#endif // _ICorJitInfoImpl
