// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include <stdio.h>
#include <windows.h>
#include <Logger.h>
#include "palclr.h"
#include "sstring.h"

void Logger::Enable() {
    m_isEnabled = true;
}

void Logger::Disable() {
    m_isEnabled = false;
}

void print(const wchar_t *val) {
    // If val is longer than 2048 characters, wprintf will refuse to print it.
    // So write it in chunks.

    const size_t chunkSize = 1024;

    wchar_t chunk[chunkSize];

    auto valLength = ::wcslen(val);

    for (size_t i = 0 ; i < valLength ; i += chunkSize) {

        ::wcsncpy_s(chunk, chunkSize, val + i, _TRUNCATE);

        ::wprintf(W("%s"), chunk);
    }
}

Logger& Logger::operator<< (bool val) {
    if (m_isEnabled) {
        if (val) {
            EnsurePrefixIsPrinted();
            print(W("true"));
        } else {
            EnsurePrefixIsPrinted();
            print(W("false"));
        }
    }
    return *this;
}
void PrintAsHResult(int val) {
    const wchar_t * str = nullptr;

    switch (val) {
    case 0x00000000: str = W("S_OK"); break;
    case 0x00000001: str = W("S_FALSE"); break;
    case 0x8000000B: str = W("E_BOUNDS"); break;
    case 0x8000000C: str = W("E_CHANGED_STATE"); break;
    case 0x80000013: str = W("RO_E_CLOSED"); break;
    case 0x8000211D: str = W("COR_E_AMBIGUOUSMATCH"); break;
    case 0x80004001: str = W("E_NOTIMPL"); break;
    case 0x80004002: str = W("COR_E_INVALIDCAST"); break;
        //case 0x80004002: str = W("E_NOINTERFACE"); break;
    case 0x80004003: str = W("COR_E_NULLREFERENCE"); break;
        //case 0x80004003: str = W("E_POINTER"); break;
    case 0x80004004: str = W("E_ABORT"); break;
    case 0x80004005: str = W("E_FAIL"); break;
    case 0x8000FFFF: str = W("E_UNEXPECTED"); break;
    case 0x8002000a: str = W("DISP_E_OVERFLOW"); break;
    case 0x8002000e: str = W("COR_E_TARGETPARAMCOUNT"); break;
    case 0x80020012: str = W("COR_E_DIVIDEBYZERO"); break;
    case 0x80028ca0: str = W("TYPE_E_TYPEMISMATCH"); break;
    case 0x80070005: str = W("COR_E_UNAUTHORIZEDACCESS"); break;
        //case 0x80070005: str = W("E_ACCESSDENIED"); break;
    case 0x80070006: str = W("E_HANDLE"); break;
    case 0x8007000B: str = W("COR_E_BADIMAGEFORMAT"); break;
    case 0x8007000E: str = W("COR_E_OUTOFMEMORY"); break;
        //case 0x8007000E: str = W("E_OUTOFMEMORY"); break;
    case 0x80070057: str = W("COR_E_ARGUMENT"); break;
        //case 0x80070057: str = W("E_INVALIDARG"); break;
    case 0x80070216: str = W("COR_E_ARITHMETIC"); break;
    case 0x800703E9: str = W("COR_E_STACKOVERFLOW"); break;
    case 0x80090020: str = W("NTE_FAIL"); break;
    case 0x80131013: str = W("COR_E_TYPEUNLOADED"); break;
    case 0x80131014: str = W("COR_E_APPDOMAINUNLOADED"); break;
    case 0x80131015: str = W("COR_E_CANNOTUNLOADAPPDOMAIN"); break;
    case 0x80131040: str = W("FUSION_E_REF_DEF_MISMATCH"); break;
    case 0x80131047: str = W("FUSION_E_INVALID_NAME"); break;
    case 0x80131416: str = W("CORSEC_E_POLICY_EXCEPTION"); break;
    case 0x80131417: str = W("CORSEC_E_MIN_GRANT_FAIL"); break;
    case 0x80131418: str = W("CORSEC_E_NO_EXEC_PERM"); break;
        //case 0x80131419: str = W("CORSEC_E_XMLSYNTAX"); break;
    case 0x80131430: str = W("CORSEC_E_CRYPTO"); break;
    case 0x80131431: str = W("CORSEC_E_CRYPTO_UNEX_OPER"); break;
    case 0x80131500: str = W("COR_E_EXCEPTION"); break;
    case 0x80131501: str = W("COR_E_SYSTEM"); break;
    case 0x80131502: str = W("COR_E_ARGUMENTOUTOFRANGE"); break;
    case 0x80131503: str = W("COR_E_ARRAYTYPEMISMATCH"); break;
    case 0x80131504: str = W("COR_E_CONTEXTMARSHAL"); break;
    case 0x80131505: str = W("COR_E_TIMEOUT"); break;
    case 0x80131506: str = W("COR_E_EXECUTIONENGINE"); break;
    case 0x80131507: str = W("COR_E_FIELDACCESS"); break;
    case 0x80131508: str = W("COR_E_INDEXOUTOFRANGE"); break;
    case 0x80131509: str = W("COR_E_INVALIDOPERATION"); break;
    case 0x8013150A: str = W("COR_E_SECURITY"); break;
    case 0x8013150C: str = W("COR_E_SERIALIZATION"); break;
    case 0x8013150D: str = W("COR_E_VERIFICATION"); break;
    case 0x80131510: str = W("COR_E_METHODACCESS"); break;
    case 0x80131511: str = W("COR_E_MISSINGFIELD"); break;
    case 0x80131512: str = W("COR_E_MISSINGMEMBER"); break;
    case 0x80131513: str = W("COR_E_MISSINGMETHOD"); break;
    case 0x80131514: str = W("COR_E_MULTICASTNOTSUPPORTED"); break;
    case 0x80131515: str = W("COR_E_NOTSUPPORTED"); break;
    case 0x80131516: str = W("COR_E_OVERFLOW"); break;
    case 0x80131517: str = W("COR_E_RANK"); break;
    case 0x80131518: str = W("COR_E_SYNCHRONIZATIONLOCK"); break;
    case 0x80131519: str = W("COR_E_THREADINTERRUPTED"); break;
    case 0x8013151A: str = W("COR_E_MEMBERACCESS"); break;
    case 0x80131520: str = W("COR_E_THREADSTATE"); break;
    case 0x80131521: str = W("COR_E_THREADSTOP"); break;
    case 0x80131522: str = W("COR_E_TYPELOAD"); break;
    case 0x80131523: str = W("COR_E_ENTRYPOINTNOTFOUND"); break;
        //case 0x80131523: str = W("COR_E_UNSUPPORTEDFORMAT"); break;
    case 0x80131524: str = W("COR_E_DLLNOTFOUND"); break;
    case 0x80131525: str = W("COR_E_THREADSTART"); break;
    case 0x80131527: str = W("COR_E_INVALIDCOMOBJECT"); break;
    case 0x80131528: str = W("COR_E_NOTFINITENUMBER"); break;
    case 0x80131529: str = W("COR_E_DUPLICATEWAITOBJECT"); break;
    case 0x8013152B: str = W("COR_E_SEMAPHOREFULL"); break;
    case 0x8013152C: str = W("COR_E_WAITHANDLECANNOTBEOPENED"); break;
    case 0x8013152D: str = W("COR_E_ABANDONEDMUTEX"); break;
    case 0x80131530: str = W("COR_E_THREADABORTED"); break;
    case 0x80131531: str = W("COR_E_INVALIDOLEVARIANTTYPE"); break;
    case 0x80131532: str = W("COR_E_MISSINGMANIFESTRESOURCE"); break;
    case 0x80131533: str = W("COR_E_SAFEARRAYTYPEMISMATCH"); break;
    case 0x80131534: str = W("COR_E_TYPEINITIALIZATION"); break;
    case 0x80131535: str = W("COR_E_COMEMULATE"); break;
        //case 0x80131535: str = W("COR_E_MARSHALDIRECTIVE"); break;
    case 0x80131536: str = W("COR_E_MISSINGSATELLITEASSEMBLY"); break;
    case 0x80131537: str = W("COR_E_FORMAT"); break;
    case 0x80131538: str = W("COR_E_SAFEARRAYRANKMISMATCH"); break;
    case 0x80131539: str = W("COR_E_PLATFORMNOTSUPPORTED"); break;
    case 0x8013153A: str = W("COR_E_INVALIDPROGRAM"); break;
    case 0x8013153B: str = W("COR_E_OPERATIONCANCELED"); break;
    case 0x8013153D: str = W("COR_E_INSUFFICIENTMEMORY"); break;
    case 0x8013153E: str = W("COR_E_RUNTIMEWRAPPED"); break;
    case 0x80131541: str = W("COR_E_DATAMISALIGNED"); break;
    case 0x80131543: str = W("COR_E_TYPEACCESS"); break;
    case 0x80131577: str = W("COR_E_KEYNOTFOUND"); break;
    case 0x80131578: str = W("COR_E_INSUFFICIENTEXECUTIONSTACK"); break;
    case 0x80131600: str = W("COR_E_APPLICATION"); break;
    case 0x80131601: str = W("COR_E_INVALIDFILTERCRITERIA"); break;
    case 0x80131602: str = W("COR_E_REFLECTIONTYPELOAD   "); break;
    case 0x80131603: str = W("COR_E_TARGET"); break;
    case 0x80131604: str = W("COR_E_TARGETINVOCATION"); break;
    case 0x80131605: str = W("COR_E_CUSTOMATTRIBUTEFORMAT"); break;
    case 0x80131622: str = W("COR_E_OBJECTDISPOSED"); break;
    case 0x80131623: str = W("COR_E_SAFEHANDLEMISSINGATTRIBUTE"); break;
    case 0x80131640: str = W("COR_E_HOSTPROTECTION"); break;
    }

    ::wprintf(W("0x%x"), val);

    if (str != nullptr) {
        ::wprintf(W("/%0s"), str);
    }

}

Logger& Logger::operator<< (int val) {

    if (m_isEnabled) {

        EnsurePrefixIsPrinted();

        if (m_formatHRESULT) {

            PrintAsHResult(val);
            m_formatHRESULT = false;

        } else {

            ::wprintf(W("%d"), val);

        }
    }

    return *this;
}

#ifdef _MSC_VER
Logger& Logger::operator<< (long val) {
    if (m_isEnabled) {
        EnsurePrefixIsPrinted();

        if (m_formatHRESULT) {

            PrintAsHResult(val);
            m_formatHRESULT = false;

        } else {

            ::wprintf(W("%d"), val);

        }
    }
    return *this;
}

Logger& Logger::operator<< (unsigned long val) {
    if (m_isEnabled) {
        EnsurePrefixIsPrinted();

        if (m_formatHRESULT) {

            PrintAsHResult(val);
            m_formatHRESULT = false;

        } else {

            ::wprintf(W("%d"), val);

        }
    }
    return *this;
}
#endif

Logger& Logger::operator<< (const wchar_t *val) {
    if (m_isEnabled) {
        EnsurePrefixIsPrinted();
        print(val);
    }
    return *this;
}

Logger& Logger::operator<< (const char *val) {
    if (m_isEnabled) {
        EnsurePrefixIsPrinted();

        SString valUTF8(SString::Utf8Literal, val);
        SmallStackSString valUnicode;
        valUTF8.ConvertToUnicode(valUnicode);

        print(valUnicode);
    }
    return *this;
}

Logger& Logger::operator<< (Logger& ( *pf )(Logger&)) {
    if (m_isEnabled) {
        return pf(*this);
    } else {
        return *this;
    }
}

void Logger::EnsurePrefixIsPrinted() {
    if (this->m_isEnabled && this->m_prefixRequired) {
        print(W(" HOSTLOG: "));
        m_prefixRequired = false;
    }
}

// Manipulators

// Newline
Logger& Logger::endl (Logger& log) {
    if (log.m_isEnabled) {
        log.EnsurePrefixIsPrinted();
        print(W("\r\n"));
        log.m_prefixRequired = true;
        log.m_formatHRESULT = false;
    }
    return log;
}

// Format the next integer value as an HResult
Logger& Logger::hresult (Logger& log) {
    log.m_formatHRESULT = true;
    return log;
}

