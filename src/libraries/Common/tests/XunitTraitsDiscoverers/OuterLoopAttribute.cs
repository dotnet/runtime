// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary>
    /// Apply this attribute to your test method to specify a outer-loop category.
    /// </summary>
    [TraitDiscoverer("Xunit.TraitDiscoverers.OuterLoopTestsDiscoverer", "XunitTraitsDiscoverers")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OuterLoopAttribute : Attribute, ITraitAttribute
    {
        public OuterLoopAttribute() { }
    }
}
