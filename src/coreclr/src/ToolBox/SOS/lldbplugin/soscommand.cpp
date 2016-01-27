// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "sosplugin.h"
#include <dlfcn.h>
#include <string.h>
#include <string>

class sosCommand : public lldb::SBCommandPluginInterface
{
    const char *m_command;
    void *m_sosHandle;

public:
    sosCommand(const char *command)
    {
        m_command = command;
        m_sosHandle = NULL;
    }

    virtual bool
    DoExecute (lldb::SBDebugger debugger,
               char** arguments,
               lldb::SBCommandReturnObject &result)
    {
        DebugClient* client = new DebugClient(debugger, result);
        LoadSos(client);

        if (m_sosHandle)
        {
            const char* sosCommand = m_command;
            if (sosCommand == NULL) 
            {
                if (arguments == NULL) {
                    goto exit;
                }
                sosCommand = *arguments++;
            }
            CommandFunc commandFunc = (CommandFunc)dlsym(m_sosHandle, sosCommand);
            if (commandFunc)
            {
                std::string str;
                if (arguments != NULL)
                {
                    for (const char* arg = *arguments; arg; arg = *(++arguments))
                    {
                        str.append(arg);
                        str.append(" ");
                    }
                }
                const char* sosArgs = str.c_str();
                HRESULT hr = commandFunc(client, sosArgs);
                if (hr != S_OK)
                {
                    client->Output(DEBUG_OUTPUT_ERROR, "%s %s failed\n", sosCommand, sosArgs);
                }
            }
            else
            {
                client->Output(DEBUG_OUTPUT_ERROR, "SOS command '%s' not found %s\n", sosCommand, dlerror());
            }
        }
exit:
        delete client;
        return result.Succeeded();
    }

    void
    LoadSos(DebugClient *client)
    {
        if (m_sosHandle == NULL)
        {
            if (g_coreclrDirectory == NULL)
            {
                const char *coreclrModule = MAKEDLLNAME_A("coreclr");
                const char *directory = client->GetModuleDirectory(coreclrModule);
                if (directory != NULL)
                {
                    std::string path(directory);
                    path.append("/");
                    g_coreclrDirectory = strdup(path.c_str());
                }
                else
                {
                    client->Output(DEBUG_OUTPUT_WARNING, "The %s module is not loaded yet in the target process\n", coreclrModule);
                }
            }

            if (g_coreclrDirectory != NULL)
            {
                m_sosHandle = LoadModule(client, MAKEDLLNAME_A("sos"));
            }
        }
    }

    void *
    LoadModule(DebugClient *client, const char *moduleName)
    {
        std::string modulePath(g_coreclrDirectory);
        modulePath.append(moduleName);

        void *moduleHandle = dlopen(modulePath.c_str(), RTLD_NOW);
        if (moduleHandle == NULL)
        {
            client->Output(DEBUG_OUTPUT_ERROR, "dlopen(%s) failed %s\n", modulePath.c_str(), dlerror());
        }

        return moduleHandle;
    }
};

bool
sosCommandInitialize(lldb::SBDebugger debugger)
{
    lldb::SBCommandInterpreter interpreter = debugger.GetCommandInterpreter();
    interpreter.AddCommand("sos", new sosCommand(NULL), "Various coreclr debugging commands. See 'soshelp' for more details. sos <command-name> <args>");
    interpreter.AddCommand("bpmd", new sosCommand("bpmd"), "Creates a breakpoint at the specified managed method in the specified module.");
    interpreter.AddCommand("clrstack", new sosCommand("ClrStack"), "Provides a stack trace of managed code only.");
    interpreter.AddCommand("clrthreads", new sosCommand("Threads"), "List the managed threads running.");
    interpreter.AddCommand("dumpheap", new sosCommand("DumpHeap"), "Displays info about the garbage-collected heap and collection statistics about objects.");
    interpreter.AddCommand("dumplog", new sosCommand("DumpLog"), "Writes the contents of an in-memory stress log to the specified file.");
    interpreter.AddCommand("dumpmd", new sosCommand("DumpMD"), "Displays information about a MethodDesc structure at the specified address.");
    interpreter.AddCommand("dumpmt", new sosCommand("DumpMT"), "Displays information about a method table at the specified address.");
    interpreter.AddCommand("dumpobj", new sosCommand("DumpObj"), "Displays info about an object at the specified address.");
    interpreter.AddCommand("dso", new sosCommand("DumpStackObjects"), "Displays all managed objects found within the bounds of the current stack.");
    interpreter.AddCommand("eeheap", new sosCommand("EEHeap"), "Displays info about process memory consumed by internal runtime data structures.");
    interpreter.AddCommand("gcroot", new sosCommand("GCRoot"), "Displays info about references (or roots) to an object at the specified address.");
    interpreter.AddCommand("ip2md", new sosCommand("IP2MD"), "Displays the MethodDesc structure at the specified address in code that has been JIT-compiled.");
    interpreter.AddCommand("name2ee", new sosCommand("Name2EE"), "Displays the MethodTable structure and EEClass structure for the specified type or method in the specified module.");
    interpreter.AddCommand("pe", new sosCommand("PrintException"), "Displays and formats fields of any object derived from the Exception class at the specified address.");
    interpreter.AddCommand("soshelp", new sosCommand("Help"), "Displays all available commands when no parameter is specified, or displays detailed help information about the specified command. soshelp <command>");
    return true;
}
