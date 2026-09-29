// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.HotReload.Utils.Generator;

public class Config
{

    public static ConfigBuilder Builder () => new ();

    public class ConfigBuilder {
        internal ConfigBuilder () {}

        public bool Live {get; set;} = false;

        public List<KeyValuePair<string,string>> Properties {get; set;} = new List<KeyValuePair<string,string>> ();
        public string ProjectPath {get; set; } = "";

        public string ScriptPath {get; set; } = "";

        public string OutputSummaryPath {get; set; } = "";
        public Config Bake () {
            return new MsbuildConfig(this);
        }

        public List<string> EditAndContinueCapabilities {get; set; } = new List<string>();

        public bool NoWarnUnknownCapabilities {get; set;} = false;
    }

    protected Config (ConfigBuilder builder) {
        Live = builder.Live;
        Properties = builder.Properties;
        ProjectPath = builder.ProjectPath;
        ScriptPath = builder.ScriptPath;
        OutputSummaryPath = builder.OutputSummaryPath;
        EditAndContinueCapabilities = builder.EditAndContinueCapabilities.ToArray();
        NoWarnUnknownCapabilities = builder.NoWarnUnknownCapabilities;
    }

    public bool Live { get; }

    /// Additional properties for msbuild
    public IReadOnlyList<KeyValuePair<string,string>> Properties { get; }


    /// the csproj for this project
    public string ProjectPath { get; }

    /// the files to watch for live changes
    public string LiveCodingWatchPattern { get => "*.cs"; }

    /// the directory to watch for live changes
    public string LiveCodingWatchDir { get => Path.GetDirectoryName(ProjectPath) ?? "."; }

    /// the path of a JSON script to drive the delta generation
    public string ScriptPath { get; }

    /// A path for a JSON file to collect the produced artifacts
    public string OutputSummaryPath { get; }

    /// A set of strings specifying edit and continue capabilities to pass to Roslyn
    public string[] EditAndContinueCapabilities { get; }

    /// If 'true' don't print a warning if a capability is not known.
    public bool NoWarnUnknownCapabilities {get; }
}

internal class MsbuildConfig : Config {
    internal MsbuildConfig (ConfigBuilder builder) : base (builder) {}
}
