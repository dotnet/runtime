// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "sosplugin.h"

namespace lldb {
    bool PluginInitialize (lldb::SBDebugger debugger);
}

bool
lldb::PluginInitialize (lldb::SBDebugger debugger)
{
    sosCommandInitialize(debugger);
    setclrpathCommandInitialize(debugger);
    setsostidCommandInitialize(debugger);
    return true;
}
