// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "inproccrashreporter.h"
#include "signalsafeformatter.h"

#include <stddef.h>
#include <stdint.h>

// Manages the on-disk lifecycle of in-proc crash reports: at startup it
// establishes the managed report directory, prunes stale and over-retention
// reports, and selects deletion candidates; on a crash it hands out a uniquely
// named temp report file and finalizes it. Composed alongside (not derived from)
// the signal-safe writer family that emits the report contents.
//
// Members run in one of two execution contexts:
//   * Initialization path -- runs once at process startup. May allocate and call
//     libc/filesystem APIs; NOT async-signal-safe. This is the default contract
//     for members of this class.
//   * Crash/signal path -- invoked from the crash signal handler. Must be
//     async-signal-safe and allocation-free. Only these members run there:
//     IsReportFileOutputEnabled, PrepareReportFile, FinishReportFile,
//     BuildReportPaths, DeleteCandidates, and the shared AppendPathComponent.
//     Each is marked accordingly below.
class InProcCrashReportLifecycle
{
public:
    InProcCrashReportLifecycle() = default;
    ~InProcCrashReportLifecycle();
    InProcCrashReportLifecycle(const InProcCrashReportLifecycle&) = delete;
    InProcCrashReportLifecycle& operator=(const InProcCrashReportLifecycle&) = delete;

    // Prepares lifecycle-managed storage for crash reports under rootPath for the
    // given processName, keeping at most maxFileCount completed reports
    // (CRASHREPORT_UNLIMITED_FILE_COUNT for no limit,
    // CRASHREPORT_CLEANUP_ONLY_FILE_COUNT for cleanup-only). Runs at startup (not
    // the crash path) and may allocate. Returns false if storage could not be
    // initialized or output is disabled.
    bool Initialize(
        const char* rootPath,
        const char* processName,
        int32_t maxFileCount);

    // Returns whether lifecycle-managed report files should be written. False when
    // initialization failed or when output is intentionally disabled (for example
    // the cleanup-only mode, maxFileCount == CRASHREPORT_CLEANUP_ONLY_FILE_COUNT,
    // which still initializes successfully). Crash/signal-path safe: reads one bool.
    bool IsReportFileOutputEnabled() const { return m_reportFileOutputEnabled; }

    // Opens a uniquely named temporary report file under the managed directory,
    // returning its path and an open fd. Deletes any over-retention candidates
    // first. Runs on the crash/signal path: allocation-free and signal-safe.
    // Returns false if no file could be opened.
    bool PrepareReportFile(
        SignalSafeFormatter* formatter,
        char* reportFilePath,
        size_t reportFilePathSize,
        int* fd);

    // Finalizes the report opened by PrepareReportFile: on success links the temp
    // file to its final reportFilePath, then removes the temp file in all cases.
    // Runs on the crash/signal path: allocation-free and signal-safe.
    void FinishReportFile(
        bool succeeded,
        const char* reportFilePath);

private:
    // How existing and future reports are retained, derived once from the
    // configured maxFileCount so the scan does not re-test sentinels per entry.
    enum class RetentionMode
    {
        CleanupOnly, // delete every completed report and leave output disabled
        Unlimited,   // keep every completed report; never prune
        Bounded,     // keep at most maxFileCount completed reports
    };

    // Outcome of CollectExistingReports, distinguishing a hard failure from the
    // intentional cleanup-only path (both leave report output disabled).
    enum class CollectResult
    {
        Failed,
        CleanupOnlyComplete,
        Ready,
    };

    struct ReportPath
    {
        char value[CRASHREPORT_PATH_BUFFER_SIZE];
    };

    struct FileInfo
    {
        uint64_t timestamp;
        uint64_t pid;
        uint64_t suffix;
        ReportPath path;
    };

    // Maps the configured maxFileCount onto the retention mode that governs how
    // the existing-report scan and pruning behave.
    static RetentionMode GetRetentionMode(int32_t maxFileCount);

    // Resolves rootPath/processName into m_reportDirectory, creating the managed
    // directory tree and verifying it is writable. Initialization path; logs and
    // returns false on failure.
    bool EstablishReportDirectory(
        const char* rootPath,
        const char* processName);

    // Scans m_reportDirectory: removes stale temp files and, in CleanupOnly mode,
    // every completed report; otherwise collects completed reports into a newly
    // allocated array transferred to the caller on CollectResult::Ready.
    // Initialization path; may allocate.
    CollectResult CollectExistingReports(
        RetentionMode mode,
        FileInfo** reports,
        size_t* reportCount);

    // Sorts the collected reports oldest-first and records the over-retention
    // entries (beyond maxFileCount - 1, reserving one slot for the imminent
    // report) into m_deleteCandidates. Initialization path; logs and returns
    // false if the candidate storage allocation fails.
    bool SelectDeleteCandidates(
        FileInfo* reports,
        size_t reportCount,
        int32_t maxFileCount);

    // Builds the final and temporary report paths from the timestamp/pid/suffix
    // into the caller-provided buffers. Crash-path, allocation-free.
    bool BuildReportPaths(
        SignalSafeFormatter* formatter,
        char* reportFilePath,
        size_t reportFilePathSize,
        char* tempReportFilePath,
        size_t tempReportFilePathSize,
        uint64_t timestamp,
        uint32_t pid,
        uint32_t suffix);

    // Unlinks the over-retention reports selected during Initialize. Crash-path.
    void DeleteCandidates();

    // Appends component as a new path segment (inserting a single '/' separator as
    // needed). Allocation-free; used by both the init and crash paths.
    static bool AppendPathComponent(
        char* buffer,
        size_t bufferSize,
        size_t* pos,
        const char* component);

    // Returns whether path is absolute (begins with '/'). Initialization path.
    static bool IsAbsolutePath(const char* path);

    // Resolves rootPath (expanding a leading '~' or '$HOME') into an absolute path.
    static bool ResolveRootPath(
        char* buffer,
        size_t bufferSize,
        const char* rootPath);

    // Creates the directory if missing; succeeds if it already exists as a directory.
    static bool EnsureDirectory(const char* path);

    // Verifies the directory permits create/delete by creating and removing a
    // hidden probe file. Borrows the (init-time idle) m_tempReportFilePath buffer
    // rather than placing a second large path buffer on the stack.
    bool ProbeDirectoryWritable(const char* path);

    // Parses a managed report file name (report-<timestamp>-<pid>[-<suffix>]<ext>),
    // accepting either a completed report or the in-progress .tmp form, into info.
    // Reports which form matched through isTempExtension.
    static bool TryParseReportName(
        const char* name,
        FileInfo* info,
        bool* isTempExtension);

    // Returns whether a process with the given pid currently exists.
    static bool IsProcessAlive(uint64_t pid);

    // qsort comparator ordering reports oldest-first (timestamp, then suffix, then path).
    static int CompareFileInfo(
        const void* left,
        const void* right);

    // Returns whether c is allowed unescaped in a path component.
    static bool IsSafePathCharacter(char c);

    // Copies value into buffer, replacing unsafe characters with '_' and falling
    // back to "unknown" when the result would be empty.
    static void SanitizePathComponent(
        char* buffer,
        size_t bufferSize,
        const char* value);

    char m_reportDirectory[CRASHREPORT_PATH_BUFFER_SIZE] = {};
    char m_tempReportFilePath[CRASHREPORT_PATH_BUFFER_SIZE] = {};
    ReportPath* m_deleteCandidates = nullptr;
    size_t m_deleteCandidateCount = 0;
    bool m_reportFileOutputEnabled = false;
};
