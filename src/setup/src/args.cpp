#include "args.h"
#include "utils.h"

arguments_t::arguments_t() :
    trace_level(trace::level_t::Error),
    managed_application(_X("")),
    clr_path(_X("")),
    app_argc(0),
    app_argv(nullptr)
{
}

void display_help()
{
    xerr <<
        _X("Usage: " HOST_EXE_NAME " [ASSEMBLY] [ARGUMENTS]\n")
        _X("Execute the specified managed assembly with the passed in arguments\n\n")
        _X("The Host's behavior can be altered using the following environment variables:\n")
        _X(" CLRHOST_CLR_PATH       Set the directory which contains the CoreCLR runtime. Overrides all other values for CLR search paths\n")
        _X(" CLRHOST_TRACE          Set to affect trace levels (0 = Errors only (default), 1 = Warnings, 2 = Info, 3 = Verbose)\n");
}

bool parse_arguments(const int argc, const pal::char_t* argv[], arguments_t& args)
{
    // Get the full name of the application
    if (!pal::get_own_executable_path(args.own_path) || !pal::realpath(args.own_path))
    {
        trace::error(_X("failed to locate current executable"));
        return false;
    }

    auto own_name = get_filename(args.own_path);
    auto own_dir = get_directory(args.own_path);

    if (own_name.compare(HOST_EXE_NAME) == 0)
    {
        // corerun mode. First argument is managed app
        if (argc < 2)
        {
            display_help();
            return false;
        }
        args.managed_application = pal::string_t(argv[1]);
        args.app_argc = argc - 2;
        args.app_argv = &argv[2];
    }
    else
    {
        // coreconsole mode. Find the managed app in the same directory
        pal::string_t managed_app(own_dir);
        managed_app.push_back(DIR_SEPARATOR);
        managed_app.append(change_extension(own_name, _X(".dll")));
        args.managed_application = managed_app;
        args.app_argv = &argv[1];
        args.app_argc = argc - 1;
    }

    // Read trace environment variable
    pal::string_t trace_str;
    if (pal::getenv(_X("CLRHOST_TRACE"), trace_str))
    {
        auto trace_val = pal::xtoi(trace_str.c_str());
        if (trace_val >= (int)trace::level_t::Error && trace_val <= (int)trace::level_t::Verbose)
        {
            args.trace_level = (trace::level_t)trace_val;
        }
    }

    // Read CLR path from environment variable
    pal::getenv(_X("CLRHOST_CLR_PATH"), args.clr_path);

    return true;
}
