// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "sosplugin.h"
#include <dlfcn.h>
#include <string.h>
#include <string>
#include <stdlib.h>
#include <limits.h>

class setclrpathCommand : public lldb::SBCommandPluginInterface
{
public:
    setclrpathCommand()
    {
    }

    virtual bool
    DoExecute (lldb::SBDebugger debugger,
               char** arguments,
               lldb::SBCommandReturnObject &result)
    {
        if (arguments[0] == NULL)
        {
            result.Printf("Load path for sos/dac/dbi: '%s'\n", g_coreclrDirectory == NULL ? "<none>" : g_coreclrDirectory);
        }
        else {
            if (g_coreclrDirectory != NULL)
            {
                free(g_coreclrDirectory);
            }

            std::string path(arguments[0]);
            if (path[path.length() - 1] != '/')
            {
                path.append("/");
            }

            g_coreclrDirectory = strdup(path.c_str());
            result.Printf("Set load path for sos/dac/dbi to '%s'\n", g_coreclrDirectory);
        }
        return result.Succeeded();
    }
};

bool
setclrpathCommandInitialize(lldb::SBDebugger debugger)
{
    lldb::SBCommandInterpreter interpreter = debugger.GetCommandInterpreter();
    lldb::SBCommand command = interpreter.AddCommand("setclrpath", new setclrpathCommand(), "Set the path to load coreclr sos/dac/dbi files. setclrpath <path>");
    return true;
}
