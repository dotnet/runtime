#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <errno.h>
#include <signal.h>

#ifdef _WIN32
#include <windows.h>
#include <string>
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
    return exit_code;
}

int run_timed_process(const long timeout, const int exe_argc, const char *exe_path_and_argv[])
{
#ifdef _WIN32
    std::string cmdline(exe_path_and_argv[0]);

    for (int i = 1; i < exe_argc; i++)
    {
        cmdline.append(" ");
        cmdline.append(exe_path_and_argv[i]);
    }

    STARTUPINFOA startup_info;
    PROCESS_INFORMATION proc_info;
    unsigned long exit_code;

    ZeroMemory(&startup_info, sizeof(startup_info));
    startup_info.cb = sizeof(startup_info);
    ZeroMemory(&proc_info, sizeof(proc_info));

    if (!CreateProcessA(NULL, &cmdline[0], NULL, NULL, FALSE, 0, NULL, NULL,
                       &startup_info, &proc_info))
    {
        int error_code = GetLastError();
        printf("Process creation failed... Code %d.\n", error_code);
        return error_code;
    }

    WaitForSingleObject(proc_info.hProcess, timeout);
    GetExitCodeProcess(proc_info.hProcess, &exit_code);

    CloseHandle(proc_info.hProcess);
    CloseHandle(proc_info.hThread);
    return exit_code;

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

            // TODO: Explain why we are multiplying by 25 here, and dividing
            // by 40 in the while clause.
            usleep(check_interval * 25);

            if (wait_code)
            {
                if (WIFEXITED(child_status))
                {
                    printf("Child process exited successfully with status %d.\n",
                            WEXITSTATUS(child_status));
                    return WEXITSTATUS(child_status);
                }
            }
        } while (check_count++ < ((timeout / check_interval) * 40));
    }

    printf("Child process took too long and timed out... Exiting it...\n");
    kill(child_pid, SIGKILL);
#endif
    return ETIMEDOUT;
}

