// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILTrim
{
    public record class TrimmerSettings(
        int? MaxDegreeOfParallelism = null,
        LogStrategy LogStrategy = LogStrategy.None,
        string LogFile = null,
        bool LibraryMode = false
        )
    {
        public int EffectiveDegreeOfParallelism => MaxDegreeOfParallelism ?? Environment.ProcessorCount;
    }

    public enum LogStrategy
    {
        None,
        FirstMark,
        FullGraph,
        EventSource
    }
}
