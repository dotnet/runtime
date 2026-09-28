// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;

namespace CdacUsageGraph.Reporting;

/// <summary>Phase E: writes one report artifact from the <see cref="UsageGraph"/>.</summary>
internal interface IReportWriter
{
    /// <summary>Writes the report to <paramref name="outputDirectory"/> and returns a one-line summary.</summary>
    string Write(UsageGraph graph, string outputDirectory);
}
