// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "inproccrashreportlifecycle.h"

#include "crashreportstringutils.h"
#include "pal.h"

#include <dirent.h>
#include <errno.h>
#include <fcntl.h>
#include <limits.h>
#include <new>
#include <signal.h>
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
// Bounded retry avoids an unbounded crash-path loop if a process repeatedly
// crashes in the same second with the same pid and stale files are present.
static constexpr uint32_t CrashReportMaxSuffixRetry = 32;
// Directory scans run at initialization, but misconfigured roots should not let
// a diagnostic feature stall application startup indefinitely.
static constexpr size_t CrashReportMaxDirectoryEntriesToScan = 4096;

InProcCrashReportLifecycle::~InProcCrashReportLifecycle()
{
    delete[] m_deleteCandidates;
}

bool
InProcCrashReportLifecycle::Initialize(
    const char* rootPath,
    const char* processName,
    int32_t maxFileCount)
{
    m_reportFileOutputEnabled = false;
    m_reportDirectory[0] = '\0';
    m_tempReportFilePath[0] = '\0';

    if (!EstablishReportDirectory(rootPath, processName))
    {
        return false;
    }

    RetentionMode mode = GetRetentionMode(maxFileCount);

    FileInfo* reports = nullptr;
    size_t reportCount = 0;
    if (CollectExistingReports(mode, &reports, &reportCount) != CollectResult::Ready)
    {
        // Both a hard failure and the cleanup-only path leave output disabled; the
        // former already logged, the latter performed its deletions during the scan.
        return false;
    }

    if (mode == RetentionMode::Bounded && !SelectDeleteCandidates(reports, reportCount, maxFileCount))
    {
        free(reports);
        return false;
    }

    free(reports);
    m_reportFileOutputEnabled = true;
    return true;
}

InProcCrashReportLifecycle::RetentionMode
InProcCrashReportLifecycle::GetRetentionMode(int32_t maxFileCount)
{
    if (maxFileCount == CRASHREPORT_CLEANUP_ONLY_FILE_COUNT)
    {
        return RetentionMode::CleanupOnly;
    }

    if (maxFileCount == CRASHREPORT_UNLIMITED_FILE_COUNT)
    {
        return RetentionMode::Unlimited;
    }

    // The configuration layer constrains maxFileCount to the unlimited (-1),
    // cleanup-only (0), or positive-bound domain, so anything else is a bound.
    return RetentionMode::Bounded;
}

bool
InProcCrashReportLifecycle::EstablishReportDirectory(
    const char* rootPath,
    const char* processName)
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

    char appName[CRASHREPORT_STRING_BUFFER_SIZE];
    SanitizePathComponent(appName, sizeof(appName), processName);

    size_t pos = 0;
    if (!CrashReportStringUtils::AppendString(m_reportDirectory, sizeof(m_reportDirectory), &pos, root) ||
        !AppendPathComponent(m_reportDirectory, sizeof(m_reportDirectory), &pos, CrashReportManagedRootDirectory) ||
        !EnsureDirectory(m_reportDirectory) ||
        !AppendPathComponent(m_reportDirectory, sizeof(m_reportDirectory), &pos, CrashReportManagedReportDirectory) ||
        !EnsureDirectory(m_reportDirectory) ||
        !AppendPathComponent(m_reportDirectory, sizeof(m_reportDirectory), &pos, appName) ||
        !EnsureDirectory(m_reportDirectory) ||
        !ProbeDirectoryWritable(m_reportDirectory))
    {
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: failed to initialize crash report directory");
        m_reportDirectory[0] = '\0';
        return false;
    }

    return true;
}

InProcCrashReportLifecycle::CollectResult
InProcCrashReportLifecycle::CollectExistingReports(
    RetentionMode mode,
    FileInfo** reports,
    size_t* reportCount)
{
    *reports = nullptr;
    *reportCount = 0;

    DIR* dir = opendir(m_reportDirectory);
    if (dir == nullptr)
    {
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: failed to scan crash report directory");
        return CollectResult::Failed;
    }

    FileInfo* collected = nullptr;
    size_t collectedCount = 0;
    size_t collectedCapacity = 0;
    size_t scannedEntries = 0;
    bool scanExceeded = false;

    while (dirent* entry = readdir(dir))
    {
        if (++scannedEntries > CrashReportMaxDirectoryEntriesToScan)
        {
            scanExceeded = true;
            break;
        }

        FileInfo info = {};
        bool hasTempExtension = false;
        bool parsedOwnedName = TryParseReportName(entry->d_name, &info, &hasTempExtension);
        bool isTemp = parsedOwnedName && hasTempExtension;
        bool isCompleted = parsedOwnedName && !hasTempExtension;

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
            if (!IsProcessAlive(info.pid))
            {
                unlink(fullPath);
            }
            continue;
        }

        if (!isCompleted)
        {
            continue;
        }

        if (mode == RetentionMode::CleanupOnly)
        {
            unlink(fullPath);
            continue;
        }

        if (mode == RetentionMode::Unlimited)
        {
            continue;
        }

        if (collectedCount == collectedCapacity)
        {
            size_t newCapacity = collectedCapacity == 0 ? 16 : collectedCapacity * 2;
            FileInfo* grown = reinterpret_cast<FileInfo*>(realloc(collected, newCapacity * sizeof(FileInfo)));
            if (grown == nullptr)
            {
                free(collected);
                closedir(dir);
                InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: failed to allocate retention scan storage");
                return CollectResult::Failed;
            }

            collected = grown;
            collectedCapacity = newCapacity;
        }

        CrashReportStringUtils::CopyString(info.path.value, sizeof(info.path.value), fullPath);
        collected[collectedCount++] = info;
    }

    closedir(dir);

    if (scanExceeded)
    {
        free(collected);
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: too many files in crash report directory");
        return CollectResult::Failed;
    }

    if (mode == RetentionMode::CleanupOnly)
    {
        free(collected);
        return CollectResult::CleanupOnlyComplete;
    }

    *reports = collected;
    *reportCount = collectedCount;
    return CollectResult::Ready;
}

bool
InProcCrashReportLifecycle::SelectDeleteCandidates(
    FileInfo* reports,
    size_t reportCount,
    int32_t maxFileCount)
{
    qsort(reports, reportCount, sizeof(FileInfo), &CompareFileInfo);

    size_t keepBeforeCrash = static_cast<size_t>(maxFileCount - 1);
    if (reportCount <= keepBeforeCrash)
    {
        return true;
    }

    m_deleteCandidateCount = reportCount - keepBeforeCrash;
    m_deleteCandidates = new (std::nothrow) ReportPath[m_deleteCandidateCount];
    if (m_deleteCandidates == nullptr)
    {
        m_deleteCandidateCount = 0;
        InProcCrashReportLogInitializationFailure(".NET crash report file output disabled: failed to allocate retention candidate storage");
        return false;
    }

    for (size_t i = 0; i < m_deleteCandidateCount; i++)
    {
        CrashReportStringUtils::CopyString(m_deleteCandidates[i].value, sizeof(m_deleteCandidates[i].value), reports[i].path.value);
    }

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
    uint64_t timestamp = static_cast<uint64_t>(time(nullptr));
    uint32_t pid = static_cast<uint32_t>(GetCurrentProcessId());

    // Retention candidates are deleted before opening the temp file so the
    // completed-report set never exceeds the configured bound and old reports
    // can free space for the new crash report. If the new write later fails,
    // the deleted candidates are intentionally not restored.
    DeleteCandidates();

    for (uint32_t suffix = 0; suffix <= CrashReportMaxSuffixRetry; suffix++)
    {
        if (!BuildReportPaths(formatter, reportFilePath, reportFilePathSize, m_tempReportFilePath, sizeof(m_tempReportFilePath), timestamp, pid, suffix))
        {
            return false;
        }

        if (access(reportFilePath, F_OK) == 0)
        {
            continue;
        }

        int tempFd = open(m_tempReportFilePath, O_WRONLY | O_CREAT | O_EXCL | O_CLOEXEC, 0600);
        if (tempFd == -1)
        {
            if (errno == EEXIST)
            {
                continue;
            }

            return false;
        }

        *fd = tempFd;
        return true;
    }

    reportFilePath[0] = '\0';
    m_tempReportFilePath[0] = '\0';
    return false;
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

    if (succeeded && reportFilePath != nullptr && reportFilePath[0] != '\0')
    {
        (void)link(m_tempReportFilePath, reportFilePath);
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
    uint32_t pid,
    uint32_t suffix)
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

    if (suffix != 0)
    {
        if (!CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, "-") ||
            !CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, formatter->FormatUnsignedDecimal(suffix)))
        {
            return false;
        }
    }

    if (!CrashReportStringUtils::AppendString(reportFilePath, reportFilePathSize, &pos, CrashReportFileExtension))
    {
        return false;
    }

    size_t tempPos = 0;
    return CrashReportStringUtils::AppendString(tempReportFilePath, tempReportFilePathSize, &tempPos, reportFilePath) &&
        CrashReportStringUtils::AppendString(tempReportFilePath, tempReportFilePathSize, &tempPos, CrashReportTempExtension);
}

void
InProcCrashReportLifecycle::DeleteCandidates()
{
    for (size_t i = 0; i < m_deleteCandidateCount; i++)
    {
        if (m_deleteCandidates[i].value[0] != '\0')
        {
            unlink(m_deleteCandidates[i].value);
        }
    }
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

    buffer[0] = '\0';
    size_t pos = 0;

    if (rootPath[0] == '~' && (rootPath[1] == '\0' || rootPath[1] == '/'))
    {
        const char* home = getenv("HOME");
        if (home == nullptr || home[0] == '\0')
        {
            return false;
        }

        if (!CrashReportStringUtils::AppendString(buffer, bufferSize, &pos, home))
        {
            return false;
        }

        rootPath++;
        if (*rootPath == '/')
        {
            rootPath++;
        }
        if (*rootPath != '\0' && !AppendPathComponent(buffer, bufferSize, &pos, rootPath))
        {
            return false;
        }
    }
    else if (strncmp(rootPath, "$HOME", 5) == 0 && (rootPath[5] == '\0' || rootPath[5] == '/'))
    {
        const char* home = getenv("HOME");
        if (home == nullptr || home[0] == '\0')
        {
            return false;
        }

        if (!CrashReportStringUtils::AppendString(buffer, bufferSize, &pos, home))
        {
            return false;
        }

        rootPath += 5;
        if (*rootPath == '/')
        {
            rootPath++;
        }
        if (*rootPath != '\0' && !AppendPathComponent(buffer, bufferSize, &pos, rootPath))
        {
            return false;
        }
    }
    else if (!CrashReportStringUtils::AppendString(buffer, bufferSize, &pos, rootPath))
    {
        return false;
    }

    return IsAbsolutePath(buffer);
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
    // Borrow the temp-report path buffer as scratch: it is unused until the
    // crash path calls PrepareReportFile, so reusing it here avoids a second
    // large path buffer while keeping the probe allocation-free.
    char* probePath = m_tempReportFilePath;
    probePath[0] = '\0';
    size_t pos = 0;

    // Use a hidden throwaway file to verify the directory allows create/delete.
    SignalSafeFormatter formatter;
    bool built =
        CrashReportStringUtils::AppendString(probePath, sizeof(m_tempReportFilePath), &pos, path) &&
        CrashReportStringUtils::AppendString(probePath, sizeof(m_tempReportFilePath), &pos, "/.probe-") &&
        CrashReportStringUtils::AppendString(probePath, sizeof(m_tempReportFilePath), &pos, formatter.FormatUnsignedDecimal(static_cast<uint64_t>(GetCurrentProcessId())));

    bool writable = false;
    if (built)
    {
        int fd = open(probePath, O_WRONLY | O_CREAT | O_EXCL | O_CLOEXEC, 0600);
        if (fd != -1)
        {
            bool closeSucceeded = close(fd) == 0;
            bool unlinkSucceeded = unlink(probePath) == 0;
            writable = closeSucceeded && unlinkSucceeded;
        }
    }

    // Leave the borrowed buffer empty so the crash path's "no temp file yet"
    // invariant continues to hold after initialization.
    m_tempReportFilePath[0] = '\0';
    return writable;
}

static bool TryParseUnsigned(
    const char** value,
    uint64_t* result)
{
    const char* current = *value;
    if (current == nullptr || *current < '0' || *current > '9')
    {
        return false;
    }

    uint64_t parsed = 0;
    do
    {
        uint64_t digit = static_cast<uint64_t>(*current - '0');
        if (parsed > (UINT64_MAX - digit) / 10)
        {
            return false;
        }

        parsed = parsed * 10 + digit;
        current++;
    } while (*current >= '0' && *current <= '9');

    *value = current;
    *result = parsed;
    return true;
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
    uint64_t timestamp = 0;
    uint64_t pid = 0;
    uint64_t suffix = 0;

    if (!TryParseUnsigned(&current, &timestamp) || *current != '-')
    {
        return false;
    }
    current++;

    if (!TryParseUnsigned(&current, &pid))
    {
        return false;
    }

    if (*current == '-')
    {
        current++;
        if (!TryParseUnsigned(&current, &suffix))
        {
            return false;
        }
    }

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
    info->suffix = suffix;
    return true;
}

bool
InProcCrashReportLifecycle::IsProcessAlive(uint64_t pid)
{
    if (pid == 0 || pid > static_cast<uint64_t>(INT_MAX))
    {
        return false;
    }

    // Signal 0 probes for process existence without delivering a signal.
    if (kill(static_cast<pid_t>(pid), 0) == 0)
    {
        return true;
    }

    return errno != ESRCH;
}

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
    if (leftInfo->suffix < rightInfo->suffix)
    {
        return -1;
    }
    if (leftInfo->suffix > rightInfo->suffix)
    {
        return 1;
    }
    return strcmp(leftInfo->path.value, rightInfo->path.value);
}

bool
InProcCrashReportLifecycle::IsSafePathCharacter(char c)
{
    return (c >= 'A' && c <= 'Z') ||
        (c >= 'a' && c <= 'z') ||
        (c >= '0' && c <= '9') ||
        c == '.' ||
        c == '_' ||
        c == '-';
}

void
InProcCrashReportLifecycle::SanitizePathComponent(
    char* buffer,
    size_t bufferSize,
    const char* value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    const char* source = (value != nullptr && value[0] != '\0') ? value : "unknown";
    size_t pos = 0;
    while (*source != '\0' && pos + 1 < bufferSize)
    {
        buffer[pos++] = IsSafePathCharacter(*source) ? *source : '_';
        source++;
    }

    if (pos == 0)
    {
        const char fallback[] = "unknown";
        for (size_t i = 0; fallback[i] != '\0' && pos + 1 < bufferSize; i++)
        {
            buffer[pos++] = fallback[i];
        }
    }

    buffer[pos] = '\0';
}
