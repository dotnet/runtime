// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(const char*, mono_opcode_name, (int opcode))

MONO_API_FUNCTION(MonoOpcodeEnum, mono_opcode_value, (const mono_byte **ip, const mono_byte *end))
