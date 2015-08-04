//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"
#include <dlfcn.h>
#include <string.h>
#include <string>

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
            result.Printf("setclrpath error - no path\n");
            return false;
        }

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
        result.Printf("Set load path for sos/dac/dbi to %s\n", g_coreclrDirectory);
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
