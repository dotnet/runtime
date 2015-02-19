//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"
#include <dlfcn.h>
#include <string>

class sosCommand : public lldb::SBCommandPluginInterface
{
public:
    virtual bool
    DoExecute (lldb::SBDebugger debugger,
               char** arguments,
               lldb::SBCommandReturnObject &result)
    {
        if (arguments)
        {
            DebugClient* client = new DebugClient(debugger, result);
            const char* sosCommand = *arguments++;
            const char* sosLibrary = "/ssd/coreclr/binaries/Product/amd64/debug/" MAKEDLLNAME_A("sos");
            HRESULT hr = E_FAIL;

            void *dl_handle = dlopen(sosLibrary, RTLD_LAZY);
            if (dl_handle)
            {
                CommandFunc commandFunc = (CommandFunc)dlsym(dl_handle, sosCommand);
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

                dlclose(dl_handle);
            }
            else
            {
                client->Output(DEBUG_OUTPUT_ERROR, "dlopen(%s) failed %s\n", sosLibrary, dlerror());
            }

            delete client;
            return result.Succeeded();
        }
        return false;
    }
};

bool
sosCommandInitialize(lldb::SBDebugger debugger)
{
    lldb::SBCommandInterpreter interpreter = debugger.GetCommandInterpreter();
    interpreter.AddCommand("sos", new sosCommand(), "various managed debugging commands");
    return true;
}
