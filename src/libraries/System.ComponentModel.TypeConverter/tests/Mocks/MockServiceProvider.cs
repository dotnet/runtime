// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.ComponentModel.Tests
{
    public class MockServiceProvider : IServiceProvider
    {
        private Dictionary<Type, object> Services { get; } = new Dictionary<Type, object>();

        public void Setup(Type serviceType, object service) => Services.Add(serviceType, service);

        public object GetService(Type serviceType)
        {
            if (!Services.TryGetValue(serviceType, out object value))
            {
                throw new NotImplementedException("Unrecognized service: " + serviceType.ToString());
            }

            return value;
        }
    }
}
