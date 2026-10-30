// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "inproccrashreportlifecycle.h"

#include "crashreportstringutils.h"
#include "pal.h"

#include <dirent.h>
#include <errno.h>
#include <fcntl.h>
#include <new>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <time.h>
#include <unistd.h>

static const char CrashReportManagedRootDirectory[] = ".dotnet";
static const char CrashReportManagedReportDirectory[] = "crash-reports";
static const char CrashReportFilePrefix[] = "report-";
static const char CrashReportFileExtension[] = ".crashreport.json";
static const char CrashReportTempExtension[] = ".tmp";

static const uint64_t NanosecondsPerSecond = 1000000000ull;

bool
InProcCrashReportLifecycle::Initialize(
    const char* rootPath,
    int32_t maxFileCount)
{
    m_reportFileOutputEnabled = false;
    m_reportDirectory[0] = '\0';
    m_tempReportFilePath[0] = '\0';
    m_cachedOldestReport.value[0] = '\0';

    if (!EstablishReportDirectory(rootPath))
    {
        return false;
    }

    if (!PruneExistingReports(maxFileCount))
    {
        return false;
    }

    m_reportFileOutputEnabled = true;
    return true;
}

bool
InProcCrashReportLifecycle::EstablishReportDirectory(
    const char* rootPath)
{
    if (rootPath == nullptr || rootPath[0] == '\0')
    {
        return false;
    }

    char root[CRASHREPORT_PATH_BUFFER_SIZE];
    if (!ResolveRootPath(root, sizeof(root), rootPath))
    {
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: invalid CrashReportRootPath");
        return false;
    }

    struct stat rootStat;
    if (stat(root, &rootStat) != 0 || !S_ISDIR(rootStat.st_mode))
    {
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: CrashReportRootPath is not an existing directory");
        return false;
    }

    size_t pos = 0;
    if (!CrashReportStringUtils::AppendString(m_reportDirectory, sizeof(m_reportDirectory), &pos, root) ||
        !AppendPathComponent(m_reportDirectory, sizeof(m_reportDirectory), &pos, CrashReportManagedRootDirectory) ||
        !EnsureDirectory(m_reportDirectory) ||
        !AppendPathComponent(m_reportDirectory, sizeof(m_reportDirectory), &pos, CrashReportManagedReportDirectory) ||
        !EnsureDirectory(m_reportDirectory) ||
        !ProbeDirectoryWritable(m_reportDirectory))
    {
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: failed to initialize crash report directory");
        m_reportDirectory[0] = '\0';
        return false;
    }

    return true;
}

size_t
InProcCrashReportLifecycle::FindOldestReportIndex(
    const FileInfo* reports,
    size_t reportCount)
{
    size_t oldest = 0;
    for (size_t i = 1; i < reportCount; i++)
    {
        if (CompareFileInfo(&reports[i], &reports[oldest]) < 0)
        {
            oldest = i;
        }
    }

    return oldest;
}

bool
InProcCrashReportLifecycle::PruneExistingReports(int32_t maxFileCount)
{
    DIR* dir = opendir(m_reportDirectory);
    if (dir == nullptr)
    {
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: failed to scan crash report directory");
        return false;
    }

    // Retain at most the newest maxFileCount completed reports. The kept set is
    // held in a fixed array sized to the bound, so a directory with an
    // unexpectedly large number of reports cannot drive an unbounded allocation;
    // overflow reports are unlinked inline as they are encountered. maxFileCount
    // is guaranteed positive by the configuration layer.
    size_t capacity = static_cast<size_t>(maxFileCount);
    FileInfo* kept = new (std::nothrow) FileInfo[capacity]();
    if (kept == nullptr)
    {
        closedir(dir);
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: failed to allocate retention scan storage");
        return false;
    }

    size_t keptCount = 0;
    while (dirent* entry = readdir(dir))
    {
        FileInfo info = {};
        bool hasTempExtension = false;
        bool parsedOwnedName = TryParseReportName(entry->d_name, &info, &hasTempExtension);
        bool isTemp = parsedOwnedName && hasTempExtension;
        bool isCompleted = parsedOwnedName && !hasTempExtension;

        if (!isTemp && !isCompleted)
        {
            continue;
        }

        char fullPath[CRASHREPORT_PATH_BUFFER_SIZE];
        fullPath[0] = '\0';
        size_t fullPathPos = 0;
        if (!CrashReportStringUtils::AppendString(fullPath, sizeof(fullPath), &fullPathPos, m_reportDirectory) ||
            !AppendPathComponent(fullPath, sizeof(fullPath), &fullPathPos, entry->d_name))
        {
            continue;
        }

        if (isTemp)
        {
            // Any leftover temp file is from a previous, now-defunct run of this
            // app (each app has its own report directory under its private
            // storage, and the writer renames its temp to the final name before
            // returning), so it can be removed unconditionally.
            unlink(fullPath);
            continue;
        }

        if (keptCount < capacity)
        {
            kept[keptCount].timestamp = info.timestamp;
            kept[keptCount].pid = info.pid;
            CrashReportStringUtils::CopyString(kept[keptCount].path.value, sizeof(kept[keptCount].path.value), fullPath);
            keptCount++;
            continue;
        }

        // The kept set is full, so this entry competes with the current oldest
        // kept report: unlink the older of the two and keep the newer. A linear
        // FindOldestReportIndex scan is used rather than a timestamp-ordered heap:
        // the array holds at most maxFileCount entries (a small bound), this runs
        // once at init, and the directory is pruned to the bound on every init so
        // the scanned count stays near the bound in steady state. A min-heap would
        // perform better but adds an <algorithm> dependency and heap bookkeeping
        // for no measurable gain here.
        size_t oldestIndex = FindOldestReportIndex(kept, keptCount);

        if (kept[oldestIndex].timestamp < info.timestamp ||
            (kept[oldestIndex].timestamp == info.timestamp && strcmp(kept[oldestIndex].path.value, fullPath) < 0))
        {
            unlink(kept[oldestIndex].path.value);
            kept[oldestIndex].timestamp = info.timestamp;
            kept[oldestIndex].pid = info.pid;
            CrashReportStringUtils::CopyString(kept[oldestIndex].path.value, sizeof(kept[oldestIndex].path.value), fullPath);
        }
        else
        {
            unlink(fullPath);
        }
    }

    closedir(dir);

    // A full kept set means the directory already holds maxFileCount reports, so
    // the next crash report would exceed the bound: cache the oldest, and the
    // crash path unlinks it before publishing the new report. Below the bound
    // nothing is cached and the crash path deletes nothing.
    if (keptCount == capacity)
    {
        size_t oldestIndex = FindOldestReportIndex(kept, keptCount);
        CrashReportStringUtils::CopyString(m_cachedOldestReport.value, sizeof(m_cachedOldestReport.value), kept[oldestIndex].path.value);
    }

    delete[] kept;
    return true;
}

bool
InProcCrashReportLifecycle::PrepareReportFile(
    SignalSafeFormatter* formatter,
    char* reportFilePath,
    size_t reportFilePathSize,
    int* fd)
{
    if (formatter == nullptr || reportFilePath == nullptr || reportFilePathSize == 0 ||
        fd == nullptr || m_reportDirectory[0] == '\0')
    {
        return false;
    }

    reportFilePath[0] = '\0';
    *fd = -1;

    // Nanosecond-resolution timestamp keeps report names unique without a retry
    // loop: even back-to-back crashes in the same process get distinct names.
    // clock_gettime(CLOCK_REALTIME) is POSIX async-signal-safe, so it is valid
    // on the crash path. A failed read degrades to a zero timestamp rather than
    // aborting the report write.
    struct timespec now = {};
    clock_gettime(CLOCK_REALTIME, &now);
    uint64_t timestampNs = static_cast<uint64_t>(now.tv_sec) * NanosecondsPerSecond +
        static_cast<uint64_t>(now.tv_nsec);
    uint32_t pid = static_cast<uint32_t>(GetCurrentProcessId());

    // Delete the cached over-retention report (if any) before opening the temp
    // file, freeing a slot so the completed set stays at the bound. A later write
    // failure intentionally does not restore it.
    if (m_cachedOldestReport.value[0] != '\0')
    {
        unlink(m_cachedOldestReport.value);
    }

    if (!BuildReportPaths(formatter, reportFilePath, reportFilePathSize, m_tempReportFilePath, sizeof(m_tempReportFilePath), timestampNs, pid))
    {
        return false;
    }

    int tempFd = open(m_tempReportFilePath, O_WRONLY | O_CREAT | O_TRUNC | O_CLOEXEC, 0600);
    if (tempFd == -1)
    {
        reportFilePath[0] = '\0';
        m_tempReportFilePath[0] = '\0';
        return false;
    }

    *fd = tempFd;
    return true;
}

void
InProcCrashReportLifecycle::FinishReportFile(
    bool succeeded,
    const char* reportFilePath)
{
    if (m_tempReportFilePath[0] == '\0')
    {
        return;
    }

    // Publish the completed report by renaming the temp file to its final name.
    // rename is async-signal-safe and, unlike link, is permitted in the Android
    // and Apple app-private storage sandboxes (where link fails with EPERM); temp
    // and final share m_reportDirectory, so this is an atomic same-directory op.
    // Only publish when the destination is absent (access fails with ENOENT) to
    // preserve the "never overwrite a completed report" invariant; any other
    // errno leaves the destination state unknown, so decline rather than risk a
    // replace. The collision-resistant final name (nanosecond timestamp plus pid)
    // keeps the residual TOCTOU window benign. On any failure the temp is removed.
    if (succeeded && reportFilePath != nullptr && reportFilePath[0] != '\0' &&
        access(reportFilePath, F_OK) != 0 && errno == ENOENT &&
        rename(m_tempReportFilePath, reportFilePath) == 0)
    {
        m_tempReportFilePath[0] = '\0';
        return;
    }

    unlink(m_tempReportFilePath);
    m_tempReportFilePath[0] = '\0';
}

bool
InProcCrashReportLifecycle::BuildReportPaths(
    SignalSafeFormatter* formatter,
    char* reportFilePath,
    size_t reportFilePathSize,
    char* tempReportFilePath,
    size_t tempReportFilePathSize,
    uint64_t timestamp,
    uint32_t pid)
{
    reportFilePath[0] = '\0';
    tempReportFilePath[0] = '\0';

    size_t pos = 0;
    if (!CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, m_reportDirectory) ||
        !AppendPathComponent(reportFilePath, reportFilePathSize, &pos, CrashReportFilePrefix) ||
        !CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, formatter->FormatUnsignedDecimal(timestamp)) ||
        !CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, "-") ||
        !CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, formatter->FormatUnsignedDecimal(pid)))
    {
        return false;
    }

    if (!CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, CrashReportFileExtension))
    {
        return false;
    }

    size_t tempPos = 0;
    return CrashReportStringUtils::AppendString(tempReportFilePath, tempReportFilePathSize, &tempPos, reportFilePath) &&
        CrashReportStringUtils::AppendString(tempReportFilePath, tempReportFilePathSize, &tempPos, CrashReportTempExtension);
}

bool
InProcCrashReportLifecycle::AppendPathComponent(
    char* buffer,
    size_t bufferSize,
    size_t* pos,
    const char* component)
{
    if (buffer == nullptr || pos == nullptr || component == nullptr || component[0] == '\0')
    {
        return false;
    }

    if (*pos != 0 && buffer[*pos - 1] != '/')
    {
        if (!CrashReportStringUtils::AppendString(buffer, bufferSize, pos, "/"))
        {
            return false;
        }
    }

    while (*component == '/')
    {
        component++;
    }

    return component[0] != '\0' && CrashReportStringUtils::AppendString(buffer, bufferSize, pos, component);
}

bool
InProcCrashReportLifecycle::IsAbsolutePath(const char* path)
{
    return path != nullptr && path[0] == '/';
}

bool
InProcCrashReportLifecycle::ResolveRootPath(
    char* buffer,
    size_t bufferSize,
    const char* rootPath)
{
    if (buffer == nullptr || bufferSize == 0 || rootPath == nullptr || rootPath[0] == '\0')
    {
        return false;
    }

    // The configuring host is responsible for supplying a fully-resolved
    // absolute path; the runtime does not expand a leading '~' or environment
    // variables and rejects anything that is not already absolute.
    if (!IsAbsolutePath(rootPath))
    {
        return false;
    }

    buffer[0] = '\0';
    size_t pos = 0;
    return CrashReportStringUtils::AppendString(buffer, bufferSize, &pos, rootPath);
}

bool
InProcCrashReportLifecycle::EnsureDirectory(const char* path)
{
    if (path == nullptr || path[0] == '\0')
    {
        return false;
    }

    struct stat st;
    if (stat(path, &st) == 0)
    {
        return S_ISDIR(st.st_mode);
    }

    if (errno != ENOENT)
    {
        return false;
    }

    if (mkdir(path, 0700) != 0)
    {
        return errno == EEXIST && stat(path, &st) == 0 && S_ISDIR(st.st_mode);
    }

    return true;
}

bool
InProcCrashReportLifecycle::ProbeDirectoryWritable(const char* path)
{
    // This runs only on the initialization path, so the probe paths are kept in
    // local stack buffers rather than borrowing a member buffer; that keeps the
    // probe self-contained and off both the heap and the signal path.
    char probePath[CRASHREPORT_PATH_BUFFER_SIZE];
    probePath[0] = '\0';
    size_t pos = 0;

    // Use a hidden throwaway file to verify the directory allows create, rename,
    // and delete (rename is the operation FinishReportFile uses to publish).
    SignalSafeFormatter formatter;
    bool built =
        CrashReportStringUtils::AppendString(probePath, sizeof(probePath), &pos, path) &&
        CrashReportStringUtils::AppendString(probePath, sizeof(probePath), &pos, "/.probe-") &&
        CrashReportStringUtils::AppendString(probePath, sizeof(probePath), &pos, formatter.FormatUnsignedDecimal(static_cast<uint64_t>(GetCurrentProcessId())));

    bool writable = false;
    if (built)
    {
        int fd = open(probePath, O_WRONLY | O_CREAT | O_TRUNC | O_CLOEXEC, 0600);
        if (fd != -1)
        {
            bool closeSucceeded = close(fd) == 0;

            // Also verify the publish primitive the crash path depends on: a
            // same-directory rename. Some sandboxes (notably Android and Apple
            // app-private storage) permit create/delete yet reject other
            // link/rename operations. Probing it here disables file output up
            // front with a clear diagnostic instead of silently losing every
            // report when FinishReportFile cannot publish on the signal path.
            char committedPath[CRASHREPORT_PATH_BUFFER_SIZE];
            size_t committedPos = 0;
            bool committedBuilt =
                CrashReportStringUtils::AppendString(committedPath, sizeof(committedPath), &committedPos, probePath) &&
                CrashReportStringUtils::AppendString(committedPath, sizeof(committedPath), &committedPos, ".committed");

            if (committedBuilt)
            {
                // Clear any stale committed artifact so the rename targets a fresh name instead of replacing it.
                unlink(committedPath);
            }

            if (committedBuilt && rename(probePath, committedPath) == 0)
            {
                bool unlinkSucceeded = unlink(committedPath) == 0;
                writable = closeSucceeded && unlinkSucceeded;
            }
            else
            {
                // The rename probe failed (or the target path did not fit); remove the probe file so no stray artifact remains.
                unlink(probePath);
            }
        }
    }

    return writable;
}

bool
InProcCrashReportLifecycle::TryParseReportName(
    const char* name,
    FileInfo* info,
    bool* isTempExtension)
{
    if (name == nullptr || info == nullptr || isTempExtension == nullptr)
    {
        return false;
    }

    *isTempExtension = false;

    size_t prefixLength = sizeof(CrashReportFilePrefix) - 1;
    size_t extensionLength = sizeof(CrashReportFileExtension) - 1;

    // The shortest name this function can accept is the prefix, a single
    // timestamp digit, the '-' separator, a single pid digit, and the extension.
    // "0-0" encodes that minimal timestamp-separator-pid core. Reject anything
    // shorter up front so we never walk the per-part parse for a name that cannot
    // possibly match.
    size_t minimumLength = prefixLength + (sizeof("0-0") - 1) + extensionLength;
    if (strlen(name) < minimumLength)
    {
        return false;
    }

    if (strncmp(name, CrashReportFilePrefix, prefixLength) != 0)
    {
        return false;
    }

    const char* current = name + prefixLength;

    // The timestamp and pid are written by this process as plain decimal digits,
    // so parse them directly with strtoull/strtoul. end == current means no digits
    // were consumed, which rejects an empty timestamp or pid component.
    char* end = nullptr;
    uint64_t timestamp = strtoull(current, &end, 10);
    if (end == current || *end != '-')
    {
        return false;
    }
    current = end + 1;

    uint64_t pid = strtoul(current, &end, 10);
    if (end == current)
    {
        return false;
    }
    current = end;

    if (strncmp(current, CrashReportFileExtension, extensionLength) != 0)
    {
        return false;
    }
    current += extensionLength;

    if (*current != '\0')
    {
        size_t tempExtensionLength = sizeof(CrashReportTempExtension) - 1;
        if (strncmp(current, CrashReportTempExtension, tempExtensionLength) != 0 ||
            current[tempExtensionLength] != '\0')
        {
            return false;
        }

        *isTempExtension = true;
    }

    info->timestamp = timestamp;
    info->pid = pid;
    return true;
}

// Comparator ordering reports oldest-first (timestamp, then path).
int
InProcCrashReportLifecycle::CompareFileInfo(
    const void* left,
    const void* right)
{
    const FileInfo* leftInfo = reinterpret_cast<const FileInfo*>(left);
    const FileInfo* rightInfo = reinterpret_cast<const FileInfo*>(right);

    if (leftInfo->timestamp < rightInfo->timestamp)
    {
        return -1;
    }
    if (leftInfo->timestamp > rightInfo->timestamp)
    {
        return 1;
    }
    return strcmp(leftInfo->path.value, rightInfo->path.value);
}
