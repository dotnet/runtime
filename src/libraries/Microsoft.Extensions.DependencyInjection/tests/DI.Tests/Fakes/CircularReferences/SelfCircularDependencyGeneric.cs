// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DependencyInjection.Tests.Fakes
{
    public class SelfCircularDependencyGeneric<TDependency>
    {
        public SelfCircularDependencyGeneric(SelfCircularDependencyGeneric<string> dependency)
        {

        }

        public SelfCircularDependencyGeneric()
        {

        }
    }
}