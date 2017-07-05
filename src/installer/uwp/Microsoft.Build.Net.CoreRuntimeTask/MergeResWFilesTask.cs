// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Net.CoreRuntimeTask
{
    public sealed class MergeResWFilesTask : Task
    {
        private List<ITaskItem> mergedResources = new List<ITaskItem>();

        /// <summary>
        /// The list of merged resource files
        /// </summary>
        [Output]
        public ITaskItem[] MergedResources { get; set; }

        /// <summary>
        /// OutputLocation is where the merged resw files are going to be stored.
        /// The merged resw files are going to be overwriting the existing files
        /// in OutputLocation
        /// </summary>
        [Required]
        public string OutputLocation { get; set; }
        /// <summary>
        /// The *.resw files to merge for
        /// </summary>
        [Required]
        public ITaskItem[] ResourceFiles { get; set; }
        /// <summary>
        /// Implementation of Microsoft.Build.Utilities.Task.Execute
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            bool succeeded = (0 == InternalExecute());
            MergedResources = mergedResources.ToArray();
            return succeeded;

        }

        /// <summary>
        /// The actual implementation of the Execute
        /// </summary>
        /// <returns>Returns 0 on success</returns>
        private int InternalExecute()
        {

            // Loop through the ResourceFiles in groups of ResourceIndexName
            foreach (var resourceIndex in GetResourceIndexes())
            {
                var resourceFiles = GetResourceFilesOfResourceIndex(resourceIndex);
                var firstResourceFile = resourceFiles.FirstOrDefault();
                if (firstResourceFile != null)
                {
                    XDocument doc = XDocument.Load(firstResourceFile.ItemSpec);
                    HashSet<string> mergedResourceKeys = new HashSet<string>();
                    foreach (var key in doc.Descendants("data").Select(x => x.Attribute("name").Value))
                        mergedResourceKeys.Add(key);
                    foreach (var resourceFile in resourceFiles.Skip(1))
                    {
                        XDocument mergee = XDocument.Load(resourceFile.ItemSpec);
                        
                        foreach (var dataNode in mergee.Descendants("data"))
                        {
                            if (mergedResourceKeys.Add(dataNode.Attribute("name").Value))
                                doc.Root.Add(dataNode);
                        }

                    }
                    Directory.CreateDirectory(Path.GetFullPath(OutputLocation));
                    var mergedFilePath = Path.Combine(Path.GetFullPath(OutputLocation), Path.GetFileName(firstResourceFile.ItemSpec));
                    doc.Save(mergedFilePath);
                    TaskItem newItem = new TaskItem(firstResourceFile);
                    newItem.ItemSpec = mergedFilePath;
                    mergedResources.Add(newItem);
                }
                    
            }
            return 0;
        }

        /// <summary>
        /// Returns the unique resource indexes from the list of resource files
        /// </summary>
        /// <returns>Unique list of resource</returns>
        private IEnumerable<string> GetResourceIndexes()
        {
            return ResourceFiles.Select(x => x.GetMetadata("ResourceIndexName")).Distinct();
        }
        /// <summary>
        /// Filters the resources according to the index
        /// </summary>
        /// <param name="resourceIndex">Item metadata value (of ResourceIndex) to filter the ResourceFiles</param>
        /// <returns>Filtered list of ResourceFiles</returns>
        private IEnumerable<ITaskItem> GetResourceFilesOfResourceIndex(string resourceIndex)
        {
            return ResourceFiles.Where(x => x.GetMetadata("ResourceIndexname").Equals(resourceIndex, StringComparison.OrdinalIgnoreCase));
        }

    }
}
