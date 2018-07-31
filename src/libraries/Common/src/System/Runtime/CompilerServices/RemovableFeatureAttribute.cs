// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.CompilerServices
{
    // Instructs IL linkers that the method body decorated with this attribute can be removed
    // during publishing.
    //
    // By default, the body gets replaced by throwing code.
    //
    // UseNopBody can be set to suppress the throwing behavior, replacing the throw with
    // a no-operation body.
    [AttributeUsage(AttributeTargets.Method)]
    internal class RemovableFeatureAttribute : Attribute
    {
        public bool UseNopBody;

        public string FeatureSwitchName;

        public RemovableFeatureAttribute(string featureSwitchName)
        {
            FeatureSwitchName = featureSwitchName;
        }
    }
}
