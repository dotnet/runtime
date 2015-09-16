// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal enum SecurityStatus
    {
        // Success / Informational
        OK = 0x00000000,
        ContinueNeeded = unchecked((int)0x00090312),
        CompleteNeeded = unchecked((int)0x00090313),
        CompAndContinue = unchecked((int)0x00090314),
        ContextExpired = unchecked((int)0x00090317),
        CredentialsNeeded = unchecked((int)0x00090320),
        Renegotiate = unchecked((int)0x00090321),

        // Errors
        OutOfMemory = unchecked((int)0x80090300),
        InvalidHandle = unchecked((int)0x80090301),
        Unsupported = unchecked((int)0x80090302),
        TargetUnknown = unchecked((int)0x80090303),
        InternalError = unchecked((int)0x80090304),
        PackageNotFound = unchecked((int)0x80090305),
        NotOwner = unchecked((int)0x80090306),
        CannotInstall = unchecked((int)0x80090307),
        InvalidToken = unchecked((int)0x80090308),
        CannotPack = unchecked((int)0x80090309),
        QopNotSupported = unchecked((int)0x8009030A),
        NoImpersonation = unchecked((int)0x8009030B),
        LogonDenied = unchecked((int)0x8009030C),
        UnknownCredentials = unchecked((int)0x8009030D),
        NoCredentials = unchecked((int)0x8009030E),
        MessageAltered = unchecked((int)0x8009030F),
        OutOfSequence = unchecked((int)0x80090310),
        NoAuthenticatingAuthority = unchecked((int)0x80090311),
        IncompleteMessage = unchecked((int)0x80090318),
        IncompleteCredentials = unchecked((int)0x80090320),
        BufferNotEnough = unchecked((int)0x80090321),
        WrongPrincipal = unchecked((int)0x80090322),
        TimeSkew = unchecked((int)0x80090324),
        UntrustedRoot = unchecked((int)0x80090325),
        IllegalMessage = unchecked((int)0x80090326),
        CertUnknown = unchecked((int)0x80090327),
        CertExpired = unchecked((int)0x80090328),
        AlgorithmMismatch = unchecked((int)0x80090331),
        SecurityQosFailed = unchecked((int)0x80090332),
        SmartcardLogonRequired = unchecked((int)0x8009033E),
        UnsupportedPreauth = unchecked((int)0x80090343),
        BadBinding = unchecked((int)0x80090346)
    }

#if TRACE_VERBOSE
        internal static string MapSecurityStatus(uint statusCode)
        {
            switch (statusCode)
            {
                case 0: return "0";
                case 0x80090001: return "NTE_BAD_UID";
                case 0x80090002: return "NTE_BAD_HASH";
                case 0x80090003: return "NTE_BAD_KEY";
                case 0x80090004: return "NTE_BAD_LEN";
                case 0x80090005: return "NTE_BAD_DATA";
                case 0x80090006: return "NTE_BAD_SIGNATURE";
                case 0x80090007: return "NTE_BAD_VER";
                case 0x80090008: return "NTE_BAD_ALGID";
                case 0x80090009: return "NTE_BAD_FLAGS";
                case 0x8009000A: return "NTE_BAD_TYPE";
                case 0x8009000B: return "NTE_BAD_KEY_STATE";
                case 0x8009000C: return "NTE_BAD_HASH_STATE";
                case 0x8009000D: return "NTE_NO_KEY";
                case 0x8009000E: return "NTE_NO_MEMORY";
                case 0x8009000F: return "NTE_EXISTS";
                case 0x80090010: return "NTE_PERM";
                case 0x80090011: return "NTE_NOT_FOUND";
                case 0x80090012: return "NTE_DOUBLE_ENCRYPT";
                case 0x80090013: return "NTE_BAD_PROVIDER";
                case 0x80090014: return "NTE_BAD_PROV_TYPE";
                case 0x80090015: return "NTE_BAD_PUBLIC_KEY";
                case 0x80090016: return "NTE_BAD_KEYSET";
                case 0x80090017: return "NTE_PROV_TYPE_NOT_DEF";
                case 0x80090018: return "NTE_PROV_TYPE_ENTRY_BAD";
                case 0x80090019: return "NTE_KEYSET_NOT_DEF";
                case 0x8009001A: return "NTE_KEYSET_ENTRY_BAD";
                case 0x8009001B: return "NTE_PROV_TYPE_NO_MATCH";
                case 0x8009001C: return "NTE_SIGNATURE_FILE_BAD";
                case 0x8009001D: return "NTE_PROVIDER_DLL_FAIL";
                case 0x8009001E: return "NTE_PROV_DLL_NOT_FOUND";
                case 0x8009001F: return "NTE_BAD_KEYSET_PARAM";
                case 0x80090020: return "NTE_FAIL";
                case 0x80090021: return "NTE_SYS_ERR";
                case 0x80090022: return "NTE_SILENT_CONTEXT";
                case 0x80090023: return "NTE_TOKEN_KEYSET_STORAGE_FULL";
                case 0x80090024: return "NTE_TEMPORARY_PROFILE";
                case 0x80090025: return "NTE_FIXEDPARAMETER";
                case 0x80090300: return "SEC_E_INSUFFICIENT_MEMORY";
                case 0x80090301: return "SEC_E_INVALID_HANDLE";
                case 0x80090302: return "SEC_E_UNSUPPORTED_FUNCTION";
                case 0x80090303: return "SEC_E_TARGET_UNKNOWN";
                case 0x80090304: return "SEC_E_INTERNAL_ERROR";
                case 0x80090305: return "SEC_E_SECPKG_NOT_FOUND";
                case 0x80090306: return "SEC_E_NOT_OWNER";
                case 0x80090307: return "SEC_E_CANNOT_INSTALL";
                case 0x80090308: return "SEC_E_INVALID_TOKEN";
                case 0x80090309: return "SEC_E_CANNOT_PACK";
                case 0x8009030A: return "SEC_E_QOP_NOT_SUPPORTED";
                case 0x8009030B: return "SEC_E_NO_IMPERSONATION";
                case 0x8009030C: return "SEC_E_LOGON_DENIED";
                case 0x8009030D: return "SEC_E_UNKNOWN_CREDENTIALS";
                case 0x8009030E: return "SEC_E_NO_CREDENTIALS";
                case 0x8009030F: return "SEC_E_MESSAGE_ALTERED";
                case 0x80090310: return "SEC_E_OUT_OF_SEQUENCE";
                case 0x80090311: return "SEC_E_NO_AUTHENTICATING_AUTHORITY";
                case 0x00090312: return "SEC_I_CONTINUE_NEEDED";
                case 0x00090313: return "SEC_I_COMPLETE_NEEDED";
                case 0x00090314: return "SEC_I_COMPLETE_AND_CONTINUE";
                case 0x00090315: return "SEC_I_LOCAL_LOGON";
                case 0x80090316: return "SEC_E_BAD_PKGID";
                case 0x80090317: return "SEC_E_CONTEXT_EXPIRED";
                case 0x00090317: return "SEC_I_CONTEXT_EXPIRED";
                case 0x80090318: return "SEC_E_INCOMPLETE_MESSAGE";
                case 0x80090320: return "SEC_E_INCOMPLETE_CREDENTIALS";
                case 0x80090321: return "SEC_E_BUFFER_TOO_SMALL";
                case 0x00090320: return "SEC_I_INCOMPLETE_CREDENTIALS";
                case 0x00090321: return "SEC_I_RENEGOTIATE";
                case 0x80090322: return "SEC_E_WRONG_PRINCIPAL";
                case 0x00090323: return "SEC_I_NO_LSA_CONTEXT";
                case 0x80090324: return "SEC_E_TIME_SKEW";
                case 0x80090325: return "SEC_E_UNTRUSTED_ROOT";
                case 0x80090326: return "SEC_E_ILLEGAL_MESSAGE";
                case 0x80090327: return "SEC_E_CERT_UNKNOWN";
                case 0x80090328: return "SEC_E_CERT_EXPIRED";
                case 0x80090329: return "SEC_E_ENCRYPT_FAILURE";
                case 0x80090330: return "SEC_E_DECRYPT_FAILURE";
                case 0x80090331: return "SEC_E_ALGORITHM_MISMATCH";
                case 0x80090332: return "SEC_E_SECURITY_QOS_FAILED";
                case 0x80090333: return "SEC_E_UNFINISHED_CONTEXT_DELETED";
                case 0x80090334: return "SEC_E_NO_TGT_REPLY";
                case 0x80090335: return "SEC_E_NO_IP_ADDRESSES";
                case 0x80090336: return "SEC_E_WRONG_CREDENTIAL_HANDLE";
                case 0x80090337: return "SEC_E_CRYPTO_SYSTEM_INVALID";
                case 0x80090338: return "SEC_E_MAX_REFERRALS_EXCEEDED";
                case 0x80090339: return "SEC_E_MUST_BE_KDC";
                case 0x8009033A: return "SEC_E_STRONG_CRYPTO_NOT_SUPPORTED";
                case 0x8009033B: return "SEC_E_TOO_MANY_PRINCIPALS";
                case 0x8009033C: return "SEC_E_NO_PA_DATA";
                case 0x8009033D: return "SEC_E_PKINIT_NAME_MISMATCH";
                case 0x8009033E: return "SEC_E_SMARTCARD_LOGON_REQUIRED";
                case 0x8009033F: return "SEC_E_SHUTDOWN_IN_PROGRESS";
                case 0x80090340: return "SEC_E_KDC_INVALID_REQUEST";
                case 0x80090341: return "SEC_E_KDC_UNABLE_TO_REFER";
                case 0x80090342: return "SEC_E_KDC_UNKNOWN_ETYPE";
                case 0x80090343: return "SEC_E_UNSUPPORTED_PREAUTH";
                case 0x80090345: return "SEC_E_DELEGATION_REQUIRED";
                case 0x80090346: return "SEC_E_BAD_BINDINGS";
                case 0x80090347: return "SEC_E_MULTIPLE_ACCOUNTS";
                case 0x80090348: return "SEC_E_NO_KERB_KEY";
                case 0x80091001: return "CRYPT_E_MSG_ERROR";
                case 0x80091002: return "CRYPT_E_UNKNOWN_ALGO";
                case 0x80091003: return "CRYPT_E_OID_FORMAT";
                case 0x80091004: return "CRYPT_E_INVALID_MSG_TYPE";
                case 0x80091005: return "CRYPT_E_UNEXPECTED_ENCODING";
                case 0x80091006: return "CRYPT_E_AUTH_ATTR_MISSING";
                case 0x80091007: return "CRYPT_E_HASH_VALUE";
                case 0x80091008: return "CRYPT_E_INVALID_INDEX";
                case 0x80091009: return "CRYPT_E_ALREADY_DECRYPTED";
                case 0x8009100A: return "CRYPT_E_NOT_DECRYPTED";
                case 0x8009100B: return "CRYPT_E_RECIPIENT_NOT_FOUND";
                case 0x8009100C: return "CRYPT_E_CONTROL_TYPE";
                case 0x8009100D: return "CRYPT_E_ISSUER_SERIALNUMBER";
                case 0x8009100E: return "CRYPT_E_SIGNER_NOT_FOUND";
                case 0x8009100F: return "CRYPT_E_ATTRIBUTES_MISSING";
                case 0x80091010: return "CRYPT_E_STREAM_MSG_NOT_READY";
                case 0x80091011: return "CRYPT_E_STREAM_INSUFFICIENT_DATA";
                case 0x00091012: return "CRYPT_I_NEW_PROTECTION_REQUIRED";
                case 0x80092001: return "CRYPT_E_BAD_LEN";
                case 0x80092002: return "CRYPT_E_BAD_ENCODE";
                case 0x80092003: return "CRYPT_E_FILE_ERROR";
                case 0x80092004: return "CRYPT_E_NOT_FOUND";
                case 0x80092005: return "CRYPT_E_EXISTS";
                case 0x80092006: return "CRYPT_E_NO_PROVIDER";
                case 0x80092007: return "CRYPT_E_SELF_SIGNED";
                case 0x80092008: return "CRYPT_E_DELETED_PREV";
                case 0x80092009: return "CRYPT_E_NO_MATCH";
                case 0x8009200A: return "CRYPT_E_UNEXPECTED_MSG_TYPE";
                case 0x8009200B: return "CRYPT_E_NO_KEY_PROPERTY";
                case 0x8009200C: return "CRYPT_E_NO_DECRYPT_CERT";
                case 0x8009200D: return "CRYPT_E_BAD_MSG";
                case 0x8009200E: return "CRYPT_E_NO_SIGNER";
                case 0x8009200F: return "CRYPT_E_PENDING_CLOSE";
                case 0x80092010: return "CRYPT_E_REVOKED";
                case 0x80092011: return "CRYPT_E_NO_REVOCATION_DLL";
                case 0x80092012: return "CRYPT_E_NO_REVOCATION_CHECK";
                case 0x80092013: return "CRYPT_E_REVOCATION_OFFLINE";
                case 0x80092014: return "CRYPT_E_NOT_IN_REVOCATION_DATABASE";
                case 0x80092020: return "CRYPT_E_INVALID_NUMERIC_STRING";
                case 0x80092021: return "CRYPT_E_INVALID_PRINTABLE_STRING";
                case 0x80092022: return "CRYPT_E_INVALID_IA5_STRING";
                case 0x80092023: return "CRYPT_E_INVALID_X500_STRING";
                case 0x80092024: return "CRYPT_E_NOT_CHAR_STRING";
                case 0x80092025: return "CRYPT_E_FILERESIZED";
                case 0x80092026: return "CRYPT_E_SECURITY_SETTINGS";
                case 0x80092027: return "CRYPT_E_NO_VERIFY_USAGE_DLL";
                case 0x80092028: return "CRYPT_E_NO_VERIFY_USAGE_CHECK";
                case 0x80092029: return "CRYPT_E_VERIFY_USAGE_OFFLINE";
                case 0x8009202A: return "CRYPT_E_NOT_IN_CTL";
                case 0x8009202B: return "CRYPT_E_NO_TRUSTED_SIGNER";
                case 0x8009202C: return "CRYPT_E_MISSING_PUBKEY_PARA";
                case 0x80093000: return "CRYPT_E_OSS_ERROR";
                case 0x80093001: return "OSS_MORE_BUF";
                case 0x80093002: return "OSS_NEGATIVE_UINTEGER";
                case 0x80093003: return "OSS_PDU_RANGE";
                case 0x80093004: return "OSS_MORE_INPUT";
                case 0x80093005: return "OSS_DATA_ERROR";
                case 0x80093006: return "OSS_BAD_ARG";
                case 0x80093007: return "OSS_BAD_VERSION";
                case 0x80093008: return "OSS_OUT_MEMORY";
                case 0x80093009: return "OSS_PDU_MISMATCH";
                case 0x8009300A: return "OSS_LIMITED";
                case 0x8009300B: return "OSS_BAD_PTR";
                case 0x8009300C: return "OSS_BAD_TIME";
                case 0x8009300D: return "OSS_INDEFINITE_NOT_SUPPORTED";
                case 0x8009300E: return "OSS_MEM_ERROR";
                case 0x8009300F: return "OSS_BAD_TABLE";
                case 0x80093010: return "OSS_TOO_LONG";
                case 0x80093011: return "OSS_CONSTRAINT_VIOLATED";
                case 0x80093012: return "OSS_FATAL_ERROR";
                case 0x80093013: return "OSS_ACCESS_SERIALIZATION_ERROR";
                case 0x80093014: return "OSS_NULL_TBL";
                case 0x80093015: return "OSS_NULL_FCN";
                case 0x80093016: return "OSS_BAD_ENCRULES";
                case 0x80093017: return "OSS_UNAVAIL_ENCRULES";
                case 0x80093018: return "OSS_CANT_OPEN_TRACE_WINDOW";
                case 0x80093019: return "OSS_UNIMPLEMENTED";
                case 0x8009301A: return "OSS_OID_DLL_NOT_LINKED";
                case 0x8009301B: return "OSS_CANT_OPEN_TRACE_FILE";
                case 0x8009301C: return "OSS_TRACE_FILE_ALREADY_OPEN";
                case 0x8009301D: return "OSS_TABLE_MISMATCH";
                case 0x8009301E: return "OSS_TYPE_NOT_SUPPORTED";
                case 0x8009301F: return "OSS_REAL_DLL_NOT_LINKED";
                case 0x80093020: return "OSS_REAL_CODE_NOT_LINKED";
                case 0x80093021: return "OSS_OUT_OF_RANGE";
                case 0x80093022: return "OSS_COPIER_DLL_NOT_LINKED";
                case 0x80093023: return "OSS_CONSTRAINT_DLL_NOT_LINKED";
                case 0x80093024: return "OSS_COMPARATOR_DLL_NOT_LINKED";
                case 0x80093025: return "OSS_COMPARATOR_CODE_NOT_LINKED";
                case 0x80093026: return "OSS_MEM_MGR_DLL_NOT_LINKED";
                case 0x80093027: return "OSS_PDV_DLL_NOT_LINKED";
                case 0x80093028: return "OSS_PDV_CODE_NOT_LINKED";
                case 0x80093029: return "OSS_API_DLL_NOT_LINKED";
                case 0x8009302A: return "OSS_BERDER_DLL_NOT_LINKED";
                case 0x8009302B: return "OSS_PER_DLL_NOT_LINKED";
                case 0x8009302C: return "OSS_OPEN_TYPE_ERROR";
                case 0x8009302D: return "OSS_MUTEX_NOT_CREATED";
                case 0x8009302E: return "OSS_CANT_CLOSE_TRACE_FILE";
                case 0x80093100: return "CRYPT_E_ASN1_ERROR";
                case 0x80093101: return "CRYPT_E_ASN1_INTERNAL";
                case 0x80093102: return "CRYPT_E_ASN1_EOD";
                case 0x80093103: return "CRYPT_E_ASN1_CORRUPT";
                case 0x80093104: return "CRYPT_E_ASN1_LARGE";
                case 0x80093105: return "CRYPT_E_ASN1_CONSTRAINT";
                case 0x80093106: return "CRYPT_E_ASN1_MEMORY";
                case 0x80093107: return "CRYPT_E_ASN1_OVERFLOW";
                case 0x80093108: return "CRYPT_E_ASN1_BADPDU";
                case 0x80093109: return "CRYPT_E_ASN1_BADARGS";
                case 0x8009310A: return "CRYPT_E_ASN1_BADREAL";
                case 0x8009310B: return "CRYPT_E_ASN1_BADTAG";
                case 0x8009310C: return "CRYPT_E_ASN1_CHOICE";
                case 0x8009310D: return "CRYPT_E_ASN1_RULE";
                case 0x8009310E: return "CRYPT_E_ASN1_UTF8";
                case 0x80093133: return "CRYPT_E_ASN1_PDU_TYPE";
                case 0x80093134: return "CRYPT_E_ASN1_NYI";
                case 0x80093201: return "CRYPT_E_ASN1_EXTENDED";
                case 0x80093202: return "CRYPT_E_ASN1_NOEOD";
                case 0x80094001: return "CERTSRV_E_BAD_REQUESTSUBJECT";
                case 0x80094002: return "CERTSRV_E_NO_REQUEST";
                case 0x80094003: return "CERTSRV_E_BAD_REQUESTSTATUS";
                case 0x80094004: return "CERTSRV_E_PROPERTY_EMPTY";
                case 0x80094005: return "CERTSRV_E_INVALID_CA_CERTIFICATE";
                case 0x80094006: return "CERTSRV_E_SERVER_SUSPENDED";
                case 0x80094007: return "CERTSRV_E_ENCODING_LENGTH";
                case 0x80094008: return "CERTSRV_E_ROLECONFLICT";
                case 0x80094009: return "CERTSRV_E_RESTRICTEDOFFICER";
                case 0x8009400A: return "CERTSRV_E_KEY_ARCHIVAL_NOT_CONFIGURED";
                case 0x8009400B: return "CERTSRV_E_NO_VALID_KRA";
                case 0x8009400C: return "CERTSRV_E_BAD_REQUEST_KEY_ARCHIVAL";
                case 0x80094800: return "CERTSRV_E_UNSUPPORTED_CERT_TYPE";
                case 0x80094801: return "CERTSRV_E_NO_CERT_TYPE";
                case 0x80094802: return "CERTSRV_E_TEMPLATE_CONFLICT";
                case 0x80096001: return "TRUST_E_SYSTEM_ERROR";
                case 0x80096002: return "TRUST_E_NO_SIGNER_CERT";
                case 0x80096003: return "TRUST_E_COUNTER_SIGNER";
                case 0x80096004: return "TRUST_E_CERT_SIGNATURE";
                case 0x80096005: return "TRUST_E_TIME_STAMP";
                case 0x80096010: return "TRUST_E_BAD_DIGEST";
                case 0x80096019: return "TRUST_E_BASIC_CONSTRAINTS";
                case 0x8009601E: return "TRUST_E_FINANCIAL_CRITERIA";
                case 0x80097001: return "MSSIPOTF_E_OUTOFMEMRANGE";
                case 0x80097002: return "MSSIPOTF_E_CANTGETOBJECT";
                case 0x80097003: return "MSSIPOTF_E_NOHEADTABLE";
                case 0x80097004: return "MSSIPOTF_E_BAD_MAGICNUMBER";
                case 0x80097005: return "MSSIPOTF_E_BAD_OFFSET_TABLE";
                case 0x80097006: return "MSSIPOTF_E_TABLE_TAGORDER";
                case 0x80097007: return "MSSIPOTF_E_TABLE_LONGWORD";
                case 0x80097008: return "MSSIPOTF_E_BAD_FIRST_TABLE_PLACEMENT";
                case 0x80097009: return "MSSIPOTF_E_TABLES_OVERLAP";
                case 0x8009700A: return "MSSIPOTF_E_TABLE_PADBYTES";
                case 0x8009700B: return "MSSIPOTF_E_FILETOOSMALL";
                case 0x8009700C: return "MSSIPOTF_E_TABLE_CHECKSUM";
                case 0x8009700D: return "MSSIPOTF_E_FILE_CHECKSUM";
                case 0x80097010: return "MSSIPOTF_E_FAILED_POLICY";
                case 0x80097011: return "MSSIPOTF_E_FAILED_HINTS_CHECK";
                case 0x80097012: return "MSSIPOTF_E_NOT_OPENTYPE";
                case 0x80097013: return "MSSIPOTF_E_FILE";
                case 0x80097014: return "MSSIPOTF_E_CRYPT";
                case 0x80097015: return "MSSIPOTF_E_BADVERSION";
                case 0x80097016: return "MSSIPOTF_E_DSIG_STRUCTURE";
                case 0x80097017: return "MSSIPOTF_E_PCONST_CHECK";
                case 0x80097018: return "MSSIPOTF_E_STRUCTURE";
                case 0x800B0001: return "TRUST_E_PROVIDER_UNKNOWN";
                case 0x800B0002: return "TRUST_E_ACTION_UNKNOWN";
                case 0x800B0003: return "TRUST_E_SUBJECT_FORM_UNKNOWN";
                case 0x800B0004: return "TRUST_E_SUBJECT_NOT_TRUSTED";
                case 0x800B0005: return "DIGSIG_E_ENCODE";
                case 0x800B0006: return "DIGSIG_E_DECODE";
                case 0x800B0007: return "DIGSIG_E_EXTENSIBILITY";
                case 0x800B0008: return "DIGSIG_E_CRYPTO";
                case 0x800B0009: return "PERSIST_E_SIZEDEFINITE";
                case 0x800B000A: return "PERSIST_E_SIZEINDEFINITE";
                case 0x800B000B: return "PERSIST_E_NOTSELFSIZING";
                case 0x800B0100: return "TRUST_E_NOSIGNATURE";
                case 0x800B0101: return "CERT_E_EXPIRED";
                case 0x800B0102: return "CERT_E_VALIDITYPERIODNESTING";
                case 0x800B0103: return "CERT_E_ROLE";
                case 0x800B0104: return "CERT_E_PATHLENCONST";
                case 0x800B0105: return "CERT_E_CRITICAL";
                case 0x800B0106: return "CERT_E_PURPOSE";
                case 0x800B0107: return "CERT_E_ISSUERCHAINING";
                case 0x800B0108: return "CERT_E_MALFORMED";
                case 0x800B0109: return "CERT_E_UNTRUSTEDROOT";
                case 0x800B010A: return "CERT_E_CHAINING";
                case 0x800B010B: return "TRUST_E_FAIL";
                case 0x800B010C: return "CERT_E_REVOKED";
                case 0x800B010D: return "CERT_E_UNTRUSTEDTESTROOT";
                case 0x800B010E: return "CERT_E_REVOCATION_FAILURE";
                case 0x800B010F: return "CERT_E_CN_NO_MATCH";
                case 0x800B0110: return "CERT_E_WRONG_USAGE";
                case 0x800B0111: return "TRUST_E_EXPLICIT_DISTRUST";
                case 0x800B0112: return "CERT_E_UNTRUSTEDCA";
                case 0x800B0113: return "CERT_E_INVALID_POLICY";
                case 0x800B0114: return "CERT_E_INVALID_NAME";
            }

            return string.Format("0x{0:x} [{1}]", statusCode, statusCode);
        }
#endif // TRACE_VERBOSE
}
