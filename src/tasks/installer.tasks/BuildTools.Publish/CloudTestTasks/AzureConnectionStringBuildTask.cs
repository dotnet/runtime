// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Utilities;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Build.CloudTestTasks
{
    public abstract class AzureConnectionStringBuildTask : Task
    {
        /// <summary>
        /// Azure Storage account connection string.  Supersedes Account Key / Name.  
        /// Will cause errors if both are set.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The Azure account key used when creating the connection string.
        /// When we fully deprecate these, can just make them get; only.
        /// </summary>
        public string AccountKey { get; set; }

        /// <summary>
        /// The Azure account name used when creating the connection string.
        /// When we fully deprecate these, can just make them get; only.
        /// </summary>
        public string AccountName { get; set; }

        public void ParseConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                if (!(string.IsNullOrEmpty(AccountKey) && string.IsNullOrEmpty(AccountName)))
                {
                    Log.LogError("If the ConnectionString property is set, you must not provide AccountKey / AccountName.  These values will be deprecated in the future.");
                }
                else
                {
                    Regex storageConnectionStringRegex = new Regex("AccountName=(?<name>.+?);AccountKey=(?<key>.+?);");

                    MatchCollection matches = storageConnectionStringRegex.Matches(ConnectionString);
                    if (matches.Count > 0)
                    {
                        // When we deprecate this format, we'll want to demote these to private
                        AccountName = matches[0].Groups["name"].Value;
                        AccountKey = matches[0].Groups["key"].Value;
                    }
                    else
                    {
                        Log.LogError("Error parsing connection string.  Please review its value.");
                    }
                }
            }
            else if (string.IsNullOrEmpty(AccountKey) || string.IsNullOrEmpty(AccountName))
            {
                Log.LogError("Error, must provide either ConnectionString or AccountName with AccountKey");
            }
        }
    }
}
