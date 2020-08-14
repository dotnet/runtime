// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility
{
    internal class FileSystemOperationRecorder
    {
        public IList<IDictionary<string, object>> Records = new List<IDictionary<string, object>>();

        public void Add(string action, object values)
        {
            var record = new Dictionary<string, object>
            {
                {"action", action }
            };

            foreach (var p in values.GetType().GetTypeInfo().DeclaredProperties)
            {
                record[p.Name] = p.GetValue(values);
            }

            Records.Add(record);
        }
    }
}
