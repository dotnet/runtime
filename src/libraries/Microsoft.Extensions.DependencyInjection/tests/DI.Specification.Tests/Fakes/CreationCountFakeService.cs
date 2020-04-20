// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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