// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// --------------------------------------------------------------------------------------------------
// generatedumpflags.h
//
// Dump generation flags
//
// Must be the same as the WriteDumpFlags enum in the diagnostics repo

#pragma once

enum GenerateDumpFlags
{
    GenerateDumpFlagsNone = 0x00,
    GenerateDumpFlagsLoggingEnabled = 0x01,
    GenerateDumpFlagsVerboseLoggingEnabled = 0x02,
    GenerateDumpFlagsCrashReportEnabled = 0x04,
    GenerateDumpFlagsCrashReportOnlyEnabled = 0x08
};
