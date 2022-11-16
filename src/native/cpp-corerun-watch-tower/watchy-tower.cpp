// File: watchy-tower.cpp

// Will replace these includes with Microsoft .NET's ones if necessary afterwards :)
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstdarg>
#include <vector>
#include <unistd.h>
#include <errno.h>
#include <signal.h>
#include <sys/wait.h>

struct configuration
{
    configuration() = default;

    long int timeout;
    const char *corerun_path;
    std::vector<const char *> corerun_argv;
};

static void display_usage();
static bool parse_args(const int, const char *[], configuration&);
static int run_timed_process(configuration&);

int main(const int argc, const char *argv[])
{
    configuration config {};

    if (!parse_args(argc, argv, config))
        return EXIT_FAILURE;

    // These printf's are just for general info during development. They will
    // be removed before merging the PR.
    printf("Timeout Given: %ld\n", config.timeout);
    printf("Corerun Path Given: %s\n", config.corerun_path);
    printf("Corerun Arguments Given:\n");
    int r = run_timed_process(config);
    printf("%d\n", r);
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
            // The args flag has to come last, since we can't know beforehand
            // how many of the following arguments belong to this flag. So, making
            // it last-only, we can assume it's until we're done processing
            // the argument vector.
            while (++i < argc)
            {
                config.corerun_argv.push_back(argv[i]);
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

static int run_timed_process(configuration& config)
{
    const int check_interval_ms = 1000;
    int check_count = 0;
    int corerun_argc = config.corerun_argv.size();

    // Since our corerun_argv only contains the actual arguments to corerun,
    // but the execv() call expects a list including the name of the executable,
    // we make an additional slot in our list to add it.
    char *program_args[corerun_argc + 1];
    program_args[0] = (char *) config.corerun_path;

    pid_t child_pid;
    int child_status;
    int w;

    // The calls to execute child processes require a char * array.
    for (int i = 0; i < corerun_argc; i++)
    {
        // We're using index+1 to account for the corerun executable path stored
        // at index 0.
        program_args[i+1] = (char *) config.corerun_argv.at(i);
    }

    for (int j = 0; j <= corerun_argc; j++)
        printf("At [%d]: %s\n", j, program_args[j]);
    return 100;

    child_pid = fork();

    if (child_pid < 0)
    {
        // Fork failed. No memory available.
        printf("Fork failed... Out of memory.\n");
        return ENOMEM;
    }
    else if (child_pid == 0)
    {
        // Instructions for the child process!

        // Run the test binary and exit unsuccessfully if it's killed or dies for
        // whatever reason.
        execv(program_args[0], &program_args[0]);
        _exit(EXIT_FAILURE);
    }
    else
    {
        // Instructions for the parent process!

        // Wait for the child process (running test) to finish, while keeping
        // track of how long it's been running.
        do
        {
            w = waitpid(child_pid, &child_status, WNOHANG);

            // Something went very wrong.
            if (w == -1)
                return EINVAL;

            // Wait a bit before checking the test process' status again.
            // Usleep() takes its argument in microseconds, hence we multiply
            // by 1000 our interval in milliseconds.
            usleep(check_interval_ms * 1000);

            if (w)
            {
                if (WIFEXITED(child_status))
                {
                    // Our test run completed successfully.
                    return WEXITSTATUS(child_status);
                }
            }
        } while (check_count++ < (config.timeout / check_interval_ms));
    }

    printf("Test process took too long to complete, and timed out.\n");
    kill(child_pid, SIGKILL);
    return ETIMEDOUT;
}
