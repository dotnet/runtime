// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;

using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public class ResourceHelper
    {
        public static string GetExceptionMessage(string resourceName, params object[] parameters)
        {
            Type srType = typeof(SR);
            PropertyInfo property = srType.GetRuntimeProperties().Single(p => p.Name == resourceName);

            string errorMessageTemplate = (string)property.GetMethod.Invoke(null, null);

            Assert.False(string.IsNullOrEmpty(errorMessageTemplate),
                         string.Format("Localized error message '{0}' does not exist.", resourceName));

            return string.Format(errorMessageTemplate, parameters);
        }
    }
}
