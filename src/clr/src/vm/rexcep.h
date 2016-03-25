// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//====================================================================

//
// Purpose: Lists the commonly-used Runtime Exceptions visible to users.
// 

//
//====================================================================

// If you add an exception, modify CorError.h to add an HResult there.
// (Guidelines for picking a unique number for your HRESULT are in CorError.h)
// Also modify your managed Exception class to include its HResult.  
// Modify __HResults in the same directory as your exception, to include
// your new HResult.  And of course, add your exception and symbolic
// name for your HResult to the list below so it can be thrown from
// within the EE and recognized in Interop scenarios.


// This is an exhaustive list of all exceptions that can be
// thrown by the EE itself.  If you add to this list the IL spec
// needs to be updated!   

// Note: When multiple exceptions map to the same hresult it is very important
//       that the exception that should be created when the hresult in question
//       is returned by a function be FIRST in the list.
//


//
// These are the macro's that need to be implemented before this file is included.
//

//
// EXCEPTION_BEGIN_DEFINE(ns, reKind, bHRformessage, ...)
//
// This macro starts an exception definition.
//
// ns               Namespace of the exception.
// reKind           Name of the exception.
// bHRformessage    When the exception is thrown from the EE, if this argument is true
//                  the EE will create a string with the HRESULT, so that you get a more 
//                  meaningful error message than let's say AssemblyLoadException.
//                  Usually you want to set this to true if your exception corresponds to
//                  more than one HRESULT.
// ...              The list of HRESULTs that map to this exception. The first of the list
//                  is used as the representative HRESULT for the reKind value.
//

//
// #define EXCEPTION_ADD_HR(hr)
//
// This macro adds an additional HRESULT that maps to the exception.
//
// hr          Additional HRESULT that maps to the exception.
//

//
// #define EXCEPTION_END_DEFINE()
//
// This macro terminates the exception definition.
//


//
// Namespaces used to define the exceptions.
//



#include "namespace.h"


// Temporary workaround - adding some HRESULTs that the Jupiter team will define and 
// add to one of their header files for errors.  Once these have been RI'ed into
// windows and are in a file adjacent to winerror.h, we can remove these constants.
// Given integration time, remove this by August 2011 at the very latest.
#ifndef E_XAMLPARSEFAILED
#define E_XAMLPARSEFAILED _HRESULT_TYPEDEF_(0x802B000AL)
#endif
#ifndef E_LAYOUTCYCLE
#define E_LAYOUTCYCLE _HRESULT_TYPEDEF_(0x802B0014L)
#endif
#ifndef E_ELEMENTNOTENABLED
#define E_ELEMENTNOTENABLED _HRESULT_TYPEDEF_(0x802B001EL)
#endif
#ifndef E_ELEMENTNOTAVAILABLE
#define E_ELEMENTNOTAVAILABLE _HRESULT_TYPEDEF_(0x802B001FL)
#endif
#ifndef RO_E_CLOSED
#define RO_E_CLOSED _HRESULT_TYPEDEF_(0x80000013L)
#endif
#ifndef APPMODEL_ERROR_NO_PACKAGE
#define APPMODEL_ERROR_NO_PACKAGE        15700L
#endif

//
// Actual definition of the exceptions and their matching HRESULT's.
// HRESULTs are expected to be defined in CorError.h, and must also be
// redefined in managed code in an __HResults class.  The managed exception
// object MUST use the same HRESULT in all of its constructors for COM Interop.
// Read comments near top of this file.
//
//
// NOTE: Please keep this list sorted according to the name of the exception.
//
//

DEFINE_EXCEPTION(g_ReflectionNS,       AmbiguousMatchException,        false,  COR_E_AMBIGUOUSMATCH)
#ifdef FEATURE_CORECLR
// ApplicationException is removed in CoreCLR
#define kApplicationException kException 
#else
DEFINE_EXCEPTION(g_SystemNS,           ApplicationException,           false,  COR_E_APPLICATION)
#endif // FEATURE_CORECLR
DEFINE_EXCEPTION(g_SystemNS,           AppDomainUnloadedException,     false,  COR_E_APPDOMAINUNLOADED)
DEFINE_EXCEPTION(g_SystemNS,           ArithmeticException,            false,  COR_E_ARITHMETIC)

DEFINE_EXCEPTION(g_SystemNS,           ArgumentException,              false,
                 COR_E_ARGUMENT, STD_CTL_SCODE(449), STD_CTL_SCODE(450), CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT)

DEFINE_EXCEPTION(g_SystemNS,           ArgumentOutOfRangeException,    false,  COR_E_ARGUMENTOUTOFRANGE, HRESULT_FROM_WIN32(ERROR_NO_UNICODE_TRANSLATION))
DEFINE_EXCEPTION(g_SystemNS,           ArrayTypeMismatchException,     false,  COR_E_ARRAYTYPEMISMATCH)

// Keep in sync with the list in EEFileLoadException::GetFileLoadKind in clrex.cpp
DEFINE_EXCEPTION(g_SystemNS,       BadImageFormatException,        true, 
                 COR_E_BADIMAGEFORMAT, CLDB_E_FILE_OLDVER,
                 CLDB_E_INDEX_NOTFOUND,
                 CLDB_E_FILE_CORRUPT, COR_E_NEWER_RUNTIME,
                 COR_E_ASSEMBLYEXPECTED,
                 HRESULT_FROM_WIN32(ERROR_BAD_EXE_FORMAT),
                 HRESULT_FROM_WIN32(ERROR_EXE_MARKED_INVALID),
                 CORSEC_E_INVALID_IMAGE_FORMAT,
                 HRESULT_FROM_WIN32(ERROR_NOACCESS),
                 HRESULT_FROM_WIN32(ERROR_INVALID_ORDINAL),
                 HRESULT_FROM_WIN32(ERROR_INVALID_DLL),
                 HRESULT_FROM_WIN32(ERROR_FILE_CORRUPT),
                 IDS_CLASSLOAD_32BITCLRLOADING64BITASSEMBLY,
                 COR_E_LOADING_REFERENCE_ASSEMBLY,
                 META_E_BAD_SIGNATURE,
                 COR_E_LOADING_WINMD_REFERENCE_ASSEMBLY)

DEFINE_EXCEPTION(g_SystemNS,           CannotUnloadAppDomainException, false,  COR_E_CANNOTUNLOADAPPDOMAIN)
DEFINE_EXCEPTION(g_CodeContractsNS,    ContractException,              false,  COR_E_CODECONTRACTFAILED)
DEFINE_EXCEPTION(g_SystemNS,           ContextMarshalException,        false,  COR_E_CONTEXTMARSHAL)
DEFINE_EXCEPTION(g_ReflectionNS,       CustomAttributeFormatException, false,  COR_E_CUSTOMATTRIBUTEFORMAT)

#if defined(FEATURE_X509) || defined(FEATURE_CRYPTO)
DEFINE_EXCEPTION(g_CryptographyNS,     CryptographicException,         false,  CORSEC_E_CRYPTO)
#endif // FEATURE_X509 || FEATURE_CRYPTO
#ifndef FEATURE_CORECLR
DEFINE_EXCEPTION(g_CryptographyNS,     CryptographicUnexpectedOperationException, false,  CORSEC_E_CRYPTO_UNEX_OPER)
#endif // FEATURE_CORECLR

DEFINE_EXCEPTION(g_SystemNS,           DataMisalignedException,        false,  COR_E_DATAMISALIGNED)

DEFINE_EXCEPTION(g_IONS,               DirectoryNotFoundException,     true,   COR_E_DIRECTORYNOTFOUND, STG_E_PATHNOTFOUND, CTL_E_PATHNOTFOUND)

DEFINE_EXCEPTION(g_SystemNS,           DivideByZeroException,          false,  COR_E_DIVIDEBYZERO, CTL_E_DIVISIONBYZERO)

DEFINE_EXCEPTION(g_SystemNS,           DllNotFoundException,           false,  COR_E_DLLNOTFOUND)
DEFINE_EXCEPTION(g_SystemNS,           DuplicateWaitObjectException,   false,  COR_E_DUPLICATEWAITOBJECT)

DEFINE_EXCEPTION(g_IONS,               EndOfStreamException,           false,  COR_E_ENDOFSTREAM, STD_CTL_SCODE(62))

DEFINE_EXCEPTION(g_SystemNS,           EntryPointNotFoundException,    false,  COR_E_ENTRYPOINTNOTFOUND)
DEFINE_EXCEPTION(g_SystemNS,           Exception,                      false,  COR_E_EXCEPTION)
DEFINE_EXCEPTION(g_SystemNS,           ExecutionEngineException,       false,  COR_E_EXECUTIONENGINE)

DEFINE_EXCEPTION(g_SystemNS,           FieldAccessException,           false,  COR_E_FIELDACCESS)

DEFINE_EXCEPTION(g_IONS,               FileLoadException,              true,
                 COR_E_FILELOAD, FUSION_E_INVALID_PRIVATE_ASM_LOCATION,
                 FUSION_E_SIGNATURE_CHECK_FAILED,
                 FUSION_E_LOADFROM_BLOCKED, FUSION_E_CACHEFILE_FAILED,
                 FUSION_E_ASM_MODULE_MISSING, FUSION_E_INVALID_NAME,
                 FUSION_E_PRIVATE_ASM_DISALLOWED, FUSION_E_HOST_GAC_ASM_MISMATCH,
                 COR_E_MODULE_HASH_CHECK_FAILED, FUSION_E_REF_DEF_MISMATCH,
                 SECURITY_E_INCOMPATIBLE_SHARE, SECURITY_E_INCOMPATIBLE_EVIDENCE,
                 SECURITY_E_UNVERIFIABLE, COR_E_FIXUPSINEXE, HRESULT_FROM_WIN32(ERROR_TOO_MANY_OPEN_FILES),
                 HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION), HRESULT_FROM_WIN32(ERROR_LOCK_VIOLATION),
                 HRESULT_FROM_WIN32(ERROR_OPEN_FAILED), HRESULT_FROM_WIN32(ERROR_DISK_CORRUPT),
                 HRESULT_FROM_WIN32(ERROR_UNRECOGNIZED_VOLUME),
                 HRESULT_FROM_WIN32(ERROR_DLL_INIT_FAILED),
                 FUSION_E_CODE_DOWNLOAD_DISABLED, CORSEC_E_MISSING_STRONGNAME,
                 MSEE_E_ASSEMBLYLOADINPROGRESS,
                 HRESULT_FROM_WIN32(ERROR_FILE_INVALID))

DEFINE_EXCEPTION(g_IONS,               FileNotFoundException,           true, 
                 HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND), HRESULT_FROM_WIN32(ERROR_MOD_NOT_FOUND), 
                 HRESULT_FROM_WIN32(ERROR_INVALID_NAME), CTL_E_FILENOTFOUND,
                 HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND), HRESULT_FROM_WIN32(ERROR_BAD_NET_NAME),
                 HRESULT_FROM_WIN32(ERROR_BAD_NETPATH), HRESULT_FROM_WIN32(ERROR_NOT_READY),
                 HRESULT_FROM_WIN32(ERROR_WRONG_TARGET_NAME), INET_E_UNKNOWN_PROTOCOL,
                 INET_E_CONNECTION_TIMEOUT, INET_E_CANNOT_CONNECT, INET_E_RESOURCE_NOT_FOUND,
                 INET_E_OBJECT_NOT_FOUND, INET_E_DOWNLOAD_FAILURE, INET_E_DATA_NOT_AVAILABLE,
                 HRESULT_FROM_WIN32(ERROR_DLL_NOT_FOUND),
                 CLR_E_BIND_ASSEMBLY_VERSION_TOO_LOW, CLR_E_BIND_ASSEMBLY_PUBLIC_KEY_MISMATCH,
                 CLR_E_BIND_ASSEMBLY_NOT_FOUND)

DEFINE_EXCEPTION(g_SystemNS,           FormatException,                false,  COR_E_FORMAT)

DEFINE_EXCEPTION(g_SystemNS,           IndexOutOfRangeException,       false,  COR_E_INDEXOUTOFRANGE, 0x800a0009 /*Subscript out of range*/)
DEFINE_EXCEPTION(g_SystemNS,           InsufficientExecutionStackException, false,  COR_E_INSUFFICIENTEXECUTIONSTACK)
DEFINE_EXCEPTION(g_SystemNS,           InvalidCastException,           false,  COR_E_INVALIDCAST)
#ifdef FEATURE_COMINTEROP
DEFINE_EXCEPTION(g_InteropNS,          InvalidComObjectException,      false,  COR_E_INVALIDCOMOBJECT)
#endif //FEATURE_COMINTEROP
DEFINE_EXCEPTION(g_ReflectionNS,       InvalidFilterCriteriaException, false,  COR_E_INVALIDFILTERCRITERIA)
DEFINE_EXCEPTION(g_InteropNS,          InvalidOleVariantTypeException, false,  COR_E_INVALIDOLEVARIANTTYPE)

DEFINE_EXCEPTION(g_SystemNS,           InvalidOperationException,      false,  COR_E_INVALIDOPERATION)

#ifdef FEATURE_COMINTEROP
DEFINE_EXCEPTION_HR_WINRT_ONLY(g_SystemNS, InvalidOperationException,  COR_E_INVALIDOPERATION,
                                                                       E_ILLEGAL_STATE_CHANGE, 
                                                                       E_ILLEGAL_METHOD_CALL,   
                                                                       E_ILLEGAL_DELEGATE_ASSIGNMENT, 
                                                                       HRESULT_FROM_WIN32(APPMODEL_ERROR_NO_PACKAGE))
#endif  // FEATURE_COMINTEROP

DEFINE_EXCEPTION(g_SystemNS,           InvalidProgramException,        false,  COR_E_INVALIDPROGRAM)

DEFINE_EXCEPTION(g_IONS,               IOException,                    false,  COR_E_IO, CTL_E_DEVICEIOERROR, STD_CTL_SCODE(31036), STD_CTL_SCODE(31037))

DEFINE_EXCEPTION(g_InteropNS,          MarshalDirectiveException,      false,  COR_E_MARSHALDIRECTIVE)
DEFINE_EXCEPTION(g_SystemNS,           MethodAccessException,          false,  COR_E_METHODACCESS, META_E_CA_FRIENDS_SN_REQUIRED)
DEFINE_EXCEPTION(g_SystemNS,           MemberAccessException,          false,  COR_E_MEMBERACCESS)
DEFINE_EXCEPTION(g_SystemNS,           MissingFieldException,          false,  COR_E_MISSINGFIELD)
DEFINE_EXCEPTION(g_ResourcesNS,        MissingManifestResourceException, false,COR_E_MISSINGMANIFESTRESOURCE)

DEFINE_EXCEPTION(g_SystemNS,           MissingMemberException,         false,  COR_E_MISSINGMEMBER, STD_CTL_SCODE(461))

DEFINE_EXCEPTION(g_SystemNS,           MissingMethodException,         false,  COR_E_MISSINGMETHOD)
DEFINE_EXCEPTION(g_SystemNS,           MulticastNotSupportedException, false,  COR_E_MULTICASTNOTSUPPORTED)

DEFINE_EXCEPTION(g_SystemNS,           NotFiniteNumberException,       false,  COR_E_NOTFINITENUMBER)

DEFINE_EXCEPTION(g_SystemNS,           NotSupportedException,          false,  COR_E_NOTSUPPORTED, STD_CTL_SCODE(438), STD_CTL_SCODE(445), STD_CTL_SCODE(458), STD_CTL_SCODE(459))

DEFINE_EXCEPTION(g_SystemNS,           NullReferenceException,         false,  COR_E_NULLREFERENCE)
// Note: this has to come after NullReferenceException since we want NullReferenceException to be created
// when E_POINTER is returned from COM interfaces.
DEFINE_EXCEPTION(g_SystemNS,           AccessViolationException,       false,  E_POINTER)

DEFINE_EXCEPTION(g_SystemNS,           ObjectDisposedException,        false,  COR_E_OBJECTDISPOSED, RO_E_CLOSED)

DEFINE_EXCEPTION(g_SystemNS,           OperationCanceledException,     false,  COR_E_OPERATIONCANCELED)

DEFINE_EXCEPTION(g_SystemNS,           OverflowException,              false,  COR_E_OVERFLOW, CTL_E_OVERFLOW)

DEFINE_EXCEPTION(g_IONS,               PathTooLongException,           false,  COR_E_PATHTOOLONG)
 
DEFINE_EXCEPTION(g_SystemNS,           PlatformNotSupportedException,  false,  COR_E_PLATFORMNOTSUPPORTED)

DEFINE_EXCEPTION(g_SystemNS,           RankException,                  false,  COR_E_RANK)
DEFINE_EXCEPTION(g_ReflectionNS,       ReflectionTypeLoadException,    false,  COR_E_REFLECTIONTYPELOAD)
#ifdef FEATURE_REMOTING
DEFINE_EXCEPTION(g_RemotingNS,         RemotingException,              false,  COR_E_REMOTING)
#endif // FEATURE_REMOTING
DEFINE_EXCEPTION(g_CompilerServicesNS, RuntimeWrappedException,        false,  COR_E_RUNTIMEWRAPPED)

#ifdef FEATURE_REMOTING
DEFINE_EXCEPTION(g_RemotingNS,         ServerException,                false,  COR_E_SERVER)
#endif // FEATURE_REMOTING

DEFINE_EXCEPTION(g_SecurityNS,         SecurityException,              true,
                 COR_E_SECURITY, CORSEC_E_INVALID_STRONGNAME,
                 CTL_E_PERMISSIONDENIED, STD_CTL_SCODE(419),
                 CORSEC_E_INVALID_PUBLICKEY, CORSEC_E_SIGNATURE_MISMATCH)

#if FEATURE_COMINTEROP
DEFINE_EXCEPTION(g_InteropNS,          SafeArrayRankMismatchException, false,  COR_E_SAFEARRAYRANKMISMATCH)
DEFINE_EXCEPTION(g_InteropNS,          SafeArrayTypeMismatchException, false,  COR_E_SAFEARRAYTYPEMISMATCH)
#endif //FEATURE_COMINTEROP
DEFINE_EXCEPTION(g_SerializationNS,    SerializationException,         false,  COR_E_SERIALIZATION)

DEFINE_EXCEPTION(g_SystemNS,           StackOverflowException,         false,  COR_E_STACKOVERFLOW, CTL_E_OUTOFSTACKSPACE)

DEFINE_EXCEPTION(g_ThreadingNS,        SynchronizationLockException,   false,  COR_E_SYNCHRONIZATIONLOCK)
DEFINE_EXCEPTION(g_SystemNS,           SystemException,                false,  COR_E_SYSTEM)

DEFINE_EXCEPTION(g_ReflectionNS,       TargetException,                false,  COR_E_TARGET)
DEFINE_EXCEPTION(g_ReflectionNS,       TargetInvocationException,      false,  COR_E_TARGETINVOCATION)
DEFINE_EXCEPTION(g_ReflectionNS,       TargetParameterCountException,  false,  COR_E_TARGETPARAMCOUNT)
DEFINE_EXCEPTION(g_ThreadingNS,        ThreadAbortException,           false,  COR_E_THREADABORTED)
DEFINE_EXCEPTION(g_ThreadingNS,        ThreadInterruptedException,     false,  COR_E_THREADINTERRUPTED)
DEFINE_EXCEPTION(g_ThreadingNS,        ThreadStateException,           false,  COR_E_THREADSTATE)
DEFINE_EXCEPTION(g_ThreadingNS,        ThreadStartException,           false,  COR_E_THREADSTART)
DEFINE_EXCEPTION(g_SystemNS,           TypeAccessException,            false,  COR_E_TYPEACCESS)
DEFINE_EXCEPTION(g_SystemNS,           TypeInitializationException,    false,  COR_E_TYPEINITIALIZATION)

#ifdef FEATURE_COMINTEROP
DEFINE_EXCEPTION(g_SystemNS,           TypeLoadException,              false,  COR_E_TYPELOAD,
                 RO_E_METADATA_NAME_NOT_FOUND, CLR_E_BIND_TYPE_NOT_FOUND)
#else
DEFINE_EXCEPTION(g_SystemNS,           TypeLoadException,              false,  COR_E_TYPELOAD)
#endif

DEFINE_EXCEPTION(g_SystemNS,           TypeUnloadedException,          false,  COR_E_TYPEUNLOADED)

DEFINE_EXCEPTION(g_SystemNS,           UnauthorizedAccessException,    true,   COR_E_UNAUTHORIZEDACCESS, CTL_E_PATHFILEACCESSERROR, STD_CTL_SCODE(335))

DEFINE_EXCEPTION(g_SecurityNS,         VerificationException,          false,  COR_E_VERIFICATION)

#ifdef FEATURE_CAS_POLICY
DEFINE_EXCEPTION(g_PolicyNS,           PolicyException,                true,   CORSEC_E_POLICY_EXCEPTION, CORSEC_E_NO_EXEC_PERM, CORSEC_E_MIN_GRANT_FAIL)
DEFINE_EXCEPTION(g_SecurityNS,         XmlSyntaxException,             false,  CORSEC_E_XMLSYNTAX)
#endif // FEATURE_CAS_POLICY

DEFINE_EXCEPTION(g_InteropNS,          COMException,                   false,  E_FAIL)
DEFINE_EXCEPTION(g_InteropNS,          ExternalException,              false,  E_FAIL)
DEFINE_EXCEPTION(g_InteropNS,          SEHException,                   false,  E_FAIL)
DEFINE_EXCEPTION(g_SystemNS,           NotImplementedException,        false,  E_NOTIMPL)

DEFINE_EXCEPTION(g_SystemNS,           OutOfMemoryException,           false,  E_OUTOFMEMORY, CTL_E_OUTOFMEMORY, STD_CTL_SCODE(31001))

#ifdef FEATURE_CORECLR
DEFINE_EXCEPTION(g_SystemNS,           CrossAppDomainMarshaledException, false, E_FAIL)
#endif //FEATURE_CORECLR

#ifdef FEATURE_ISOSTORE
DEFINE_EXCEPTION(g_IsolatedStorageNS,  IsolatedStorageException,       true,
                 ISS_E_ISOSTORE, ISS_E_ISOSTORE, ISS_E_OPEN_STORE_FILE, 
                 ISS_E_OPEN_FILE_MAPPING, ISS_E_MAP_VIEW_OF_FILE, ISS_E_GET_FILE_SIZE, ISS_E_CREATE_MUTEX, ISS_E_LOCK_FAILED,
                 ISS_E_FILE_WRITE, ISS_E_SET_FILE_POINTER, ISS_E_CREATE_DIR,
                 ISS_E_CORRUPTED_STORE_FILE, ISS_E_STORE_VERSION, ISS_E_FILE_NOT_MAPPED, ISS_E_BLOCK_SIZE_TOO_SMALL, 
                 ISS_E_ALLOC_TOO_LARGE, ISS_E_USAGE_WILL_EXCEED_QUOTA, ISS_E_TABLE_ROW_NOT_FOUND, ISS_E_DEPRECATE, ISS_E_CALLER,
                 ISS_E_PATH_LENGTH, ISS_E_MACHINE, ISS_E_STORE_NOT_OPEN, ISS_E_MACHINE_DACL)
#endif // FEATURE_ISOSTORE

DEFINE_EXCEPTION(g_SystemNS,           ArgumentNullException,          false,  E_POINTER)

#define kLastExceptionInMscorlib kArgumentNullException

//
// All exceptions defined in other .NET Framework assemblies have to be at the end
//

#ifdef FEATURE_COMINTEROP
// Jupiter needs some HRESULTs mapped to exceptions in .NET Framework assemblies other than mscorlib.
DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY(g_MarkupNS, XamlParseException, "System.Runtime.WindowsRuntime.UI.Xaml", ECMA_PUBLICKEY_STR, false, E_XAMLPARSEFAILED)
DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY(g_AutomationNS, ElementNotAvailableException, "System.Runtime.WindowsRuntime.UI.Xaml", ECMA_PUBLICKEY_STR, false, E_ELEMENTNOTAVAILABLE)
DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY(g_AutomationNS, ElementNotEnabledException, "System.Runtime.WindowsRuntime.UI.Xaml", ECMA_PUBLICKEY_STR, false, E_ELEMENTNOTENABLED)
DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY(g_DirectUINS, LayoutCycleException, "System.Runtime.WindowsRuntime.UI.Xaml", ECMA_PUBLICKEY_STR, false, E_LAYOUTCYCLE)
#endif // FEATURE_COMINTEROP


// Please see comments on at the top of this list 

#undef DEFINE_EXCEPTION
#undef DEFINE_EXCEPTION_HR_WINRT_ONLY
#undef DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY
