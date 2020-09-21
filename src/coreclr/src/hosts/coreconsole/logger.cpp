// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <windows.h>
#include <Logger.h>
#include "palclr.h"

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

Logger& Logger::operator<< (int val) {

    if (m_isEnabled) {
        EnsurePrefixIsPrinted();
		::wprintf(W("%d"), val);
    }

    return *this;
}

#ifdef _MSC_VER
Logger& Logger::operator<< (long val) {
    if (m_isEnabled) {
        EnsurePrefixIsPrinted();
        ::wprintf(W("%d"), val);
    }
    return *this;
}

Logger& Logger::operator<< (unsigned long val) {
    if (m_isEnabled) {
        EnsurePrefixIsPrinted();
        ::wprintf(W("%d"), val);
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

