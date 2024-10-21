// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader;

namespace StressLogAnalyzer;

public interface ISpecialPointerFormatter
{
    string FormatMethodTable(TargetPointer pointer);
    string FormatMethodDesc(TargetPointer pointer);
    string FormatVTable(TargetPointer pointer);
    string FormatStackTrace(TargetPointer pointer);
}
