// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class DefaultDependencyAttribute : Attribute
    {
        public DefaultDependencyAttribute(LoadHint loadHintArgument)
        {
            LoadHint = loadHintArgument;
        }

        public LoadHint LoadHint { get; }
    }
}
