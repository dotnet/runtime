// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks
{
    /// <summary>
    /// This cannot be done with WriteLinesToFile task because we need to parse EmccCFlags before saving to file
    /// by escaping spaces with backslashes. MsBuild converts all backslashes to forward slashes automatically,
    /// so it cannot be done directly in the .targets (https://github.com/dotnet/msbuild/issues/3468).
    /// </summary>
    public class CreateResponseFile : Microsoft.Build.Utilities.Task
    {
        [NotNull]
        [Required]
        public ITaskItem[]? EmccCFlags            { get; set; }
        [Required]
        public string?      FilePath              { get; set; }
        public bool         Overwrite             { get; set; } = true;
        public bool         WriteOnlyWhenDifferent{ get; set; } = true;
        public override bool Execute()
        {
            try
            {
                return ExecuteActual();
            }
            catch (LogAsErrorException laee)
            {
                Log.LogError(laee.Message);
                return false;
            }
        }

        private bool ExecuteActual()
        {
            if (EmccCFlags.Length == 0)
            {
                Log.LogError($"No Emcc flags to write");
                return false;
            }

            if (string.IsNullOrEmpty(FilePath))
            {
                Log.LogError($"FilePath is empty");
                return false;
            }

            if (File.Exists(FilePath))
            {
                if (!Overwrite)
                    return true;
                var lines = File.ReadLines(FilePath);
                bool isDifferent = lines.Count() != EmccCFlags.Length;
                if (!isDifferent)
                {
                    foreach (var element in lines.Zip(EmccCFlags, (line, flag) => new { Line = line, Flag = flag }) )
                    {
                        if (element.Line != element.Flag.ItemSpec)
                        {
                            Log.LogMessage($"Has a different line, element.Line={element.Line}, element.Flag.ItemSpec={element.Flag.ItemSpec}");
                            isDifferent = true;
                            break;
                        }
                    }
                }
                if (WriteOnlyWhenDifferent && isDifferent)
                    return true;
                Write(FilePath, EmccCFlags);
            }
            else
            {
                Write(FilePath, EmccCFlags);
            }
            return !Log.HasLoggedErrors;
        }

        private void Write(string path, ITaskItem[] flags)
        {
            using (StreamWriter outputFile = new StreamWriter(path))
            {
                foreach (ITaskItem flag in flags)
                {
                    outputFile.WriteLine(flag.ItemSpec.Replace(" ", "\\ "));
                }
            }
        }
    }
}
