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
    if (argc < 3)
    {
        printf("There are missing arguments. Got %d instead of 3+ :(\n", argc);
        return EXIT_FAILURE;
    }

    const long timeout_ms = strtol(argv[1], nullptr, 10);
    int exit_code = run_timed_process(timeout_ms, argc-2, &argv[2]);

    printf("App Exit Code: %d\n", exit_code);
    return EXIT_SUCCESS;
}

int run_timed_process(const long timeout, const int exe_argc, const char *exe_path_and_argv[])
{
    const int check_interval = 1000;
    int check_count = 0;
    char *args[exe_argc];

    pid_t child_pid;
    int child_status;
    int wait_code;

    for (int i = 0; i < exe_argc; i++)
    {
        args[i] = (char *) exe_path_and_argv[i];
    }

    // This is just for development. Will remove it when it's ready to be submitted :)
    for (int j = 0; j < exe_argc; j++)
    {
        printf("[%d]: %s\n", j, args[j]);
    }

    child_pid = fork();

    if (child_pid < 0)
    {
        printf("Fork failed... No memory available.\n");
        return ENOMEM;
    }
    else if (child_pid == 0)
    {
        printf("Running child process...\n");
        execv(args[0], &args[0]);
    }
    else
    {
        do
        {
            wait_code = waitpid(child_pid, &child_status, WNOHANG);

            // Something went terribly wrong.
            if (wait_code == -1)
                return EINVAL;

            // Passing ms * 1000 because usleep() receives its parameter in microseconds.
            usleep(check_interval * 1000);

            if (wait_code)
            {
                if (WIFEXITED(child_status))
                {
                    printf("Child process exited successfully with status %d.\n",
                            WEXITSTATUS(child_status));
                    return WEXITSTATUS(child_status);
                }
            }
        } while (check_count++ < (timeout / check_interval));
    }

    printf("Child process took too long and timed out... Exiting it...\n");
    kill(child_pid, SIGKILL);
    return ETIMEDOUT;
}

