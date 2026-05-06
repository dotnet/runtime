// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal record class PreciseDebugInfo(
        ulong MethodID,
        InlineContext InlineTree,
        List<PreciseIPMapping> Mappings);

    internal record class InlineContext(
        uint Ordinal,
        ulong MethodID,
        string MethodName,
        List<InlineContext> Inlinees);

    internal record class PreciseIPMapping(
        uint NativeOffset,
        uint InlineContext,
        uint ILOffset);
}
