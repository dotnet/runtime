//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"
#include <dlfcn.h>
#include <string>

extern int corerun(const int argc, const char* argv[]);

class corerunCommand : public lldb::SBCommandPluginInterface
{
public:
    corerunCommand()
    {
    }

    virtual bool
    DoExecute (lldb::SBDebugger debugger,
               char** arguments,
               lldb::SBCommandReturnObject &result)
    {
        if (arguments)
        {
            int argc = 0;
            char **argv = arguments;
            for (const char* arg = *arguments; arg; arg = *(++arguments))
            {
                ++argc;
            }
            int exitcode = corerun((const int)argc, (const char**)argv);
            if (exitcode != 0)
            {
                result.SetError("corerun failed");
            }
        }
        return result.Succeeded();
    }
};

bool
corerunCommandInitialize(lldb::SBDebugger debugger)
{
    lldb::SBCommandInterpreter interpreter = debugger.GetCommandInterpreter();
    interpreter.AddCommand("corerun", new corerunCommand(), "run a managed app inside the debugger");
    return true;
}
