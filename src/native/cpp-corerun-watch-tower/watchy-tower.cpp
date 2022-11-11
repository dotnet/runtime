// File: watchy-tower.cpp

// Will replace these includes with Microsoft .NET's ones if necessary afterwards :)
#include <cstdio>
#include <cstdlib>
#include <cstring>

struct configuration
{
    configuration() = default;

    long int timeout;
    const char *corerun_path;
    const char *corerun_args;
};

static void display_usage();
static bool parse_args(const int, const char *[], configuration&);

int main(const int argc, const char *argv[])
{
    configuration config {};

    if (!parse_args(argc, argv, config))
        return EXIT_FAILURE;

    printf("Timeout Given: %ld\n", config.timeout);
    printf("Corerun Path Given: %s\n", config.corerun_path);
    printf("Corerun Arguments Given: %s\n", config.corerun_args);
    return EXIT_SUCCESS;
}

static void display_usage()
{
    fprintf(stderr, "Help will go here.\n");
    return ;
}

static bool parse_args(const int argc, const char *argv[], configuration& config)
{
    if (argc < 2)
    {
        display_usage();
        return false;
    }

    for (int i = 1; i < argc; i++)
    {
        const char *arg = argv[i];
        // printf("%s\n", argv[i]);

        if (strcmp(arg, "-timeout") == 0 || strcmp(arg, "--timeout") == 0)
        {
            if (++i < argc)
            {
                config.timeout = strtol(argv[i], nullptr, 10);
            }
            else
            {
                fprintf(stderr, "Option '%s': Missing value in seconds.\n", arg);
                return false;
            }
        }
        else if (strcmp(arg, "-corerun") == 0 || strcmp(arg, "--corerun") == 0)
        {
            if (++i < argc)
            {
                config.corerun_path = argv[i];
            }
            else
            {
                fprintf(stderr, "Option '%s': Missing path to corerun executable.\n", arg);
                return false;
            }
        }
        else if (strcmp(arg, "-args") == 0 || strcmp(arg, "--args") == 0)
        {
            if (++i < argc)
            {
                config.corerun_args = argv[i];
            }
            else
            {
                fprintf(stderr, "Option '%s': Missing arguments for corerun.\n", arg);
                return false;
            }
        }
        else if (strcmp(arg, "-h") == 0 || strcmp(arg, "--h") == 0 || strcmp(arg, "-?") == 0)
        {
            display_usage();
            return true;
        }
        else
        {
            fprintf(stderr, "Unknown option '%s'.\n", arg);
            return false;
        }

    }

    return true;
}
