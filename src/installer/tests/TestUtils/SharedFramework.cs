// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;

namespace Microsoft.DotNet.CoreSetup.Test
{
    /// <summary>
    /// Helper class for creating, modifying and cleaning up shared frameworks
    /// </summary>
    public static class SharedFramework
    {
        private static readonly Mutex id_mutex = new Mutex();

        // Locate the first non-existent directory of the form <basePath>-<count>
        public static string CalculateUniqueTestDirectory(string basePath)
        {
            id_mutex.WaitOne();

            int count = 0;
            string dir;

            do
            {
                dir = $"{basePath}-{count}";
                count++;
            } while (Directory.Exists(dir));

            id_mutex.ReleaseMutex();

            return dir;
        }
    }
}
