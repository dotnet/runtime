// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runtime headers
#include <coreclrhost.h>

#include "corerun.hpp"
#include "dotenv.hpp"

#include <fstream>

using char_t = pal::char_t;
using string_t = pal::string_t;

struct configuration
{
    configuration() = default;
    configuration(const configuration&) = delete;
    configuration(configuration&&) = delete;
    configuration& operator=(const configuration&) = delete;
    configuration& operator=(configuration&&) = delete;

    ~configuration()
    {
        for (int i = 0; i < entry_assembly_argc; ++i)
        {
            ::free((void*)entry_assembly_argv[i]);
        }
        ::free(entry_assembly_argv);
    }

    //
    // Settings
    //

    // CLR path - user supplied location of coreclr binary and managed assemblies.
    string_t clr_path;

    // The full path to the Supplied managed entry assembly.
    string_t entry_assembly_fullpath;

    // Arguments to pass to managed entry assembly.
    int entry_assembly_argc;
    const char_t** entry_assembly_argv;

    // Collection of user-defined key/value pairs that will be appended
    // to the initialization of the runtime.
    std::vector<string_t> user_defined_keys;
    std::vector<string_t> user_defined_values;

    // Wait for debugger to be attached.
    bool wait_to_debug;

    // Perform self test.
    bool self_test;

    // configured .env file to load
    dotenv dotenv_configuration;
};

namespace envvar
{
    // Points to a path containing the CoreCLR binary.
    const char_t* coreRoot = W("CORE_ROOT");

    // Points to a path containing additional platform assemblies.
    const char_t* coreLibraries = W("CORE_LIBRARIES");

    // Variable used to preload a mock hostpolicy for testing.
    const char_t* mockHostPolicy = W("MOCK_HOSTPOLICY");
}

static void wait_for_debugger()
{
    pal::debugger_state_t state = pal::is_debugger_attached();
    if (state == pal::debugger_state_t::na)
    {
        pal::fprintf(stdout, W("Debugger attach is not available on this platform\n"));
        return;
    }
    else if (state == pal::debugger_state_t::not_attached)
    {
        uint32_t pid = pal::get_process_id();
        pal::fprintf(stdout, W("Waiting for the debugger to attach (PID: %u). Press any key to continue ...\n"), pid);
        (void)getchar();
        state = pal::is_debugger_attached();
    }

    if (state == pal::debugger_state_t::attached)
    {
        pal::fprintf(stdout, W("Debugger is attached.\n"));
    }
    else
    {
        pal::fprintf(stdout, W("Debugger failed to attach.\n"));
    }
}

// N.B. It seems that CoreCLR doesn't always use the first instance of an assembly on the TPA list
// (for example, ni's may be preferred over il, even if they appear later). Therefore, when building
// the TPA only include the first instance of a simple assembly name to allow users the opportunity to
// override Framework assemblies by placing dlls in %CORE_LIBRARIES%.
static string_t build_tpa(const string_t& core_root, const string_t& core_libraries)
{
    static const char_t* const tpa_extensions[] =
    {
        W(".ni.dll"),  // Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
        W(".dll"),
        W(".ni.exe"),
        W(".exe"),
        nullptr
    };

    std::set<string_t> name_set;
    pal::stringstream_t tpa_list;

    // Iterate over all extensions.
    for (const char_t* const* curr_ext = tpa_extensions; *curr_ext != nullptr; ++curr_ext)
    {
        const char_t* ext = *curr_ext;
        const size_t ext_len = pal::strlen(ext);

        // Iterate over all supplied directories.
        for (const string_t& dir : { core_libraries, core_root })
        {
            if (dir.empty())
                continue;

            assert(dir.back() == pal::dir_delim);
            string_t tmp = pal::build_file_list(dir, ext, [&](const char_t* file)
                {
                    string_t file_local{ file };

                    // Strip the extension.
                    if (pal::string_ends_with(file_local, ext_len, ext))
                        file_local = file_local.substr(0, file_local.length() - ext_len);

                    // Return true if the file is new.
                    return name_set.insert(file_local).second;
                });

            // Add to the TPA.
            tpa_list << tmp;
        }
    }

    return tpa_list.str();
}

static bool try_get_export(pal::mod_t mod, const char* symbol, void** fptr)
{
    assert(mod != nullptr && symbol != nullptr && fptr != nullptr);
    *fptr = pal::get_module_symbol(mod, symbol);
    if (*fptr != nullptr)
        return true;

    pal::fprintf(stderr, W("Export '%s' not found.\n"), symbol);
    return false;
}

class logger_t final
{
    const char* _exePath;
    int _propertyCount;
    const char** _propertyKeys;
    const char** _propertyValues;
    const char* _managedAssembly;
    int _argc;
    const char** _argv;
public:
    logger_t(
        const char* exePath,
        int propertyCount, const char** propertyKeys, const char** propertyValues,
        const char* managedAssembly, int argc, const char** argv)
    : _exePath{ exePath }
    , _propertyCount{ propertyCount }
    , _propertyKeys{ propertyKeys }
    , _propertyValues{ propertyValues }
    , _managedAssembly{ managedAssembly }
    , _argc{ argc }
    , _argv{ argv }
    { }

    void dump_details(FILE* fd = stdout)
    {
        // Using std::fprintf since values have been converted to UTF-8.
        std::fprintf(fd, "Exe path: %s\n", _exePath);
        std::fprintf(fd, "Properties:\n");
        for (int i = 0; i < _propertyCount; ++i)
        {
            std::fprintf(fd, "    %s = %s\n", _propertyKeys[i], _propertyValues[i]);
        }

        std::fprintf(fd, "Managed assembly: %s\n", _managedAssembly);
        std::fprintf(fd, "Arguments (%d): ", _argc);
        for (int i = 0; i < _argc; ++i)
        {
            std::fprintf(fd, "%s ", _argv[i]);
        }
        std::fprintf(fd, "\n");
    }
};

// The current CoreCLR instance details.
static void* CurrentClrInstance;
static unsigned int CurrentAppDomainId;

static int run(const configuration& config)
{
    platform_specific_actions actions;

    // Check if debugger attach scenario was requested.
    if (config.wait_to_debug)
        wait_for_debugger();

    string_t exe_path = pal::get_exe_path();

    // Determine the managed application's path.
    string_t app_path;
    {
        string_t file;
        pal::split_path_to_dir_filename(config.entry_assembly_fullpath, app_path, file);
        pal::ensure_trailing_delimiter(app_path);
    }

    // Accumulate path for native search path.
    pal::stringstream_t native_search_dirs;
    native_search_dirs << app_path << pal::env_path_delim;

    // CORE_LIBRARIES
    string_t core_libs = pal::getenv(envvar::coreLibraries);
    if (!core_libs.empty() && core_libs != app_path)
    {
        pal::ensure_trailing_delimiter(core_libs);
        native_search_dirs << core_libs << pal::env_path_delim;
    }

    // Determine CORE_ROOT.
    // Check if the path is user supplied and if not try
    // the CORE_ROOT environment variable.
    string_t core_root = !config.clr_path.empty()
        ? config.clr_path
        : pal::getenv(envvar::coreRoot);

    // If CORE_ROOT wasn't supplied use the exe binary path, otherwise
    // ensure path is valid and add to native search path.
    if (core_root.empty())
    {
        string_t file;
        pal::split_path_to_dir_filename(exe_path, core_root, file);
        pal::ensure_trailing_delimiter(core_root);
    }
    else
    {
        pal::ensure_trailing_delimiter(core_root);
        native_search_dirs << core_root << pal::env_path_delim;
    }

    string_t tpa_list = build_tpa(core_root, core_libs);

    {
        // Load hostpolicy if requested.
        string_t mock_hostpolicy = pal::getenv(envvar::mockHostPolicy);
        if (!mock_hostpolicy.empty()
            && !pal::try_load_hostpolicy(mock_hostpolicy))
        {
            return -1;
        }
    }

    config.dotenv_configuration.load_into_current_process();

    actions.before_coreclr_load();

    // Attempt to load CoreCLR.
    pal::mod_t coreclr_mod;
    if (!pal::try_load_coreclr(core_root, coreclr_mod))
    {
        return -1;
    }

    // Get CoreCLR exports
    coreclr_initialize_ptr coreclr_init_func = nullptr;
    coreclr_execute_assembly_ptr coreclr_execute_func = nullptr;
    coreclr_shutdown_2_ptr coreclr_shutdown2_func = nullptr;
    if (!try_get_export(coreclr_mod, "coreclr_initialize", (void**)&coreclr_init_func)
        || !try_get_export(coreclr_mod, "coreclr_execute_assembly", (void**)&coreclr_execute_func)
        || !try_get_export(coreclr_mod, "coreclr_shutdown_2", (void**)&coreclr_shutdown2_func))
    {
        return -1;
    }

    // Construct CoreCLR properties.
    pal::string_utf8_t tpa_list_utf8 = pal::convert_to_utf8(std::move(tpa_list));
    pal::string_utf8_t app_path_utf8 = pal::convert_to_utf8(std::move(app_path));
    pal::string_utf8_t native_search_dirs_utf8 = pal::convert_to_utf8(native_search_dirs.str());

    std::vector<pal::string_utf8_t> user_defined_keys_utf8;
    std::vector<pal::string_utf8_t> user_defined_values_utf8;
    for (const string_t& str : config.user_defined_keys)
        user_defined_keys_utf8.push_back(pal::convert_to_utf8(str.c_str()));
    for (const string_t& str : config.user_defined_values)
        user_defined_values_utf8.push_back(pal::convert_to_utf8(str.c_str()));

    // Set base initialization properties.
    std::vector<const char*> propertyKeys;
    std::vector<const char*> propertyValues;

    // TRUSTED_PLATFORM_ASSEMBLIES
    // - The list of complete paths to each of the fully trusted assemblies
    propertyKeys.push_back("TRUSTED_PLATFORM_ASSEMBLIES");
    propertyValues.push_back(tpa_list_utf8.c_str());

    // APP_PATHS
    // - The list of paths which will be probed by the assembly loader
    propertyKeys.push_back("APP_PATHS");
    propertyValues.push_back(app_path_utf8.c_str());

    // NATIVE_DLL_SEARCH_DIRECTORIES
    // - The list of paths that will be probed for native DLLs called by PInvoke
    propertyKeys.push_back("NATIVE_DLL_SEARCH_DIRECTORIES");
    propertyValues.push_back(native_search_dirs_utf8.c_str());

    // Sanity check before adding user-defined properties
    assert(propertyKeys.size() == propertyValues.size());

    // Insert user defined properties
    for (const pal::string_utf8_t& str : user_defined_keys_utf8)
        propertyKeys.push_back(str.c_str());
    for (const pal::string_utf8_t& str : user_defined_values_utf8)
        propertyValues.push_back(str.c_str());

    assert(propertyKeys.size() == propertyValues.size());
    int propertyCount = (int)propertyKeys.size();

    // Construct arguments
    pal::string_utf8_t exe_path_utf8 = pal::convert_to_utf8(std::move(exe_path));
    std::vector<pal::string_utf8_t> argv_lifetime;
    pal::malloc_ptr<const char*> argv_utf8{ pal::convert_argv_to_utf8(config.entry_assembly_argc, config.entry_assembly_argv, argv_lifetime) };
    pal::string_utf8_t entry_assembly_utf8 = pal::convert_to_utf8(config.entry_assembly_fullpath.c_str());

    logger_t logger{
        exe_path_utf8.c_str(),
        propertyCount, propertyKeys.data(), propertyValues.data(),
        entry_assembly_utf8.c_str(), config.entry_assembly_argc, argv_utf8.get() };

    int result;
    result = coreclr_init_func(
        exe_path_utf8.c_str(),
        "corerun",
        propertyCount,
        propertyKeys.data(),
        propertyValues.data(),
        &CurrentClrInstance,
        &CurrentAppDomainId);
    if (FAILED(result))
    {
        pal::fprintf(stderr, W("BEGIN: coreclr_initialize failed - Error: 0x%08x\n"), result);
        logger.dump_details();
        pal::fprintf(stderr, W("END: coreclr_initialize failed - Error: 0x%08x\n"), result);
        return -1;
    }

    int exit_code;
    {
        actions.before_execute_assembly(config.entry_assembly_fullpath);

        result = coreclr_execute_func(
            CurrentClrInstance,
            CurrentAppDomainId,
            config.entry_assembly_argc,
            argv_utf8.get(),
            entry_assembly_utf8.c_str(),
            (uint32_t*)&exit_code);
        if (FAILED(result))
        {
            pal::fprintf(stderr, W("BEGIN: coreclr_execute_assembly failed - Error: 0x%08x\n"), result);
            logger.dump_details();
            pal::fprintf(stderr, W("END: coreclr_execute_assembly failed - Error: 0x%08x\n"), result);
            return -1;
        }

        actions.after_execute_assembly();
    }

    int latched_exit_code = 0;
    result = coreclr_shutdown2_func(CurrentClrInstance, CurrentAppDomainId, &latched_exit_code);
    if (FAILED(result))
    {
        pal::fprintf(stderr, W("coreclr_shutdown_2 failed - Error: 0x%08x\n"), result);
        exit_code = -1;
    }

    if (exit_code != -1)
        exit_code = latched_exit_code;

    return exit_code;
}

// Display the command line options
static void display_usage()
{
    pal::fprintf(
        stderr,
        W("USAGE: corerun [OPTIONS] assembly [ARGUMENTS]\n")
        W("\n")
        W("Execute the managed assembly with the passed in arguments\n")
        W("\n")
        W("Options:\n")
        W("  -c, --clr-path - path to CoreCLR binary and managed CLR assemblies.\n")
        W("  -p, --property - Property to pass to runtime during initialization.\n")
        W("                   If a property value contains spaces, quote the entire argument.\n")
        W("                   May be supplied multiple times. Format: <key>=<value>.\n")
        W("  -d, --debug - causes corerun to wait for a debugger to attach before executing.\n")
        W("  -e, --env - path to a .env file with environment variables that corerun should set.\n")
        W("  -?, -h, --help - show this help.\n")
        W("\n")
        W("The runtime binary is searched for in --clr-path, CORE_ROOT environment variable, then\n")
        W("in the directory the corerun binary is located.\n")
        W("\n")
        W("Example:\n")
        W("Wait for a debugger to attach, provide 2 additional properties for .NET\n")
        W("runtime initialization, and pass an argument to the HelloWorld.dll assembly.\n")
        W("  corerun -d -p System.GC.Concurrent=true -p \"FancyProp=/usr/first last/root\" HelloWorld.dll arg1\n")
        );
}

// Parse the command line arguments
static bool parse_args(
    const int argc,
    const char_t* argv[],
    configuration& config)
{
    // The command line must contain at least the current exe name and the managed assembly path.
    if (argc < 2)
    {
        display_usage();
        return false;
    }

    for (int i = 1; i < argc; i++)
    {
        bool is_option = pal::is_cli_option(argv[i][0]);

        // First argument that is not an option is the managed assembly to execute.
        if (!is_option)
        {
            config.entry_assembly_fullpath = pal::get_absolute_path(argv[i]);
            i++; // Move to next argument.

            config.entry_assembly_argc = argc - i;
            config.entry_assembly_argv = (const char_t**)::malloc(config.entry_assembly_argc * sizeof(const char_t*));
            assert(config.entry_assembly_argv != nullptr);
            for (int c = 0; c < config.entry_assembly_argc; ++c)
            {
                config.entry_assembly_argv[c] = pal::strdup(argv[i + c]);
            }

            // Successfully parsed arguments.
            return true;
        }

        const char_t* arg = argv[i];
        size_t arg_len = pal::strlen(arg);
        if (arg_len == 1)
        {
            pal::fprintf(stderr, W("Option %s: invalid form\n"), arg);
            break; // Invalid option
        }

        const char_t* option = arg + 1;
        if (option[0] == W('-')) // Handle double '--'
            option++;

        // Path to core_root
        if (pal::strcmp(option, W("c")) == 0 || (pal::strcmp(option, W("clr-path")) == 0))
        {
            i++;
            if (i < argc)
            {
                config.clr_path = argv[i];
            }
            else
            {
                pal::fprintf(stderr, W("Option %s: missing path\n"), arg);
                break;
            }
        }
        else if (pal::strcmp(option, W("p")) == 0 || (pal::strcmp(option, W("property")) == 0))
        {
            i++;
            if (i >= argc)
            {
                pal::fprintf(stderr, W("Option %s: missing property\n"), arg);
                break;
            }

            string_t prop = argv[i];
            size_t delim_maybe = prop.find(W('='));
            if (delim_maybe == string_t::npos)
            {
                pal::fprintf(stderr, W("Option %s: '%s' missing property value\n"), arg, prop.c_str());
                break;
            }

            string_t key = prop.substr(0, delim_maybe);
            string_t value = prop.substr(delim_maybe + 1);
            config.user_defined_keys.push_back(std::move(key));
            config.user_defined_values.push_back(std::move(value));
        }
        else if (pal::strcmp(option, W("d")) == 0 || (pal::strcmp(option, W("debug")) == 0))
        {
            config.wait_to_debug = true;
        }
        else if (pal::strcmp(option, W("st")) == 0)
        {
            config.self_test = true;
            return true;
        }
        else if (pal::strcmp(option, W("e")) == 0 || (pal::strcmp(option, W("env")) == 0))
        {
            i++;
            if (i >= argc)
            {
                pal::fprintf(stderr, W("Option %s: missing .env file path\n"), arg);
                break;
            }

            std::ifstream dotenvFile{ pal::convert_to_utf8(argv[i]) };
            config.dotenv_configuration = dotenv{ pal::string_t{ argv[i] }, dotenvFile};
        }
        else if ((pal::strcmp(option, W("?")) == 0 || (pal::strcmp(option, W("h")) == 0 || (pal::strcmp(option, W("help")) == 0))))
        {
            display_usage();
            break;
        }
        else
        {
            pal::fprintf(stderr, W("Unknown option %s\n"), arg);
            break;
        }
    }

    return false;
}

// Forward declaration for self testing method.
static int self_test();

//
// Entry points
//

int MAIN(const int argc, const char_t* argv[])
{
    configuration config{};
    if (!parse_args(argc, argv, config))
        return EXIT_FAILURE;

    if (config.self_test)
        return self_test();

    int exit_code = run(config);
    return exit_code;
}

#ifdef TARGET_WINDOWS
// Used by CoreShim to determine running CoreCLR details.
extern "C" __declspec(dllexport) HRESULT __cdecl GetCurrentClrDetails(void** clrInstance, unsigned int* appDomainId)
{
    assert(clrInstance != nullptr && appDomainId != nullptr);
    *clrInstance = CurrentClrInstance;
    *appDomainId = CurrentAppDomainId;
    return S_OK;
}
#endif // TARGET_WINDOWS

//
// Self testing for corerun.
//

#define THROW_IF_FALSE(stmt) if (!(stmt)) throw W(#stmt);
#define THROW_IF_TRUE(stmt) if (stmt) throw W(#stmt);
static int self_test()
{
    try
    {
        {
            configuration config{};
            const char_t* args[] = { W(""), W("-d"), W("foo") };
            THROW_IF_FALSE(parse_args(3, args, config));
            THROW_IF_FALSE(config.wait_to_debug);
            THROW_IF_FALSE(config.clr_path.empty());
            THROW_IF_FALSE(!config.entry_assembly_fullpath.empty());
            THROW_IF_FALSE(config.entry_assembly_argc == 0);
        }
        {
            configuration config{};
            const char_t* args[] = { W(""), W("-d"), W("foo"), W("1"), W("2"), W("3") };
            THROW_IF_FALSE(parse_args(6, args, config));
            THROW_IF_FALSE(config.wait_to_debug);
            THROW_IF_FALSE(!config.entry_assembly_fullpath.empty());
            THROW_IF_FALSE(config.entry_assembly_argc == 3);
        }
        {
            configuration config{};
            const char_t* args[] = { W(""), W("--clr-path"), W("path"), W("foo"), W("1") };
            THROW_IF_FALSE(parse_args(5, args, config));
            THROW_IF_FALSE(!config.wait_to_debug);
            THROW_IF_FALSE(config.clr_path == W("path"));
            THROW_IF_FALSE(!config.entry_assembly_fullpath.empty());
            THROW_IF_FALSE(config.entry_assembly_argc == 1);
        }
        {
            configuration config{};
            const char_t* args[] = { W(""), W("-p"), W("invalid"), W("foo") };
            THROW_IF_TRUE(parse_args(4, args, config));
        }
        {
            configuration config{};
            const char_t* args[] = { W(""), W("-p"), W("empty="), W("foo") };
            THROW_IF_FALSE(parse_args(4, args, config));
            THROW_IF_FALSE(config.user_defined_keys.size() == 1);
            THROW_IF_FALSE(config.user_defined_values.size() == 1);
            THROW_IF_FALSE(!config.entry_assembly_fullpath.empty());
        }
        {
            configuration config{};
            const char_t* args[] = { W(""), W("-p"), W("one=1"), W("foo") };
            THROW_IF_FALSE(parse_args(4, args, config));
            THROW_IF_FALSE(config.user_defined_keys.size() == 1);
            THROW_IF_FALSE(config.user_defined_values.size() == 1);
            THROW_IF_FALSE(!config.entry_assembly_fullpath.empty());
        }
        {
            configuration config{};
            const char_t* args[] = { W(""), W("-p"), W("one=1"), W("--property"), W("System.GC.Concurrent=true"), W("foo") };
            THROW_IF_FALSE(parse_args(6, args, config));
            THROW_IF_FALSE(config.user_defined_keys.size() == 2);
            THROW_IF_FALSE(config.user_defined_values.size() == 2);
            THROW_IF_FALSE(!config.entry_assembly_fullpath.empty());
        }
        {
            string_t path;
            path = W("path");
            pal::ensure_trailing_delimiter(path);
            THROW_IF_FALSE(path.back() == pal::dir_delim);
            path = W("");
            pal::ensure_trailing_delimiter(path);
            THROW_IF_FALSE(path.back() == pal::dir_delim);
            path = W("\\");
            pal::ensure_trailing_delimiter(path);
            THROW_IF_FALSE(path.back() == pal::dir_delim || path.length() == 1);
            path = W("/");
            pal::ensure_trailing_delimiter(path);
            THROW_IF_FALSE(path.back() == pal::dir_delim || path.length() == 1);
        }
        {
            THROW_IF_FALSE(!pal::string_ends_with(W(""), W(".cd")));
            THROW_IF_FALSE(pal::string_ends_with(W("ab.cd"), W(".cd")));
            THROW_IF_FALSE(!pal::string_ends_with(W("ab.cd"), W(".cde")));
            THROW_IF_FALSE(!pal::string_ends_with(W("ab.cd"), W("ab.cde")));
        }
        {
            dotenv::self_test();
        }
    }
    catch (const char_t msg[])
    {
        pal::fprintf(stderr, W("Fail: %s\n"), msg);
        return EXIT_FAILURE;
    }

    pal::fprintf(stdout, W("Self-test passed.\n"));
    return EXIT_SUCCESS;
}
