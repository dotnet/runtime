// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "inproccrashreporter.h"
#include "signalsafeformatter.h"

#include <stddef.h>
#include <stdint.h>

// Manages the on-disk lifecycle of in-proc crash reports: at startup it
// establishes the managed report directory and prunes stale and over-retention
// reports; on a crash it hands out a uniquely named temp report file and
// finalizes it. Composed alongside (not derived from) the signal-safe writer
// family that emits the report contents.
//
// Members run in one of two execution contexts:
//   * Initialization path -- runs once at process startup. May allocate and call
//     libc/filesystem APIs; NOT async-signal-safe. This is the default contract
//     for members of this class.
//   * Crash/signal path -- invoked from the crash signal handler. Must be
//     async-signal-safe and allocation-free. Only these members run there:
//     IsReportFileOutputEnabled, PrepareReportFile, FinishReportFile,
//     BuildReportPaths, and the shared AppendPathComponent.
//     Each is marked accordingly below.
class InProcCrashReportLifecycle
{
public:
    InProcCrashReportLifecycle() = default;
    InProcCrashReportLifecycle(const InProcCrashReportLifecycle&) = delete;
    InProcCrashReportLifecycle& operator=(const InProcCrashReportLifecycle&) = delete;

    // Prepares lifecycle-managed storage for crash reports under rootPath,
    // keeping at most maxFileCount completed reports. Runs at startup (not the
    // crash path) and may allocate. Returns false if storage could not be
    // initialized.
    bool Initialize(
        const char* rootPath,
        int32_t maxFileCount);

    // Returns whether lifecycle-managed report files should be written. False when
    // initialization failed. Crash/signal-path safe: reads one bool.
    bool IsReportFileOutputEnabled() const { return m_reportFileOutputEnabled; }

    // Opens a uniquely named temporary report file under the managed directory,
    // returning its path and an open fd. Deletes the cached over-retention report
    // first. Runs on the crash/signal path: allocation-free and signal-safe.
    // Returns false if no file could be opened.
    bool PrepareReportFile(
        SignalSafeFormatter* formatter,
        char* reportFilePath,
        size_t reportFilePathSize,
        int* fd);

    // Finalizes the report opened by PrepareReportFile: on success renames the temp
    // file to its final reportFilePath, otherwise removes the temp file. Runs on the
    // crash/signal path: allocation-free and signal-safe.
    void FinishReportFile(
        bool succeeded,
        const char* reportFilePath);

private:
    struct ReportPath
    {
        char value[CRASHREPORT_PATH_BUFFER_SIZE];
    };

    struct FileInfo
    {
        uint64_t timestamp;
        uint64_t pid;
        ReportPath path;
    };

    // Resolves rootPath into m_reportDirectory, creating the managed directory
    // tree and verifying it is writable. Initialization path; logs and
    // returns false on failure.
    bool EstablishReportDirectory(
        const char* rootPath);

    // Scans m_reportDirectory, removing stale temp files and retaining only the
    // newest maxFileCount completed reports (unlinking older ones inline). When
    // the directory is already at the bound, caches the oldest retained report so
    // the crash path can unlink it before publishing a new one. Initialization
    // path; may allocate. Logs and returns false on failure.
    bool PruneExistingReports(int32_t maxFileCount);

    // Returns the index of the oldest report in reports per CompareFileInfo.
    static size_t FindOldestReportIndex(
        const FileInfo* reports,
        size_t reportCount);

    // Builds the final and temporary report paths from the timestamp/pid
    // into the caller-provided buffers. Crash-path, allocation-free.
    bool BuildReportPaths(
        SignalSafeFormatter* formatter,
        char* reportFilePath,
        size_t reportFilePathSize,
        char* tempReportFilePath,
        size_t tempReportFilePathSize,
        uint64_t timestamp,
        uint32_t pid);

    // Appends component as a new path segment (inserting a single '/' separator as
    // needed). Allocation-free; used by both the init and crash paths.
    static bool AppendPathComponent(
        char* buffer,
        size_t bufferSize,
        size_t* pos,
        const char* component);

    // Returns whether path is absolute (begins with '/'). Initialization path.
    static bool IsAbsolutePath(const char* path);

    // Copies rootPath into buffer, requiring it to already be an absolute path;
    // the runtime does not expand a leading '~' or environment variables.
    static bool ResolveRootPath(
        char* buffer,
        size_t bufferSize,
        const char* rootPath);

    // Creates the directory if missing; succeeds if it already exists as a directory.
    static bool EnsureDirectory(const char* path);

    // Verifies the directory permits create, rename, and delete by exercising a
    // hidden probe file (rename is the primitive FinishReportFile uses to publish
    // a completed report). Runs only at initialization, so the probe paths live in
    // local stack buffers.
    bool ProbeDirectoryWritable(const char* path);

    // Parses a managed report file name (report-<timestamp>-<pid><ext>),
    // accepting either a completed report or the in-progress .tmp form, into info.
    // Reports which form matched through isTempExtension.
    static bool TryParseReportName(
        const char* name,
        FileInfo* info,
        bool* isTempExtension);

    // Comparator ordering reports oldest-first (timestamp, then path).
    static int CompareFileInfo(
        const void* left,
        const void* right);

    char m_reportDirectory[CRASHREPORT_PATH_BUFFER_SIZE] = {};
    char m_tempReportFilePath[CRASHREPORT_PATH_BUFFER_SIZE] = {};
    ReportPath m_cachedOldestReport = {};
    bool m_reportFileOutputEnabled = false;
};
