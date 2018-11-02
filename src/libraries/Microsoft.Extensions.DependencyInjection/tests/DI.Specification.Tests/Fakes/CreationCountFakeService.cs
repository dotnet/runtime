// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class CreationCountFakeService
    {
        public static readonly object InstanceLock = new object();

        public CreationCountFakeService(IFakeService dependency)
        {
            InstanceCount++;
            InstanceId = InstanceCount;
        }

        public static int InstanceCount { get; set; }

        public int InstanceId { get; }
    }
}