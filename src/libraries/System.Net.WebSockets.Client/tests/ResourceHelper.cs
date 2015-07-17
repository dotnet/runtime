// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

using Xunit;

namespace System.Net.WebSockets.Client.Test
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
