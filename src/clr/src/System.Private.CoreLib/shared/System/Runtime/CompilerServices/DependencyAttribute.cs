// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DependencyAttribute : Attribute
    {
        public DependencyAttribute(string dependentAssemblyArgument, LoadHint loadHintArgument)
        {
            DependentAssembly = dependentAssemblyArgument;
            LoadHint = loadHintArgument;
        }

        public string DependentAssembly { get; }
        public LoadHint LoadHint { get; }
    }
}
