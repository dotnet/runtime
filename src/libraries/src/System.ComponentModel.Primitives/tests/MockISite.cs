// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Tests
{
    public class MockSite : ISite
    {
        public IComponent Component { get; set; }
        public IContainer Container { get; set; }

        public bool DesignMode { get; set; }

        public string Name { get; set; }

        public Type ServiceType { get; set; }
        public object GetService(Type serviceType) => ServiceType;
    }
}
