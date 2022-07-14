// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Identifies which SKU and version of the .NET
**   Framework that a particular library was compiled against.
**   Emitted by VS, and can help catch deployment problems.
**
===========================================================*/

namespace System.Runtime.Versioning
{
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
