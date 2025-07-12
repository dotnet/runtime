// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.XmlSerializer.Generator
{
    /// <summary>
    /// MSBuild task to run the Microsoft.XmlSerializer.Generator directly,
    /// without spawning a new process.
    /// </summary>
    public class GenerateXmlSerializers : Task
    {
        /// <summary>
        /// Path to the target assembly (usually the output DLL).
        /// </summary>
        [Required]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// Optional path to a response file containing additional generator arguments.
        /// </summary>
        public string RspFilePath { get; set; }

        /// <summary>
        /// Force regeneration of serializers even if they exist.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Run in quiet mode with minimal output.
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// If true, errors during serializer generation will be suppressed.
        /// </summary>
        public bool IgnoreErrors { get; set; }

        /// <summary>
        /// Executes the XmlSerializer generator.
        /// </summary>
        public override bool Execute()
        {
            var args = new List<string>();

            if (!string.IsNullOrEmpty(AssemblyPath))
                args.Add(AssemblyPath);

            if (Force)
                args.Add("--force");

            if (Quiet)
                args.Add("--quiet");

            if (!string.IsNullOrEmpty(RspFilePath))
                args.Add(RspFilePath);

            Sgen sgen = new Sgen();
            Sgen.InfoWriter = new InfoTextWriter(Log);
            Sgen.WarningWriter = new WarningTextWriter(Log);
            Sgen.ErrorWriter = new ErrorTextWriter(Log);

            int exitCode = sgen.Run(args.ToArray());

            if (exitCode != 0)
            {
                Log.LogError("XmlSerializer failed with exit code {0}.", exitCode);
            }

            return IgnoreErrors || exitCode == 0;
        }
    }
}
