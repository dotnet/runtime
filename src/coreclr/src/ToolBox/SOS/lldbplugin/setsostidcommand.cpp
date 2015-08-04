//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"
#include <dlfcn.h>
#include <string.h>
#include <string>

class setsostidCommand : public lldb::SBCommandPluginInterface
{
public:
    setsostidCommand()
    {
    }

    virtual bool
        DoExecute(lldb::SBDebugger debugger,
        char** arguments,
        lldb::SBCommandReturnObject &result)
    {
        if (arguments[0] == NULL)
        {
            result.Printf("Clearing sos thread os id/index\n");
            g_currentThreadIndex = -1;
            g_currentThreadSystemId = -1;
        }
        else if (arguments[1] == NULL)
        {
            result.Printf("Need thread index parameter that maps to the os id\n");
        }
        else
        {
            ULONG tid = strtoul(arguments[0], NULL, 16);
            g_currentThreadSystemId = tid;

            ULONG index = strtoul(arguments[1], NULL, 16);
            g_currentThreadIndex = index;

            result.Printf("Set sos thread os id to 0x%x which maps to lldb thread index %d\n", tid, index);
        }
        return result.Succeeded();
    }
};

bool
setsostidCommandInitialize(lldb::SBDebugger debugger)
{
    lldb::SBCommandInterpreter interpreter = debugger.GetCommandInterpreter();
    lldb::SBCommand command = interpreter.AddCommand("setsostid", new setsostidCommand(), "Set the current os tid/thread index instead of using the one lldb provides. setsostid <tid> <index>");
    return true;
}
