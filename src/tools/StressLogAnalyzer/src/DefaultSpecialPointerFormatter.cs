// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader;

namespace StressLogAnalyzer;

internal sealed class DefaultSpecialPointerFormatter : ISpecialPointerFormatter
{
    public string FormatMethodDesc(TargetPointer pointer) => $"(MethodDesc: 0x{pointer.Value:X})";
    public string FormatMethodTable(TargetPointer pointer) => $"(MethodTable: 0x{pointer.Value:X})";
    public string FormatStackTrace(TargetPointer pointer) => "(Unknown function)";
    public string FormatVTable(TargetPointer pointer) => "(Unknown VTable)";
}
