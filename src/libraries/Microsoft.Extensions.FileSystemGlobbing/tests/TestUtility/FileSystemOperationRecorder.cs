// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
