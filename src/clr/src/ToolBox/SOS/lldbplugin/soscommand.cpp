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
        if (arguments)
        {
            DebugClient* client = new DebugClient(debugger, result);
            const char* sosCommand = *arguments++;
            HRESULT hr = E_FAIL;

            if (m_sosHandle == NULL)
            {
                // Assumes that LD_LIBRARY_PATH (or DYLD_LIBRARY_PATH on OSx) is set to runtime binaries path
                const char* sosLibrary = MAKEDLLNAME_A("sos");
                m_sosHandle = dlopen(sosLibrary, RTLD_LAZY);
                if (m_sosHandle == NULL)
                {
                    client->Output(DEBUG_OUTPUT_ERROR, "dlopen(%s) failed %s\n", sosLibrary, dlerror());
                }
            }

            if (m_sosHandle)
            {
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

                    hr = commandFunc(client, sosArgs);
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

            delete client;
        }

        return result.Succeeded();
    }
};

bool
sosCommandInitialize(lldb::SBDebugger debugger)
{
    lldb::SBCommandInterpreter interpreter = debugger.GetCommandInterpreter();
    interpreter.AddCommand("sos", new sosCommand(), "various managed debugging commands");
    return true;
}
