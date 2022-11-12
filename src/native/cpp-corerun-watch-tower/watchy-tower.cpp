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

/* Little example of forking a child process, and finishing it instantly if it
 * takes longer than a certain amount of time.

#include <cstdio>
#include <cstdlib>
#include <cstdarg>
#include <unistd.h>
#include <errno.h>
#include <signal.h>
#include <sys/wait.h>

int run_timed_process(const long, const int, const char *[]);

int main(const int argc, const char *argv[])
{
    int result = run_timed_process(3000L, argc-1, &argv[1]);
    printf("App Exit Code: %d\n", result);
    return 0;
}

int run_timed_process(const long timeout_ms, const int program_argc, const char *program_argv[])
{
//     for (int i = 0; i < program_argc; i++)
//         printf("Argv[%d] = %s\n", i, program_argv[i]);
//
    const int check_interval = 1000;
    int check_count = 0;
    char *args[program_argc];

    pid_t child_pid;
    int child_status;
    int w;

    for (int i = 0; i < program_argc; i++)
    {
        args[i] = (char *)program_argv[i];
    }

    printf("Forking!\n");
    child_pid = fork();

    if (child_pid < 0)
    {
        // Fork failed. No memory remaining available :(
        printf("Fork failed... Returning ENOMEM.\n");
        return ENOMEM;
    }
    else if (child_pid == 0)
    {
        // Instructions for child process!
        printf("Fork successful! Running child process now...\n");
        execv(args[0], &args[0]);
        _exit(EXIT_FAILURE);
    }
    else
    {
        do
        {
            // Instructions for the parent process!
            w = waitpid(child_pid, &child_status, WNOHANG);

            if (w == -1)
                return EINVAL;

            usleep(check_interval * 1000);

            if (w)
            {
                if (WIFEXITED(child_status))
                {
                    printf("Child process exited by signal %d.\n", WEXITSTATUS(child_status));
                    return WEXITSTATUS(child_status);
                }
            }
        } while (check_count++ < (timeout_ms / check_interval));
    }

    printf("Child process took too long. Timed out... Exiting...\n");
    kill(child_pid, SIGKILL);
    return ETIMEDOUT;
}

*/
