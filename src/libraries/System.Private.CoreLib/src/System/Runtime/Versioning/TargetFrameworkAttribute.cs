// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Versioning
{
    /// <summary>
    /// Identifies the version of .NET that a particular assembly was compiled against.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class TargetFrameworkAttribute : Attribute
    {
        private readonly string _frameworkName;  // A target framework moniker
        private string? _frameworkDisplayName;

        // The frameworkName parameter is intended to be the string form of a FrameworkName instance.
        public TargetFrameworkAttribute(string frameworkName)
        {
            ArgumentNullException.ThrowIfNull(frameworkName);

            _frameworkName = frameworkName;
        }

        // The target framework moniker that this assembly was compiled against.
        // Use the FrameworkName class to interpret target framework monikers.
        public string FrameworkName => _frameworkName;

        public string? FrameworkDisplayName
        {
            get => _frameworkDisplayName;
            set => _frameworkDisplayName = value;
        }
    }
}
