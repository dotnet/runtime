//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"

namespace lldb {
    bool PluginInitialize (lldb::SBDebugger debugger);
}

bool
lldb::PluginInitialize (lldb::SBDebugger debugger)
{
    return sosCommandInitialize(debugger);
}