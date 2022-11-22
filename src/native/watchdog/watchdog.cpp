#include <cstdio>
#include <cstdlib>
#include <cstdarg>
#include <errno.h>
#include <signal.h>

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#include <sys/wait.h>
#endif

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
    // We somehow need to convert the whole command-line to a single string for Windows :|
#ifdef _WIN32
    STARTUPINFO startupInfo;
    PROCESS_INFORMATION procInfo;

    ZeroMemory(&startupInfo, sizeof(startupInfo));
    startupInfo.cb = sizeof(startupInfo);
    ZeroMemory(&procInfo, sizeof(procInfo));

    if (!CreateProcess(NULL, "cmdline", NULL, NULL, FALSE, 0, NULL, NULL,
                       &startupInfo, &procInfo))
    {
        int error_code = GetLastError();
        printf("Process creation failed... Code %d.\n", error_code);
        return error_code;
    }

    WaitForSingleObject(procInfo.hProcess, timeout);
    CloseHandle(procInfo.hProcess);
    CloseHandle(procInfo.hThread);

#else
    const int check_interval = 1000;
    int check_count = 0;
    char **args = new char *[exe_argc];

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
#endif
    return ETIMEDOUT;
}

