// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace Microsoft.DotNet.Build.Tasks
{
    /// <summary>
    /// Reads contents of an input file, and searches for each replacement passed in.
    /// 
    /// When ReplacementItems is matched, it will replace the Include/ItemSpec with the corresponding
    /// ReplacementString metadata value. This can be useful if the ReplacementString is a value that
    /// cannot be represented by ITaskItem.ItemSpec (like string.Empty).
    /// 
    /// When a ReplacementPattern is matched it will replace it with the string of the corresponding (by index) 
    /// item in ReplacementStrings.
    /// 
    /// For example, if 2 ReplacementPatterns are passed in, 2 ReplacementStrings must also passed in and the first 
    /// pattern will be replaced with the first string, and the second pattern replaced with the second string.
    /// 
    /// ReplacementPattern could easily be a regex, but it isn't needed for current use cases, so leaving this 
    /// as just a string that will be replaced.
    /// </summary>
    public class ReplaceFileContents : Task
    {
        [Required]
        public string InputFile { get; set; }

        [Required]
        public string DestinationFile { get; set; }

        public ITaskItem[] ReplacementItems { get; set; }

        public ITaskItem[] ReplacementPatterns { get; set; }

        public ITaskItem[] ReplacementStrings { get; set; }

        private ITaskItem[] Empty = new ITaskItem[0];

        public override bool Execute()
        {
            if (ReplacementItems == null && ReplacementPatterns == null && ReplacementStrings == null)
            {
                throw new Exception($"ReplaceFileContents was called with no replacement values. Either pass ReplacementItems or ReplacementPatterns/ReplacementStrings properties.");
            }

            ReplacementItems = ReplacementItems ?? Empty;
            ReplacementPatterns = ReplacementPatterns ?? Empty;
            ReplacementStrings = ReplacementStrings ?? Empty;

            if (ReplacementPatterns.Length != ReplacementStrings.Length)
            {
                throw new Exception($"Expected {nameof(ReplacementPatterns)}  (length {ReplacementPatterns.Length}) and {nameof(ReplacementStrings)} (length {ReplacementStrings.Length}) to have the same length.");
            }

            if (!File.Exists(InputFile))
            {
                throw new FileNotFoundException($"Expected file {InputFile} was not found.");
            }

            string inputFileText = File.ReadAllText(InputFile);
            string outputFileText = ReplacePatterns(inputFileText);

            WriteOutputFile(outputFileText);

            return true;
        }

        public string ReplacePatterns(string inputFileText)
        {
            var outText = inputFileText;

            foreach (var replacementItem in ReplacementItems)
            {
                var replacementPattern = replacementItem.ItemSpec;
                var replacementString = replacementItem.GetMetadata("ReplacementString");

                outText = outText.Replace(replacementPattern, replacementString);
            }

            for (int i=0; i<ReplacementPatterns.Length; ++i)
            {
                var replacementPattern = ReplacementPatterns[i].ItemSpec;
                var replacementString = ReplacementStrings[i].ItemSpec;

                outText = outText.Replace(replacementPattern, replacementString);
            }

            return outText;
        }

        public void WriteOutputFile(string outputFileText)
        {
            var destinationDirectory = Path.GetDirectoryName(DestinationFile);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.WriteAllText(DestinationFile, outputFileText);
        }
    }
}
