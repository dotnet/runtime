//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"
#include <dlfcn.h>
#include <string.h>
#include <string>

class sosCommand : public lldb::SBCommandPluginInterface
{
    void *m_sosHandle;
    char m_coreclrDirectory[MAX_PATH];

public:
    sosCommand()
    {
        m_sosHandle = NULL;
    }

    virtual bool
    DoExecute (lldb::SBDebugger debugger,
               char** arguments,
               lldb::SBCommandReturnObject &result)
    {
        DebugClient* client = new DebugClient(debugger, result, m_coreclrDirectory);
        if (arguments)
        {
            LoadSos(client);

            if (m_sosHandle)
            {
                const char* sosCommand = *arguments++;
                CommandFunc commandFunc = (CommandFunc)dlsym(m_sosHandle, sosCommand);
                if (commandFunc)
                {
                    std::string str;
                    for (const char* arg = *arguments; arg; arg = *(++arguments))
                    {
                        str.append(arg);
                        str.append(" ");
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
        }

        delete client;
        return result.Succeeded();
    }

    void
    LoadSos(DebugClient *client)
    {
        if (m_sosHandle == NULL)
        {
            const char *coreclrModule = MAKEDLLNAME_A("coreclr");
            const char *directory = client->GetModuleDirectory(coreclrModule);
            if (directory == NULL)
            {
                client->Output(DEBUG_OUTPUT_WARNING, "The %s module is not loaded yet in the target process\n", coreclrModule);
            }
            else 
            {
                strcpy(m_coreclrDirectory, directory);
                strcat(m_coreclrDirectory, "/");

                // Load the DAC module first explicitly because SOS and DBI
                // have implicit references to the DAC's PAL.
                LoadModule(client, MAKEDLLNAME_A("mscordaccore"));

                m_sosHandle = LoadModule(client, MAKEDLLNAME_A("sos"));
            }
        }
    }

    void *
    LoadModule(DebugClient *client, const char *moduleName)
    {
        std::string modulePath(m_coreclrDirectory);
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
    lldb::SBCommand command = interpreter.AddCommand("sos", new sosCommand(), "Various coreclr debugging commands. sos <command-name> <args>");
    return true;
}
