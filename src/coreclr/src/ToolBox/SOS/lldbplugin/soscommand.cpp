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
        LLDBServices* services = new LLDBServices(debugger, result);
        LoadSos(services);

        if (m_sosHandle)
        {
            const char* sosCommand = m_command;
            if (sosCommand == NULL) 
            {
                if (arguments == NULL || *arguments == NULL) {
                    sosCommand = "Help";
                }
                else
                {
                    sosCommand = *arguments++;
                }
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
                HRESULT hr = commandFunc(services, sosArgs);
                if (hr != S_OK)
                {
                    services->Output(DEBUG_OUTPUT_ERROR, "%s %s failed\n", sosCommand, sosArgs);
                }
            }
            else
            {
                services->Output(DEBUG_OUTPUT_ERROR, "SOS command '%s' not found %s\n", sosCommand, dlerror());
            }
        }

        services->Release();
        return result.Succeeded();
    }

    void
    LoadSos(LLDBServices *services)
    {
        if (m_sosHandle == NULL)
        {
            if (g_coreclrDirectory == NULL)
            {
                const char *coreclrModule = MAKEDLLNAME_A("coreclr");
                const char *directory = services->GetModuleDirectory(coreclrModule);
                if (directory != NULL)
                {
                    std::string path(directory);
                    path.append("/");
                    g_coreclrDirectory = strdup(path.c_str());
                }
                else
                {
                    services->Output(DEBUG_OUTPUT_WARNING, "The %s module is not loaded yet in the target process\n", coreclrModule);
                }
            }

            if (g_coreclrDirectory != NULL)
            {
                // Load the DAC module first explicitly because SOS and DBI
                // have implicit references to the DAC's PAL.
                LoadModule(services, MAKEDLLNAME_A("mscordaccore"));

                m_sosHandle = LoadModule(services, MAKEDLLNAME_A("sos"));
            }
        }
    }

    void *
    LoadModule(LLDBServices *services, const char *moduleName)
    {
        std::string modulePath(g_coreclrDirectory);
        modulePath.append(moduleName);

        void *moduleHandle = dlopen(modulePath.c_str(), RTLD_NOW);
        if (moduleHandle == NULL)
        {
            services->Output(DEBUG_OUTPUT_ERROR, "dlopen(%s) failed %s\n", modulePath.c_str(), dlerror());
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
    interpreter.AddCommand("createdump", new sosCommand("CreateDump"), "Create a xplat minidump.");
    interpreter.AddCommand("clru", new sosCommand("u"), "Displays an annotated disassembly of a managed method.");
    interpreter.AddCommand("dumpasync", new sosCommand("DumpAsync"), "Displays info about async state machines on the garbage-collected heap.");
    interpreter.AddCommand("dumpclass", new sosCommand("DumpClass"), "Displays information about a EE class structure at the specified address.");
    interpreter.AddCommand("dumpdelegate", new sosCommand("DumpDelegate"), "Displays information about a delegate.");
    interpreter.AddCommand("dumpheap", new sosCommand("DumpHeap"), "Displays info about the garbage-collected heap and collection statistics about objects.");
    interpreter.AddCommand("dumpil", new sosCommand("DumpIL"), "Displays the Microsoft intermediate language (MSIL) that is associated with a managed method.");
    interpreter.AddCommand("dumplog", new sosCommand("DumpLog"), "Writes the contents of an in-memory stress log to the specified file.");
    interpreter.AddCommand("dumpmd", new sosCommand("DumpMD"), "Displays information about a MethodDesc structure at the specified address.");
    interpreter.AddCommand("dumpmodule", new sosCommand("DumpModule"), "Displays information about a EE module structure at the specified address.");
    interpreter.AddCommand("dumpmt", new sosCommand("DumpMT"), "Displays information about a method table at the specified address.");
    interpreter.AddCommand("dumpobj", new sosCommand("DumpObj"), "Displays info about an object at the specified address.");
    interpreter.AddCommand("dumpstack", new sosCommand("DumpStack"), "Displays a native and managed stack trace.");
    interpreter.AddCommand("dso", new sosCommand("DumpStackObjects"), "Displays all managed objects found within the bounds of the current stack.");
    interpreter.AddCommand("eeheap", new sosCommand("EEHeap"), "Displays info about process memory consumed by internal runtime data structures.");
    interpreter.AddCommand("eestack", new sosCommand("EEStack"), "Runs dumpstack on all threads in the process.");
    interpreter.AddCommand("gcroot", new sosCommand("GCRoot"), "Displays info about references (or roots) to an object at the specified address.");
    interpreter.AddCommand("ip2md", new sosCommand("IP2MD"), "Displays the MethodDesc structure at the specified address in code that has been JIT-compiled.");
    interpreter.AddCommand("name2ee", new sosCommand("Name2EE"), "Displays the MethodTable structure and EEClass structure for the specified type or method in the specified module.");
    interpreter.AddCommand("pe", new sosCommand("PrintException"), "Displays and formats fields of any object derived from the Exception class at the specified address.");
    interpreter.AddCommand("syncblk", new sosCommand("SyncBlk"), "Displays the SyncBlock holder info.");
    interpreter.AddCommand("histclear", new sosCommand("HistClear"), "Releases any resources used by the family of Hist commands.");
    interpreter.AddCommand("histinit", new sosCommand("HistInit"), "Initializes the SOS structures from the stress log saved in the debuggee.");
    interpreter.AddCommand("histobj", new sosCommand("HistObj"), "Examines all stress log relocation records and displays the chain of garbage collection relocations that may have led to the address passed in as an argument.");
    interpreter.AddCommand("histobjfind", new sosCommand("HistObjFind"), "Displays all the log entries that reference an object at the specified address.");
    interpreter.AddCommand("histroot", new sosCommand("HistRoot"), "Displays information related to both promotions and relocations of the specified root.");
    interpreter.AddCommand("soshelp", new sosCommand("Help"), "Displays all available commands when no parameter is specified, or displays detailed help information about the specified command. soshelp <command>");
    return true;
}
