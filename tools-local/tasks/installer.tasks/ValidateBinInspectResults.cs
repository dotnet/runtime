// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    // The BinInspect tool we use for signing validation does not support
    // baselining, this task is used to inspect the results and determine
    // if there is an actual failure we care about.
    public class ValidateBinInspectResults : BuildTask
    {
        private static readonly string s_RootElement = "DATA";
        private static readonly string s_RowElement = "ROW";
        private static readonly string s_ErrorElement = "Err";
        private static readonly string s_NameElement = "Full";
        private static readonly string s_ResultElement = "Pass";
        
        [Required]
        public string ResultsXml { get; set; }

        // list of Regex's to ignore 
        public ITaskItem [] BaselineFiles { get; set; }

        [Output]
        public ITaskItem[] ErrorResults { get; set; }

        public override bool Execute()
        {
            XDocument xdoc = XDocument.Load(ResultsXml);

            var rows = xdoc.Descendants(s_RootElement).Descendants(s_RowElement);
            
            List<ITaskItem> reportedFailures = new List<ITaskItem>();

            // Gather all of the rows with error results
            IEnumerable<XElement> errorRows = from descendant in rows
                            where descendant.Descendants().FirstOrDefault(f => f.Name == s_ResultElement).Value == "False"
                            select descendant;

            // Filter out baselined files which are stored as regex patterns
            HashSet<XElement> baselineExcludeElements = new HashSet<XElement>();
            if(BaselineFiles != null)
            {
                foreach(var baselineFile in BaselineFiles)
                {
                    IEnumerable<XElement> baselineExcluded = errorRows.Where(f => Regex.IsMatch(f.Descendants(s_NameElement).First().Value, baselineFile.ItemSpec, RegexOptions.IgnoreCase));
                    foreach(var baselineExclude in baselineExcluded)
                    {
                        baselineExcludeElements.Add(baselineExclude);
                    }
                }
            }
            // Gather the results with baselined files filtered out
            IEnumerable<XElement> baselinedRows = errorRows.Except(baselineExcludeElements);

            foreach (var filteredRow in baselinedRows)
            {
                ITaskItem item = new TaskItem(filteredRow.Descendants(s_NameElement).First().Value);
                item.SetMetadata("Error", filteredRow.Descendants(s_ErrorElement).First().Value);
                reportedFailures.Add(item);
            }

            ErrorResults = reportedFailures.ToArray();
            foreach(var result in ErrorResults)
            {
                Log.LogError($"{result.ItemSpec} failed with error {result.GetMetadata("Error")}");
            }
            return !Log.HasLoggedErrors;
        }
    }
}
