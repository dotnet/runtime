// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System;

namespace Wasm.Bench;

public abstract class Formatter
{
    public abstract string NewLine { get; }
    public abstract string NonBreakingSpace { get; }
    public abstract string CodeStart { get; }
    public abstract string CodeEnd { get; }
}

public class PlainFormatter : Formatter
{
    override public string NewLine => "\n";
    override public string NonBreakingSpace => " ";
    override public string CodeStart => "";
    override public string CodeEnd => "";
}

public class HTMLFormatter : Formatter
{
    override public string NewLine => "<br/>";
    override public string NonBreakingSpace => "&nbsp;";
    override public string CodeStart => "<code>";
    override public string CodeEnd => "</code>";
}

public class JsonResultsData
{
    public List<BenchTask.Result> results;
    public Dictionary<string, double> minTimes;
    public DateTime timeStamp;
}

