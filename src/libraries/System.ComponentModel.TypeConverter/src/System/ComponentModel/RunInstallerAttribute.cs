// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies whether an installer should be invoked during
    /// installation of an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RunInstallerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of
        /// the <see cref='System.ComponentModel.RunInstallerAttribute'/> class.
        /// </summary>
        public RunInstallerAttribute(bool runInstaller)
        {
            RunInstaller = runInstaller;
        }

        /// <summary>
        /// Gets a value indicating whether an installer should be
        /// invoked during installation of an assembly.
        /// </summary>
        public bool RunInstaller { get; }

        /// <summary>
        /// Specifies that a
        /// component is visible in a visual designer. This <see langword='static '/>field is
        /// read-only.
        /// </summary>
        public static readonly RunInstallerAttribute Yes = new RunInstallerAttribute(true);

        /// <summary>
        /// Specifies that a
        /// component
        /// is not visible in a visual designer. This <see langword='static '/>field is
        /// read-only.
        /// </summary>
        public static readonly RunInstallerAttribute No = new RunInstallerAttribute(false);

        /// <summary>
        /// Specifies the default visibility, which is <see cref='System.ComponentModel.RunInstallerAttribute.No'/>. This <see langword='static '/>field is
        /// read-only.
        /// </summary>
        public static readonly RunInstallerAttribute Default = No;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is RunInstallerAttribute other && other.RunInstaller == RunInstaller;
        }

        /// <summary>
        /// Returns the hashcode for this object.
        /// </summary>
        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => (Equals(Default));
    }
}
