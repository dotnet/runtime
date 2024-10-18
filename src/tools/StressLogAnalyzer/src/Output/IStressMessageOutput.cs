// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Output;

internal interface IStressMessageOutput
{
    Task OutputLineAsync(string line);

    Task OutputMessageAsync(ThreadStressLogData thread, StressMsgData message);
}
