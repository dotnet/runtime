//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"
#include <dlfcn.h>
#include <string>

class sosCommand : public lldb::SBCommandPluginInterface
{
    void *m_sosHandle;

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
        DebugClient* client = new DebugClient(debugger, result);
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
                        client->Output(DEBUG_OUTPUT_ERROR, "%s %s failed", sosCommand, sosArgs);
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
                client->Output(DEBUG_OUTPUT_WARNING, "The %s module is not loaded yet in the target process.\n", coreclrModule);
            }
            else 
            {
                std::string sosLibrary;
                sosLibrary.append(directory);
                sosLibrary.append("/");
                sosLibrary.append(MAKEDLLNAME_A("sos"));

                m_sosHandle = dlopen(sosLibrary.c_str(), RTLD_NOW);
                if (m_sosHandle == NULL)
                {
                    client->Output(DEBUG_OUTPUT_ERROR, "dlopen(%s) failed %s.\n", sosLibrary.c_str(), dlerror());
                }
            }
        }
    }
};

bool
sosCommandInitialize(lldb::SBDebugger debugger)
{
    lldb::SBCommandInterpreter interpreter = debugger.GetCommandInterpreter();
    lldb::SBCommand command = interpreter.AddCommand("sos", new sosCommand(), "Various coreclr debugging commands. sos <command-name> <args>");
    return true;
}
