// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

/// <summary>
/// <para>
/// Generates a new file at <see cref="OutputPath"/>.
/// </para>
/// <para>
/// The <see cref="TemplateFile"/> can define variables for substitution using <see cref="Properties"/>.
/// </para>
/// <example>
/// The input file might look like this:
/// <code>
/// 2 + 2 = ${Sum}
/// </code>
/// When the task is invoked like this, it will produce "2 + 2 = 4"
/// <code>
/// &lt;GenerateFileFromTemplate Properties="Sum=4;OtherValue=123;" ... &gt;
/// </code>
/// </example>
/// </summary>
public class GenerateFileFromTemplate : Task
{
    /// <summary>
    /// The template file.
    /// Variable syntax: ${VarName}
    /// If your template file needs to output this format, you can escape the dollar sign with a backtick, e.g. `${NotReplaced}
    /// </summary>
    [NotNull]
    [Required]
    public string? TemplateFile { get; set; }

    /// <summary>
    /// The destination for the generated file.
    /// </summary>
    [NotNull]
    [Required]
    public string? OutputPath { get; set; }

    /// <summary>
    /// Key=value pairs of values
    /// </summary>
    [NotNull]
    [Required]
    public ITaskItem[]? Properties { get; set; }

    public override bool Execute()
    {
        string outputPath = Path.GetFullPath(OutputPath);
        if (!File.Exists(TemplateFile))
        {
            Log.LogError($"File {TemplateFile} does not exist");
            return false;
        }

        var template = File.ReadAllText(TemplateFile);
        var values = new Dictionary<string, string>();
        foreach (ITaskItem item in Properties)
            values.Add(item.ItemSpec, item.GetMetadata("Value"));

        var result = Replace(template, values);
        string? dir = Path.GetDirectoryName(outputPath);
        if (dir != null)
            Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, result);

        return true;
    }

    public string Replace(string template, IDictionary<string, string> values)
    {
        var sb = new StringBuilder();
        var varNameSb = new StringBuilder();
        var line = 1;
        for (var i = 0; i < template.Length; i++)
        {
            var ch = template[i];
            var nextCh = i + 1 >= template.Length
                    ? '\0'
                    : template[i + 1];

            // count lines in the template file
            if (ch == '\n')
            {
                line++;
            }

            if (ch == '`' && (nextCh == '$' || nextCh == '`'))
            {
                // skip the backtick for known escape characters
                i++;
                sb.Append(nextCh);
                continue;
            }

            if (ch != '$' || nextCh != '{')
            {
                // variables begin with ${. Moving on.
                sb.Append(ch);
                continue;
            }

            varNameSb.Clear();
            i += 2;
            for (; i < template.Length; i++)
            {
                ch = template[i];
                if (ch != '}')
                {
                    varNameSb.Append(ch);
                }
                else
                {
                    // Found the end of the variable substitution
                    var varName = varNameSb.ToString();
                    if (values.TryGetValue(varName, out var value))
                    {
                        sb.Append(value);
                    }
                    else
                    {
                        Log.LogWarning(null, null, null, TemplateFile,
                            line, 0, 0, 0,
                            message: $"No property value is available for '{varName}'");
                    }

                    varNameSb.Clear();
                    break;
                }
            }

            if (varNameSb.Length > 0)
            {
                Log.LogWarning(null, null, null, TemplateFile,
                            line, 0, 0, 0,
                            message: "Expected closing bracket for variable placeholder. No substitution will be made.");
                sb.Append("${").Append(varNameSb);
            }
        }

        return sb.ToString();
    }
}