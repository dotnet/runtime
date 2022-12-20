// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CORERUN_HPP__
#define __CORERUN_HPP__

#include <cstdlib>
#include <cstdio>
#include <cstdarg>
#include <cassert>
#include <cstring>

#include <string>
#include <sstream>
#include <vector>
#include <set>
#include <functional>
#include <memory>
#include <minipal/utils.h>

// Class used to perform specific actions.
class platform_specific_actions;

//
// Platform abstraction layer
//

namespace pal
{
    // Handle to a loaded module
    using mod_t = void*;

    struct free_delete
    {
        void operator()(void* x) { ::free(x); }
    };

    template<typename T>
    using malloc_ptr = std::unique_ptr<T, free_delete>;

    enum class debugger_state_t
    {
        na,
        attached,
        not_attached,
    };
}

#ifdef TARGET_WINDOWS
#define CDECL __cdecl
#include <Windows.h>

#define DLL_EXPORT __declspec(dllexport)
#define MAIN __cdecl wmain
#define W(str) L ## str

namespace pal
{
    using char_t = wchar_t;
    using string_t = std::basic_string<char_t>;
    using stringstream_t = std::basic_stringstream<char_t>;
    using string_utf8_t = std::basic_string<char>;

    const char_t dir_delim = W('\\');
    const char_t env_path_delim = W(';');
    const char_t nativelib_ext[] = W(".dll");
    const char_t coreclr_lib[] = W("coreclr");

    inline int strcmp(const char_t* str1, const char_t* str2) { return wcscmp(str1, str2); }
    inline size_t strlen(const char_t* str) { return wcslen(str); }
    inline char_t* strdup(const char_t* str) { return ::_wcsdup(str); }
    inline int fprintf(FILE* fd, const char_t* const fmt, ...)
    {
        va_list args;
        va_start(args, fmt);
        int ret = ::vfwprintf(fd, fmt, args);
        va_end(args);
        return ret;
    }
    inline bool is_cli_option(const char_t option_maybe) { return option_maybe == W('-') || option_maybe == W('/'); }
    inline string_t getenv(const char_t* var)
    {
        DWORD needed = ::GetEnvironmentVariableW(var, nullptr, 0);
        if (needed == 0)
            return {};

        malloc_ptr<char_t> buffer{ (char_t*)::malloc(needed * sizeof(char_t)) };
        assert(buffer != nullptr);
        DWORD wrote = ::GetEnvironmentVariableW(var, buffer.get(), needed);
        assert(wrote < needed);
        return { buffer.get() };
    }
    inline string_utf8_t getenvA(const char* var)
    {
        DWORD needed = ::GetEnvironmentVariableA(var, nullptr, 0);
        if (needed == 0)
            return {};

        malloc_ptr<char> buffer{ (char*)::malloc(needed * sizeof(char)) };
        assert(buffer != nullptr);
        DWORD wrote = ::GetEnvironmentVariableA(var, buffer.get(), needed);
        assert(wrote < needed);
        return { buffer.get() };
    }
    inline void setenv(const char_t* var, string_t value)
    {
        BOOL success = ::SetEnvironmentVariableW(var, value.c_str());
        assert(success);
    }
    inline void setenvA(const char* var, string_utf8_t value)
    {
        BOOL success = ::SetEnvironmentVariableA(var, value.c_str());
        assert(success);
    }
    inline string_t get_exe_path()
    {
        char_t file_name[1024];
        DWORD count = ::GetModuleFileNameW(nullptr, file_name, ARRAY_SIZE(file_name));
        assert(::GetLastError() != ERROR_INSUFFICIENT_BUFFER);

        return { file_name };
    }
    inline string_t get_absolute_path(const char_t* path)
    {
        DWORD needed = ::GetFullPathNameW(path, 0, nullptr, nullptr);
        malloc_ptr<char_t> buffer{ (char_t*)::malloc(needed * sizeof(char_t)) };
        assert(buffer != nullptr);

        DWORD wrote = ::GetFullPathNameW(path, needed, buffer.get(), nullptr);
        assert(wrote < needed);
        return { buffer.get() };
    }

    inline uint32_t get_process_id()
    {
        return (uint32_t)::GetCurrentProcessId();
    }

    inline debugger_state_t is_debugger_attached()
    {
        return (::IsDebuggerPresent() == TRUE) ? debugger_state_t::attached : debugger_state_t::not_attached;
    }

    inline bool does_file_exist(const string_t& file_path)
    {
        return INVALID_FILE_ATTRIBUTES != ::GetFileAttributesW(file_path.c_str());
    }

    // Forward declaration
    void ensure_trailing_delimiter(pal::string_t& dir);

    inline string_t build_file_list(
        const string_t& dir,
        const char_t* ext,
        std::function<bool(const char_t*)> should_add)
    {
        assert(ext != nullptr);

        string_t dir_local = dir;
        dir_local.append(W("*"));
        dir_local.append(ext);

        WIN32_FIND_DATA data;
        HANDLE findHandle = ::FindFirstFileW(dir_local.data(), &data);
        if (findHandle == INVALID_HANDLE_VALUE)
            return {};

        stringstream_t file_list;
        do
        {
            if (!(data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
            {
                // ToLower for case-insensitive comparisons
                char_t* fileNameChar = data.cFileName;
                while (*fileNameChar)
                {
                    *fileNameChar = towlower(*fileNameChar);
                    fileNameChar++;
                }

                if (should_add(data.cFileName))
                    file_list << dir << data.cFileName << env_path_delim;
            }
        } while (FALSE != ::FindNextFileW(findHandle, &data));

        ::FindClose(findHandle);

        return file_list.str();
    }

    inline void* get_module_symbol(mod_t m, const char* sym)
    {
        assert(m != nullptr && sym != nullptr);
        return ::GetProcAddress((HMODULE)m, sym);
    }

    inline string_utf8_t convert_to_utf8(const char_t* str)
    {
        int bytes_req = ::WideCharToMultiByte(CP_UTF8, 0, str, -1, nullptr, 0, nullptr, nullptr);

        malloc_ptr<char> buffer{ (char*)::malloc(bytes_req) };
        assert(buffer != nullptr);

        int written = ::WideCharToMultiByte(CP_UTF8, 0, str, -1, buffer.get(), bytes_req, nullptr, nullptr);
        assert(bytes_req == written);

        return { buffer.get() };
    }

    inline bool try_load_hostpolicy(pal::string_t mock_hostpolicy_value)
    {
        const char_t* hostpolicyName = W("hostpolicy.dll");
        pal::mod_t hMod = (pal::mod_t)::GetModuleHandleW(hostpolicyName);
        if (hMod != nullptr)
            return true;

        // Check if a hostpolicy exists and if it does, load it.
        if (pal::does_file_exist(mock_hostpolicy_value))
            hMod = (pal::mod_t)::LoadLibraryExW(mock_hostpolicy_value.c_str(), nullptr, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);

        if (hMod == nullptr)
            pal::fprintf(stderr, W("Failed to load mock hostpolicy at path '%s'. Error: 0x%08x\n"), mock_hostpolicy_value.c_str(), ::GetLastError());

        return hMod != nullptr;
    }

    inline bool try_load_coreclr(const pal::string_t& core_root, pal::mod_t& hMod)
    {
        pal::string_t coreclr_path = core_root;
        pal::ensure_trailing_delimiter(coreclr_path);
        coreclr_path.append(pal::coreclr_lib);
        coreclr_path.append(pal::nativelib_ext);

        hMod = (pal::mod_t)::LoadLibraryExW(coreclr_path.c_str(), nullptr, 0);
        if (hMod == nullptr)
        {
            pal::fprintf(stderr, W("Failed to load: '%s'. Error: 0x%08x\n"), coreclr_path.c_str(), ::GetLastError());
            return false;
        }

        // Pin the module - CoreCLR.dll does not support being unloaded.
        HMODULE unused;
        if (!::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN, coreclr_path.c_str(), &unused))
        {
            pal::fprintf(stderr, W("Failed to pin: '%s'. Error: 0x%08x\n"), coreclr_path.c_str(), ::GetLastError());
            return false;
        }

        return true;
    }
}

class platform_specific_actions final
{
    HANDLE _actCxt;
    ULONG_PTR _actCookie;

public:
    platform_specific_actions() = default;

    void before_coreclr_load() { }

    void before_execute_assembly(const pal::string_t& assembly)
    {
        ACTCTX cxt{};
        cxt.cbSize = sizeof(cxt);
        cxt.dwFlags = (ACTCTX_FLAG_APPLICATION_NAME_VALID | ACTCTX_FLAG_RESOURCE_NAME_VALID);
        cxt.lpSource = assembly.c_str();
        cxt.lpResourceName = MAKEINTRESOURCEW(1); // The CreateProcess manifest which contains the context details

        _actCxt = ::CreateActCtxW(&cxt);
        if (_actCxt == INVALID_HANDLE_VALUE)
        {
            _actCookie = ULONG_PTR{};
            DWORD err = ::GetLastError();
            if (err != ERROR_RESOURCE_TYPE_NOT_FOUND && err != ERROR_RESOURCE_DATA_NOT_FOUND)
                pal::fprintf(stderr, W("Activation Context creation failed. Error: 0x%08x\n"), err);
        }
        else
        {
            BOOL res = ::ActivateActCtx(_actCxt, &_actCookie);
            if (res == FALSE)
                pal::fprintf(stderr, W("Failed to activate Activation Context. Error: 0x%08x\n"), ::GetLastError());
        }
    }

    void after_execute_assembly()
    {
        if (_actCookie != ULONG_PTR{})
        {
            BOOL res = ::DeactivateActCtx(0, _actCookie);
            if (res == FALSE)
                pal::fprintf(stderr, W("Failed to de-activate Activation Context. Error: 0x%08x\n"), ::GetLastError());
        }

        if (_actCxt != INVALID_HANDLE_VALUE)
            ::ReleaseActCtx(_actCxt);
    }
};

#else // !TARGET_WINDOWS
#include <dirent.h>
#include <dlfcn.h>
#include <limits.h>
#include <sys/stat.h>
#include <unistd.h>

// Needed for detecting the debugger attach scenario
#if defined(__APPLE__)
#include <sys/sysctl.h>
#include <sys/types.h>
#else // !__APPLE__
#include <fcntl.h>
#include <ctype.h>
#endif // !__APPLE__

// CMake generated
#include <config.h>
#include <minipal/getexepath.h>

#if __GNUC__ >= 4
#define DLL_EXPORT __attribute__ ((visibility ("default")))
#else
#define DLL_EXPORT
#endif
#define CDECL
#define MAIN main
#define W(str) str
#define S_OK 0
#define FAILED(result) (result < S_OK)
#if !HAVE_DIRENT_D_TYPE
#define DT_UNKNOWN 0
#define DT_DIR 4
#define DT_REG 8
#define DT_LNK 10
#endif

typedef int HRESULT;

namespace pal
{
    using char_t = char;
    using string_t = std::basic_string<char_t>;
    using stringstream_t = std::basic_stringstream<char_t>;
    using string_utf8_t = std::basic_string<char>;

    const char_t dir_delim = W('/');
    const char_t env_path_delim = W(':');

#if defined(__APPLE__)
    const char_t nativelib_ext[] = W(".dylib");
#else // Various Linux-related OS-es
    const char_t nativelib_ext[] = W(".so");
#endif
    const char_t coreclr_lib[] = W("libcoreclr");

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::strcmp(str1, str2); }
    inline size_t strlen(const char_t* str) { return ::strlen(str); }
    inline char_t* strdup(const char_t* str) { return ::strdup(str); }
    inline int fprintf(FILE* fd, const char_t* const fmt, ...)
    {
        va_list args;
        va_start(args, fmt);
        int ret = ::vfprintf(fd, fmt, args);
        va_end(args);
        return ret;
    }
    inline bool is_cli_option(const char_t option_maybe) { return option_maybe == W('-'); }
    inline string_t getenv(const char_t* var)
    {
        const char_t* val = ::getenv(var);
        if (val == nullptr)
            return {};
        return { val };
    }
    inline string_utf8_t getenvA(const char* var)
    {
        return getenv(var);
    }
    inline void setenv(const char_t* var, string_t value)
    {
        int error = ::setenv(var, value.c_str(), /* overwrite */ 1);
        assert(error == 0);
    }
    inline void setenvA(const char* var, string_utf8_t value)
    {
        setenv(var, value.c_str());
    }

    inline string_t get_exe_path() { return minipal_getexepath(); }

    inline string_t get_absolute_path(const string_t& path)
    {
        string_t abs_path = path;

        char_t realPath[PATH_MAX];
        if (realpath(path.c_str(), realPath) != nullptr && realPath[0] != W('\0'))
        {
            abs_path.assign(realPath);
            // realpath should return canonicalized path without the trailing slash
            assert(abs_path.back() != W('/'));
        }

        return abs_path;
    }

    inline uint32_t get_process_id()
    {
        return (uint32_t)getpid();
    }

    inline debugger_state_t is_debugger_attached()
    {
#if defined(__APPLE__)
        // Taken from https://developer.apple.com/library/archive/qa/qa1361/_index.html
        int                 junk;
        int                 mib[4];
        struct kinfo_proc   info;
        size_t              size;

        // Initialize the flags so that, if sysctl fails for some bizarre
        // reason, we get a predictable result.

        info.kp_proc.p_flag = 0;

        // Initialize mib, which tells sysctl the info we want, in this case
        // we're looking for information about a specific process ID.

        mib[0] = CTL_KERN;
        mib[1] = KERN_PROC;
        mib[2] = KERN_PROC_PID;
        mib[3] = getpid();

        // Call sysctl.

        size = sizeof(info);
        junk = sysctl(mib, sizeof(mib) / sizeof(*mib), &info, &size, NULL, 0);
        assert(junk == 0);

        // We're being debugged if the P_TRACED flag is set.

        return ( (info.kp_proc.p_flag & P_TRACED) != 0 ) ? debugger_state_t::attached : debugger_state_t::not_attached;

#else // !__APPLE__
        // Use procfs to detect if there is a tracer process.
        // See https://www.kernel.org/doc/html/latest/filesystems/proc.html
        char status[2048] = { 0 };
        int fd = ::open("/proc/self/status", O_RDONLY);
        if (fd == -1)
        {
            // If the file can't be opened assume we are on a not supported platform.
            return debugger_state_t::na;
        }

        // Attempt to read
        ssize_t bytes_read = ::read(fd, status, sizeof(status) - 1);
        ::close(fd);

        if (bytes_read > 0)
        {
            // We have data. At this point we can likely make a strong decision.
            const char tracer_pid_name[] = "TracerPid:";
            const char* tracer_pid_ptr = ::strstr(status, tracer_pid_name);
            if (tracer_pid_ptr == nullptr)
                return debugger_state_t::not_attached;

            // The number after the name is the process ID of the
            // tracer application or 0 if none exists.
            const char* curr = tracer_pid_ptr + (sizeof(tracer_pid_name) - 1);
            const char* end = status + bytes_read;
            for (;curr < end; ++curr)
            {
                if (::isspace(*curr))
                    continue;

                // Check the first non-space if it is 0. If so, we have
                // a non-zero process ID and a tracer is attached.
                return (::isdigit(*curr) && *curr != '0') ? debugger_state_t::attached : debugger_state_t::not_attached;
            }
        }

        // The read in data is either incomplete (i.e. small buffer) or
        // the returned content is not expected. Let's fallback to not available.
        return debugger_state_t::na;

#endif // !__APPLE__
    }

    inline bool does_file_exist(const char_t* file_path)
    {
        // Check if the specified path exists
        struct stat sb;
        if (stat(file_path, &sb) == -1)
        {
            perror(W("Path not found"));
            return false;
        }

        // Ignore directories
        if (S_ISDIR(sb.st_mode))
        {
            return false;
        }

        // Verify that the path points to a file
        if (!S_ISREG(sb.st_mode))
        {
            pal::fprintf(stderr, W("The specified managed assembly is not a file: %s\n"), file_path);
            return false;
        }

        return true;
    }

    // Forward declaration
    template<size_t LEN>
    bool string_ends_with(const string_t& str, const char_t(&suffix)[LEN]);
    bool string_ends_with(const string_t& str, size_t suffix_len, const char_t* suffix);
    void ensure_trailing_delimiter(pal::string_t& dir);

    inline string_t build_file_list(
        const string_t& directory,
        const char_t* ext,
        std::function<bool(const char_t*)> should_add)
    {
        assert(ext != nullptr);
        const size_t ext_len = pal::strlen(ext);

        DIR* dir = opendir(directory.c_str());
        if (dir == nullptr)
            return {};

        stringstream_t file_list;

        // For all entries in the directory
        struct dirent* entry;
        while ((entry = readdir(dir)) != nullptr)
        {
#if HAVE_DIRENT_D_TYPE
            int dirEntryType = entry->d_type;
#else
            int dirEntryType = DT_UNKNOWN;
#endif

            // We are interested in files only
            switch (dirEntryType)
            {
            case DT_REG:
                break;

            // Handle symlinks and file systems that do not support d_type
            case DT_LNK:
            case DT_UNKNOWN:
                {
                    string_t full_filename;
                    full_filename.append(directory);
                    full_filename.append(1, pal::dir_delim);
                    full_filename.append(entry->d_name);
                    if (!does_file_exist(full_filename.c_str()))
                        continue;
                }
                break;

            default:
                continue;
            }

            // Check if the extension matches the one we are looking for
            if (!string_ends_with(entry->d_name, ext_len, ext))
                continue;

            // Make sure if we have an assembly with multiple extensions present,
            // we insert only one version of it.
            if (should_add(entry->d_name))
                file_list << directory << dir_delim << entry->d_name << env_path_delim;
        }

        closedir(dir);
        return file_list.str();
    }

    inline void* get_module_symbol(mod_t m, const char* sym)
    {
        assert(m != nullptr && sym != nullptr);
        return dlsym(m, sym);
    }

    inline string_utf8_t convert_to_utf8(const char_t* str)
    {
        return { str };
    }

    inline bool try_load_hostpolicy(pal::string_t mock_hostpolicy_value)
    {
        if (!string_ends_with(mock_hostpolicy_value, pal::nativelib_ext))
            mock_hostpolicy_value.append(pal::nativelib_ext);

        pal::mod_t hMod = (pal::mod_t)dlopen(mock_hostpolicy_value.c_str(), RTLD_LAZY);
        if (hMod == nullptr)
            pal::fprintf(stderr, W("Failed to load mock hostpolicy at path '%s'. Error: %s\n"), mock_hostpolicy_value.c_str(), dlerror());

        return hMod != nullptr;
    }

    inline bool try_load_coreclr(const pal::string_t& core_root, pal::mod_t& hMod)
    {
        pal::string_t coreclr_path = core_root;
        pal::ensure_trailing_delimiter(coreclr_path);
        coreclr_path.append(pal::coreclr_lib);
        coreclr_path.append(pal::nativelib_ext);

        hMod = (pal::mod_t)dlopen(coreclr_path.c_str(), RTLD_NOW | RTLD_LOCAL);
        if (hMod == nullptr)
        {
            pal::fprintf(stderr, W("Failed to load: '%s'. Error: %s\n"), coreclr_path.c_str(), dlerror());
            return false;
        }

        return true;
    }
}

class platform_specific_actions final
{
public:
    platform_specific_actions() = default;

    void before_coreclr_load()
    {
#ifdef HOST_ARM
        // libunwind library is used to unwind stack frame, but libunwind for ARM
        // does not support ARM vfpv3/NEON registers in DWARF format correctly.
        // Therefore let's disable stack unwinding using DWARF information
        // See https://github.com/dotnet/runtime/issues/6479
        //
        // libunwind use following methods to unwind stack frame.
        // UNW_ARM_METHOD_ALL          0xFF
        // UNW_ARM_METHOD_DWARF        0x01
        // UNW_ARM_METHOD_FRAME        0x02
        // UNW_ARM_METHOD_EXIDX        0x04
        putenv(const_cast<char*>("UNW_ARM_UNWIND_METHOD=6"));
#endif // HOST_ARM
    }

    void before_execute_assembly(const pal::string_t& assembly) { }

    void after_execute_assembly() { }
};

#endif // !TARGET_WINDOWS

namespace pal
{
    inline void split_path_to_dir_filename(const pal::string_t& path, pal::string_t& dir, pal::string_t& filename)
    {
        size_t pos = path.find_last_of(dir_delim);
        if (pos == pal::string_t::npos)
        {
            dir = {};
            filename = path;
            return;
        }

        dir = path.substr(0, pos);
        filename = path.substr(pos + 1);
    }

    inline bool string_ends_with(const string_t& str, size_t suffix_len, const char_t* suffix)
    {
        assert(suffix != nullptr);

        size_t str_len = str.length();
        if (str_len < suffix_len)
            return false;

        const char_t* suffix_maybe = str.data() + (str_len - suffix_len);
        return ::memcmp(suffix_maybe, suffix, suffix_len * sizeof(char_t)) == 0;
    }

    template<size_t LEN>
    bool string_ends_with(const string_t& str, const char_t(&suffix)[LEN])
    {
        return string_ends_with(str, LEN - 1, suffix);
    }

    inline void ensure_trailing_delimiter(pal::string_t& dir)
    {
        if (dir.empty())
        {
            dir = pal::dir_delim;
        }
        else if (dir.back() != pal::dir_delim)
        {
            dir.push_back(pal::dir_delim);
        }
    }

    inline const char** convert_argv_to_utf8(int argc, const char_t** argv, std::vector<string_utf8_t>& lifetime)
    {
        malloc_ptr<const char*> ret{ (const char**)::malloc(sizeof(char*) * argc) };
        assert(ret != nullptr);

        lifetime.resize(argc);
        for (int i = 0; i < argc; ++i)
        {
            string_utf8_t s = convert_to_utf8(argv[i]);
            lifetime[i] = std::move(s);
            ret.get()[i] = lifetime[i].c_str();
        }

        return ret.release();
    }
}

#endif // __CORERUN_HPP__
