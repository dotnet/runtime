// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//=============================================================================
//
//
// Purpose: Define HResult constants. Every exception has one of these.
//
//
//===========================================================================*/

namespace System
{
    // Note: FACILITY_URT is defined as 0x13 (0x8013xxxx).  Within that
    // range, 0x1yyy is for Runtime errors (used for Security, Metadata, etc).
    // In that subrange, 0x15zz and 0x16zz have been allocated for classlib-type 
    // HResults. Also note that some of our HResults have to map to certain 
    // COM HR's, etc.

    // Another arbitrary decision...  Feel free to change this, as long as you
    // renumber the HResults yourself (and update rexcep.h).
    // Reflection will use 0x1600 -> 0x161f.  IO will use 0x1620 -> 0x163f.
    // Security will use 0x1640 -> 0x165f

    // There are HResults files in the IO, Remoting, Reflection & 
    // Security/Util directories as well, so choose your HResults carefully.
    internal static class HResults
    {
        internal const int APPMODEL_ERROR_NO_PACKAGE = unchecked((int)0x80073D54);
        internal const int CLDB_E_FILE_CORRUPT = unchecked((int)0x8013110e);
        internal const int CLDB_E_FILE_OLDVER = unchecked((int)0x80131107);
        internal const int CLDB_E_INDEX_NOTFOUND = unchecked((int)0x80131124);
        internal const int CLR_E_BIND_ASSEMBLY_NOT_FOUND = unchecked((int)0x80132004);
        internal const int CLR_E_BIND_ASSEMBLY_PUBLIC_KEY_MISMATCH = unchecked((int)0x80132001);
        internal const int CLR_E_BIND_ASSEMBLY_VERSION_TOO_LOW = unchecked((int)0x80132000);
        internal const int CLR_E_BIND_TYPE_NOT_FOUND = unchecked((int)0x80132005);
        internal const int CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT = unchecked((int)0x80132003);
        internal const int COR_E_ABANDONEDMUTEX = unchecked((int)0x8013152D);
        internal const int COR_E_AMBIGUOUSMATCH = unchecked((int)0x8000211D);
        internal const int COR_E_APPDOMAINUNLOADED = unchecked((int)0x80131014);
        internal const int COR_E_APPLICATION = unchecked((int)0x80131600);
        internal const int COR_E_ARGUMENT = unchecked((int)0x80070057);
        internal const int COR_E_ARGUMENTOUTOFRANGE = unchecked((int)0x80131502);
        internal const int COR_E_ARITHMETIC = unchecked((int)0x80070216);
        internal const int COR_E_ARRAYTYPEMISMATCH = unchecked((int)0x80131503);
        internal const int COR_E_ASSEMBLYEXPECTED = unchecked((int)0x80131018);
        internal const int COR_E_BADIMAGEFORMAT = unchecked((int)0x8007000B);
        internal const int COR_E_CANNOTUNLOADAPPDOMAIN = unchecked((int)0x80131015);
        internal const int COR_E_CODECONTRACTFAILED = unchecked((int)0x80131542);
        internal const int COR_E_CONTEXTMARSHAL = unchecked((int)0x80131504);
        internal const int COR_E_CUSTOMATTRIBUTEFORMAT = unchecked((int)0x80131605);
        internal const int COR_E_DATAMISALIGNED = unchecked((int)0x80131541);
        internal const int COR_E_DIVIDEBYZERO = unchecked((int)0x80020012); // DISP_E_DIVBYZERO
        internal const int COR_E_DLLNOTFOUND = unchecked((int)0x80131524);
        internal const int COR_E_DUPLICATEWAITOBJECT = unchecked((int)0x80131529);
        internal const int COR_E_ENTRYPOINTNOTFOUND = unchecked((int)0x80131523);
        internal const int COR_E_EXCEPTION = unchecked((int)0x80131500);
        internal const int COR_E_EXECUTIONENGINE = unchecked((int)0x80131506);
        internal const int COR_E_FIELDACCESS = unchecked((int)0x80131507);
        internal const int COR_E_FIXUPSINEXE = unchecked((int)0x80131019);
        internal const int COR_E_FORMAT = unchecked((int)0x80131537);
        internal const int COR_E_INDEXOUTOFRANGE = unchecked((int)0x80131508);
        internal const int COR_E_INSUFFICIENTEXECUTIONSTACK = unchecked((int)0x80131578);
        internal const int COR_E_INVALIDCAST = unchecked((int)0x80004002);
        internal const int COR_E_INVALIDCOMOBJECT = unchecked((int)0x80131527);
        internal const int COR_E_INVALIDFILTERCRITERIA = unchecked((int)0x80131601);
        internal const int COR_E_INVALIDOLEVARIANTTYPE = unchecked((int)0x80131531);
        internal const int COR_E_INVALIDOPERATION = unchecked((int)0x80131509);
        internal const int COR_E_INVALIDPROGRAM = unchecked((int)0x8013153a);
        internal const int COR_E_KEYNOTFOUND = unchecked((int)0x80131577);
        internal const int COR_E_LOADING_REFERENCE_ASSEMBLY = unchecked((int)0x80131058);
        internal const int COR_E_MARSHALDIRECTIVE = unchecked((int)0x80131535);
        internal const int COR_E_MEMBERACCESS = unchecked((int)0x8013151A);
        internal const int COR_E_METHODACCESS = unchecked((int)0x80131510);
        internal const int COR_E_MISSINGFIELD = unchecked((int)0x80131511);
        internal const int COR_E_MISSINGMANIFESTRESOURCE = unchecked((int)0x80131532);
        internal const int COR_E_MISSINGMEMBER = unchecked((int)0x80131512);
        internal const int COR_E_MISSINGMETHOD = unchecked((int)0x80131513);
        internal const int COR_E_MISSINGSATELLITEASSEMBLY = unchecked((int)0x80131536);
        internal const int COR_E_MODULE_HASH_CHECK_FAILED = unchecked((int)0x80131039);
        internal const int COR_E_MULTICASTNOTSUPPORTED = unchecked((int)0x80131514);
        internal const int COR_E_NEWER_RUNTIME = unchecked((int)0x8013101b);
        internal const int COR_E_NOTFINITENUMBER = unchecked((int)0x80131528);
        internal const int COR_E_NOTSUPPORTED = unchecked((int)0x80131515);
        internal const int COR_E_NULLREFERENCE = unchecked((int)0x80004003);
        internal const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);
        internal const int COR_E_OPERATIONCANCELED = unchecked((int)0x8013153B);
        internal const int COR_E_OUTOFMEMORY = unchecked((int)0x8007000E);
        internal const int COR_E_OVERFLOW = unchecked((int)0x80131516);
        internal const int COR_E_PLATFORMNOTSUPPORTED = unchecked((int)0x80131539);
        internal const int COR_E_RANK = unchecked((int)0x80131517);
        internal const int COR_E_REFLECTIONTYPELOAD = unchecked((int)0x80131602);
        internal const int COR_E_REMOTING = unchecked((int)0x8013150b);
        internal const int COR_E_RUNTIMEWRAPPED = unchecked((int)0x8013153e);
        internal const int COR_E_SAFEARRAYRANKMISMATCH = unchecked((int)0x80131538);
        internal const int COR_E_SAFEARRAYTYPEMISMATCH = unchecked((int)0x80131533);
        internal const int COR_E_SECURITY = unchecked((int)0x8013150A);
        internal const int COR_E_SERIALIZATION = unchecked((int)0x8013150C);
        internal const int COR_E_SERVER = unchecked((int)0x8013150e);
        internal const int COR_E_STACKOVERFLOW = unchecked((int)0x800703E9);
        internal const int COR_E_SYNCHRONIZATIONLOCK = unchecked((int)0x80131518);
        internal const int COR_E_SYSTEM = unchecked((int)0x80131501);
        internal const int COR_E_TARGET = unchecked((int)0x80131603);
        internal const int COR_E_TARGETINVOCATION = unchecked((int)0x80131604);
        internal const int COR_E_TARGETPARAMCOUNT = unchecked((int)0x8002000e);
        internal const int COR_E_THREADABORTED = unchecked((int)0x80131530);
        internal const int COR_E_THREADINTERRUPTED = unchecked((int)0x80131519);
        internal const int COR_E_THREADSTART = unchecked((int)0x80131525);
        internal const int COR_E_THREADSTATE = unchecked((int)0x80131520);
        internal const int COR_E_TIMEOUT = unchecked((int)0x80131505);
        internal const int COR_E_TYPEACCESS = unchecked((int)0x80131543);
        internal const int COR_E_TYPEINITIALIZATION = unchecked((int)0x80131534);
        internal const int COR_E_TYPELOAD = unchecked((int)0x80131522);
        internal const int COR_E_TYPEUNLOADED = unchecked((int)0x80131013);
        internal const int COR_E_UNAUTHORIZEDACCESS = unchecked((int)0x80070005);
        internal const int COR_E_VERIFICATION = unchecked((int)0x8013150D);
        internal const int COR_E_WAITHANDLECANNOTBEOPENED = unchecked((int)0x8013152C);
        internal const int CORSEC_E_CRYPTO = unchecked((int)0x80131430);
        internal const int CORSEC_E_CRYPTO_UNEX_OPER = unchecked((int)0x80131431);
        internal const int CORSEC_E_INVALID_IMAGE_FORMAT = unchecked((int)0x8013141d);
        internal const int CORSEC_E_INVALID_PUBLICKEY = unchecked((int)0x8013141e);
        internal const int CORSEC_E_INVALID_STRONGNAME = unchecked((int)0x8013141a);
        internal const int CORSEC_E_MIN_GRANT_FAIL = unchecked((int)0x80131417);
        internal const int CORSEC_E_MISSING_STRONGNAME = unchecked((int)0x8013141b);
        internal const int CORSEC_E_NO_EXEC_PERM = unchecked((int)0x80131418);
        internal const int CORSEC_E_POLICY_EXCEPTION = unchecked((int)0x80131416);
        internal const int CORSEC_E_SIGNATURE_MISMATCH = unchecked((int)0x80131420);
        internal const int CORSEC_E_XMLSYNTAX = unchecked((int)0x80131419);
        internal const int CTL_E_DEVICEIOERROR = unchecked((int)0x800A0039);
        internal const int CTL_E_DIVISIONBYZERO = unchecked((int)0x800A000B);
        internal const int CTL_E_FILENOTFOUND = unchecked((int)0x800A0035);
        internal const int CTL_E_OUTOFMEMORY = unchecked((int)0x800A0007);
        internal const int CTL_E_OUTOFSTACKSPACE = unchecked((int)0x800A001C);
        internal const int CTL_E_OVERFLOW = unchecked((int)0x800A0006);
        internal const int CTL_E_PATHFILEACCESSERROR = unchecked((int)0x800A004B);
        internal const int CTL_E_PATHNOTFOUND = unchecked((int)0x800A004C);
        internal const int CTL_E_PERMISSIONDENIED = unchecked((int)0x800A0046);
        internal const int E_ELEMENTNOTAVAILABLE = unchecked((int)0x802B001F);
        internal const int E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_HANDLE = unchecked((int)0x80070006);
        internal const int E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);
        internal const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000E);
        internal const int E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000D);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
        internal const int E_LAYOUTCYCLE = unchecked((int)0x802B0014);
        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        internal const int E_POINTER = unchecked((int)0x80004003L);
        internal const int E_XAMLPARSEFAILED = unchecked((int)0x802B000A);
        internal const int ERROR_BAD_EXE_FORMAT = unchecked((int)0x800700C1);
        internal const int ERROR_BAD_NET_NAME = unchecked((int)0x80070043);
        internal const int ERROR_BAD_NETPATH = unchecked((int)0x80070035);
        internal const int ERROR_DISK_CORRUPT = unchecked((int)0x80070571);
        internal const int ERROR_DLL_INIT_FAILED = unchecked((int)0x8007045A);
        internal const int ERROR_DLL_NOT_FOUND = unchecked((int)0x80070485);
        internal const int ERROR_EXE_MARKED_INVALID = unchecked((int)0x800700C0);
        internal const int ERROR_FILE_CORRUPT = unchecked((int)0x80070570);
        internal const int ERROR_FILE_INVALID = unchecked((int)0x800703EE);
        internal const int ERROR_FILE_NOT_FOUND = unchecked((int)0x80070002);
        internal const int ERROR_INVALID_DLL = unchecked((int)0x80070482);
        internal const int ERROR_INVALID_NAME = unchecked((int)0x8007007B);
        internal const int ERROR_INVALID_ORDINAL = unchecked((int)0x800700B6);
        internal const int ERROR_INVALID_PARAMETER = unchecked((int)0x80070057);
        internal const int ERROR_LOCK_VIOLATION = unchecked((int)0x80070021);
        internal const int ERROR_MOD_NOT_FOUND = unchecked((int)0x8007007E);
        internal const int ERROR_NO_UNICODE_TRANSLATION = unchecked((int)0x80070459);
        internal const int ERROR_NOACCESS = unchecked((int)0x800703E6);
        internal const int ERROR_NOT_OWNER = unchecked((int)0x80070120);
        internal const int ERROR_NOT_READY = unchecked((int)0x80070015);
        internal const int ERROR_OPEN_FAILED = unchecked((int)0x8007006E);
        internal const int ERROR_PATH_NOT_FOUND = unchecked((int)0x80070003);
        internal const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
        internal const int ERROR_TIMEOUT = unchecked((int)0x800705B4);
        internal const int ERROR_TOO_MANY_OPEN_FILES = unchecked((int)0x80070004);
        internal const int ERROR_UNRECOGNIZED_VOLUME = unchecked((int)0x800703ED);
        internal const int ERROR_WRONG_TARGET_NAME = unchecked((int)0x80070574);
        internal const int FUSION_E_ASM_MODULE_MISSING = unchecked((int)0x80131042);
        internal const int FUSION_E_CACHEFILE_FAILED = unchecked((int)0x80131052);
        internal const int FUSION_E_CODE_DOWNLOAD_DISABLED = unchecked((int)0x80131048);
        internal const int FUSION_E_HOST_GAC_ASM_MISMATCH = unchecked((int)0x80131050);
        internal const int FUSION_E_INVALID_NAME = unchecked((int)0x80131047);
        internal const int FUSION_E_INVALID_PRIVATE_ASM_LOCATION = unchecked((int)0x80131041);
        internal const int FUSION_E_LOADFROM_BLOCKED = unchecked((int)0x80131051);
        internal const int FUSION_E_PRIVATE_ASM_DISALLOWED = unchecked((int)0x80131044);
        internal const int FUSION_E_REF_DEF_MISMATCH = unchecked((int)0x80131040);
        internal const int FUSION_E_SIGNATURE_CHECK_FAILED = unchecked((int)0x80131045);
        internal const int INET_E_CANNOT_CONNECT = unchecked((int)0x800C0004);
        internal const int INET_E_CONNECTION_TIMEOUT = unchecked((int)0x800C000B);
        internal const int INET_E_DATA_NOT_AVAILABLE = unchecked((int)0x800C0007);
        internal const int INET_E_DOWNLOAD_FAILURE = unchecked((int)0x800C0008);
        internal const int INET_E_OBJECT_NOT_FOUND = unchecked((int)0x800C0006);
        internal const int INET_E_RESOURCE_NOT_FOUND = unchecked((int)0x800C0005);
        internal const int INET_E_UNKNOWN_PROTOCOL = unchecked((int)0x800C000D);
        internal const int ISS_E_ALLOC_TOO_LARGE = unchecked((int)0x80131484);
        internal const int ISS_E_BLOCK_SIZE_TOO_SMALL = unchecked((int)0x80131483);
        internal const int ISS_E_CALLER = unchecked((int)0x801314A1);
        internal const int ISS_E_CORRUPTED_STORE_FILE = unchecked((int)0x80131480);
        internal const int ISS_E_CREATE_DIR = unchecked((int)0x80131468);
        internal const int ISS_E_CREATE_MUTEX = unchecked((int)0x80131464);
        internal const int ISS_E_DEPRECATE = unchecked((int)0x801314A0);
        internal const int ISS_E_FILE_NOT_MAPPED = unchecked((int)0x80131482);
        internal const int ISS_E_FILE_WRITE = unchecked((int)0x80131466);
        internal const int ISS_E_GET_FILE_SIZE = unchecked((int)0x80131463);
        internal const int ISS_E_ISOSTORE = unchecked((int)0x80131450);
        internal const int ISS_E_LOCK_FAILED = unchecked((int)0x80131465);
        internal const int ISS_E_MACHINE = unchecked((int)0x801314A3);
        internal const int ISS_E_MACHINE_DACL = unchecked((int)0x801314A4);
        internal const int ISS_E_MAP_VIEW_OF_FILE = unchecked((int)0x80131462);
        internal const int ISS_E_OPEN_FILE_MAPPING = unchecked((int)0x80131461);
        internal const int ISS_E_OPEN_STORE_FILE = unchecked((int)0x80131460);
        internal const int ISS_E_PATH_LENGTH = unchecked((int)0x801314A2);
        internal const int ISS_E_SET_FILE_POINTER = unchecked((int)0x80131467);
        internal const int ISS_E_STORE_NOT_OPEN = unchecked((int)0x80131469);
        internal const int ISS_E_STORE_VERSION = unchecked((int)0x80131481);
        internal const int ISS_E_TABLE_ROW_NOT_FOUND = unchecked((int)0x80131486);
        internal const int ISS_E_USAGE_WILL_EXCEED_QUOTA = unchecked((int)0x80131485);
        internal const int META_E_BAD_SIGNATURE = unchecked((int)0x80131192);
        internal const int META_E_CA_FRIENDS_SN_REQUIRED = unchecked((int)0x801311e6);
        internal const int MSEE_E_ASSEMBLYLOADINPROGRESS = unchecked((int)0x80131016);
        internal const int RO_E_CLOSED = unchecked((int)0x80000013);
        internal const int E_BOUNDS = unchecked((int)0x8000000B);
        internal const int RO_E_METADATA_NAME_NOT_FOUND = unchecked((int)0x8000000F);
        internal const int SECURITY_E_INCOMPATIBLE_EVIDENCE = unchecked((int)0x80131403);
        internal const int SECURITY_E_INCOMPATIBLE_SHARE = unchecked((int)0x80131401);
        internal const int SECURITY_E_UNVERIFIABLE = unchecked((int)0x80131402);
        internal const int STG_E_PATHNOTFOUND = unchecked((int)0x80030003);
        public const int COR_E_DIRECTORYNOTFOUND = unchecked((int)0x80070003);
        public const int COR_E_ENDOFSTREAM = unchecked((int)0x80070026);  // OS defined
        public const int COR_E_FILELOAD = unchecked((int)0x80131621);
        public const int COR_E_FILENOTFOUND = unchecked((int)0x80070002);
        public const int COR_E_IO = unchecked((int)0x80131620);
        public const int COR_E_PATHTOOLONG = unchecked((int)0x800700CE);
    }
}
