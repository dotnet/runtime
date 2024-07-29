// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstdio>
#include <cstdlib>
#include <errno.h>
#include <signal.h>

#ifdef TARGET_WINDOWS

#include <windows.h>
#include <string>

#else // !TARGET_WINDOWS

#include <chrono>
#include <sys/wait.h>
#include <thread>
#include <unistd.h>
#include <vector>

#endif // TARGET_WINDOWS

int run_timed_process(const long, const int, const char *[]);

#ifdef TARGET_X86
int __cdecl main(const int argc, const char *argv[])
#else
int main(const int argc, const char *argv[])
#endif
{
    if (argc < 3)
    {
        printf("There are missing arguments. Got %d instead of 3+ :(\n", argc);
        return EXIT_FAILURE;
    }

    // Due to how Helix test environment variables are set, we have to receive
    // the raw timeout value in minutes. Then we convert it to milliseconds when
    // calling run_timed_process().
    const long timeout_mins = strtol(argv[1], nullptr, 10);
    int exit_code = run_timed_process(timeout_mins * 60000L, argc-2, &argv[2]);

    printf("App Exit Code: %d\n", exit_code);
    return exit_code;
}

int run_timed_process(const long timeout_ms, const int proc_argc, const char *proc_argv[])
{
#ifdef TARGET_WINDOWS
    std::string cmdline(proc_argv[0]);

    for (int i = 1; i < proc_argc; i++)
    {
        cmdline.append(" ");
        cmdline.append(proc_argv[i]);
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

    WaitForSingleObject(proc_info.hProcess, timeout_ms);
    GetExitCodeProcess(proc_info.hProcess, &exit_code);

    CloseHandle(proc_info.hProcess);
    CloseHandle(proc_info.hThread);
    return exit_code;

#else // !TARGET_WINDOWS

    const int check_interval_ms = 25;
    int check_count = 0;
    std::vector<const char*> args;

    pid_t child_pid;
    int child_status;
    int wait_code;

    for (int i = 0; i < proc_argc; i++)
    {
        args.push_back(proc_argv[i]);
    }
    args.push_back(NULL);

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
        execv(args[0], const_cast<char* const*>(args.data()));
    }
    else
    {
        do
        {
            // Instructions for the parent process!
            wait_code = waitpid(child_pid, &child_status, WNOHANG);

            if (wait_code == -1)
                return EINVAL;

            std::this_thread::sleep_for(std::chrono::milliseconds(check_interval_ms));

            if (wait_code)
            {
                if (WIFEXITED(child_status))
                    return WEXITSTATUS(child_status);
            }
            check_count++;

        } while (check_count < (timeout_ms / check_interval_ms));
    }

    printf("Child process took too long. Timed out... Exiting...\n");
    kill(child_pid, SIGKILL);

#endif // TARGET_WINDOWS
    return ETIMEDOUT;
}

