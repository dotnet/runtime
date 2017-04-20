// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Construction;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Microsoft.DotNet.Build.Tasks
{
    public class SaveItems : BuildTask
    {
        [Required]
        public string ItemName { get; set; }

        [Required]
        public ITaskItem[] Items { get; set; }

        [Output]
        [Required]
        public string[] Files { get; set; }

        public override bool Execute()
        {
            var project = ProjectRootElement.Create();

            foreach (var item in Items)
            {
                var metadata = ((ITaskItem2)item).CloneCustomMetadataEscaped();

                var metadataPairs = metadata as IEnumerable<KeyValuePair<string, string>>;

                if (metadataPairs == null)
                {
                    metadataPairs = metadata.Keys.OfType<string>().Select(key => new KeyValuePair<string, string>(key, metadata[key] as string));
                }

                project.AddItem(ItemName, item.ItemSpec, metadataPairs);
            }

            foreach(var file in Files)
            {
                var path = Path.GetDirectoryName(file);

                if (!string.IsNullOrEmpty(path))
                {
                    Directory.CreateDirectory(path);
                }

                project.Save(file);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
