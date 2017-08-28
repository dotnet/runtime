// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Xml.Linq;

namespace Microsoft.Build.Net.CoreRuntimeTask
{
    public sealed class MergePriConfigTask : Task
    {
        [Required]
        public string AppPriConfig { get; set; }

        [Required]
        public string CustomPriConfig { get; set; }

        [Required]
        public string MergedPriConfig { get; set; }

        public override bool Execute()
        {
            return (0 == InternalExecute());
        }

        private int InternalExecute()
        {
            XElement appConfig = XElement.Load(AppPriConfig);
            if (appConfig == null)
            {
                Log.LogErrorFromResources(Resources.Error_FailedToLoadResourceConfig, AppPriConfig);
                return -1;
            }

            XElement customConfig = XElement.Load(CustomPriConfig);
            if (customConfig == null)
            {
                Log.LogError(Resources.Error_FailedToLoadResourceConfig, CustomPriConfig);
                return -1;
            }


            IEnumerable<XElement> customIndexRoot = customConfig.Descendants("index").Where(x => x.Descendants("indexer-config").Any(y => y.Attribute("type").Value == "RESW")).Select(x => x);
            if (customIndexRoot.Count() > 0)
            {
                appConfig.Add(customIndexRoot);
                appConfig.Save(MergedPriConfig);
                return 0;
            }
            else
            {
                Log.LogError(Resources.Error_InvalidResourceConfig, CustomPriConfig);
                return -1;
            }
        }
    }
}
