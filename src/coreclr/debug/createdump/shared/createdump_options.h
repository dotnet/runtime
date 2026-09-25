// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CREATEDUMP_OPTIONS_H
#define CREATEDUMP_OPTIONS_H

#include <stdint.h>

enum class DumpType
{
    Mini,
    Heap,
    Triage,
    Full
};

enum class AppModelType
{
    Normal,
    SingleFile,
    NativeAOT
};

typedef struct
{
    const char* DumpPathTemplate;
    const char* LogFilePath;

    enum DumpType DumpType;
    enum AppModelType AppModel;

    bool CreateDump;
    bool CrashReport;
    bool Diagnostics;
    bool Verbose;

    int Pid;
    int CrashThread;
    int Signal;
    int SignalCode;
    int SignalErrno;
    uint64_t SignalAddress;
    uint64_t ExceptionRecord;
} CreateDumpOptions;

inline const char*
GetDumpTypeString(DumpType dumpType)
{
    switch (dumpType)
    {
        case DumpType::Mini:
            return "minidump";
        case DumpType::Heap:
            return "minidump with heap";
        case DumpType::Triage:
            return "triage minidump";
        case DumpType::Full:
            return "full dump";
        default:
            return "unknown";
    }
}

#endif // CREATEDUMP_OPTIONS_H