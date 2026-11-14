// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

public sealed class GenerateWasmTimezoneData : Task
{
    [Required]
    public string TimezoneDataDirectory { get; set; } = "";

    [Required]
    public string ProjectDirectory { get; set; } = "";

    [Required]
    public string OutputFile { get; set; } = "";

    [Output]
    public ITaskItem[] DataFiles { get; private set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            string directory = Path.GetFullPath(Path.Combine(ProjectDirectory, TimezoneDataDirectory));
            if (!Directory.Exists(directory))
                throw new LogAsErrorException($"WasmCustomTimezoneDataDirectory '{directory}' does not exist.");

            if (directory.IndexOfAny(new[] { '"', ';', '\r', '\n' }) >= 0 ||
                (Path.DirectorySeparatorChar != '\\' && directory.IndexOf('\\') >= 0))
                throw new LogAsErrorException($"WasmCustomTimezoneDataDirectory '{directory}' contains characters that cannot be embedded in the timezone configuration.");

            if (!File.Exists(Path.Combine(directory, "zone.tab")))
                throw new LogAsErrorException($"WasmCustomTimezoneDataDirectory '{directory}' must contain zone.tab and compiled TZif files.");

            directory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            string[] names = files.Select(file => file.Substring(directory.Length).Replace(Path.DirectorySeparatorChar, '/')).ToArray();
            foreach (string name in names)
            {
                if (!Regex.IsMatch(name, @"\A[A-Za-z0-9_+./-]+\z"))
                    throw new LogAsErrorException($"Timezone data file '{name}' must use only letters, digits, '_', '+', '.', '-' and directory separators.");
            }

            using TempFileName temporaryFile = new();
            using (var writer = new StreamWriter(temporaryFile.Path, false, new UTF8Encoding(false)))
            {
                writer.WriteLine($"#define TZ_FILES_DIR \"{directory.Replace('\\', '/')}\"");
                writer.WriteLine($"#define TZ_FILES {string.Join(", ", names.Select(name => $"\"{name}\""))}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputFile))!);
            Utils.CopyIfDifferent(temporaryFile.Path, OutputFile, useHash: false);
            DataFiles = files.Select(file => (ITaskItem)new TaskItem(file)).ToArray();

            return true;
        }
        catch (Exception ex) when (ex is LogAsErrorException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            Log.LogError($"Could not generate custom timezone data: {ex.Message}");
            return false;
        }
    }
}
