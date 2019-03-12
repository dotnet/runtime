// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateJsonObjectString : BuildTask
    {
        private static readonly string __indent1 = new string(' ', 4);
        private static readonly string __indent2 = new string(' ', 8);

        /// <summary>
        /// Properties to include. If multiple properties have the same name, each property value is
        /// included in an array. Only specify one value metadata: the first value is used.
        /// 
        /// %(Identity): Name of the property.
        /// %(String): String value of the property. This task adds quotes around it in the JSON.
        /// %(Object): Object value of the property. Create this with a nested call to this task.
        /// </summary>
        [Required]
        public ITaskItem[] Properties { get; set; }

        /// <summary>
        /// If set, also write the output JSON string to this file.
        /// </summary>
        public string TargetFile { get; set; }

        [Output]
        public string Json { get; set; }

        public override bool Execute()
        {
            var result = new StringBuilder();
            result.AppendLine("{");

            bool firstProperty = true;

            foreach (var group in Properties.GroupBy(item => item.ItemSpec))
            {
                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    result.AppendLine(",");
                }

                result.Append(__indent1);
                result.Append("\"");
                result.Append(group.Key);
                result.Append("\": ");

                if (group.Count() == 1)
                {
                    ITaskItem item = group.First();
                    WriteProperty(result, item, __indent1);
                }
                else
                {
                    result.AppendLine("[");

                    bool firstArrayLine = true;

                    foreach (ITaskItem item in group)
                    {
                        if (firstArrayLine)
                        {
                            firstArrayLine = false;
                        }
                        else
                        {
                            result.AppendLine(",");
                        }

                        result.Append(__indent2);
                        WriteProperty(result, item, __indent2);
                    }

                    result.AppendLine();
                    result.Append(__indent1);
                    result.Append("]");
                }
            }

            result.AppendLine();
            result.AppendLine("}");

            Json = result.ToString();

            if (!string.IsNullOrEmpty(TargetFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TargetFile));
                File.WriteAllText(TargetFile, Json);
            }

            return !Log.HasLoggedErrors;
        }

        private void WriteProperty(StringBuilder result, ITaskItem item, string indent)
        {
            string stringValue = item.GetMetadata("String");
            string objectValue = item.GetMetadata("Object");

            if (!string.IsNullOrEmpty(stringValue))
            {
                result.Append("\"");
                result.Append(stringValue);
                result.Append("\"");
            }
            else if (!string.IsNullOrEmpty(objectValue))
            {
                bool firstObjectLine = true;

                foreach (var line in objectValue.Split(
                    new[] {Environment.NewLine},
                    StringSplitOptions.RemoveEmptyEntries))
                {
                    if (firstObjectLine)
                    {
                        firstObjectLine = false;
                    }
                    else
                    {
                        result.AppendLine();
                        result.Append(indent);
                    }

                    result.Append(line);
                }
            }
            else
            {
                Log.LogError($"Item '{item.ItemSpec}' has no String or Object value.");
                result.Append("null");
            }
        }
    }
}
