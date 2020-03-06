// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Attributes
{
    public class ParamsSummaryColumn : IColumn
    {
        public string Id => nameof(ParamsSummaryColumn);
        public string ColumnName { get; } = "Params";
        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public string GetValue(Summary summary, Benchmark benchmark) => benchmark.Parameters.DisplayInfo;
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Params;
        public int PriorityInCategory => 0;
        public override string ToString() => ColumnName;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);
        public string Legend => $"Summary of all parameter values";
    }
}