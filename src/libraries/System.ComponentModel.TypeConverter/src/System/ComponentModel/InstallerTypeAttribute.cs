// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the installer to use for a type to install components.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InstallerTypeAttribute : Attribute
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private readonly string? _typeName;

        /// <summary>
        /// Initializes a new instance of the System.Windows.Forms.ComponentModel.InstallerTypeAttribute class.
        /// </summary>
        public InstallerTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type installerType)
        {
            ArgumentNullException.ThrowIfNull(installerType);

            _typeName = installerType.AssemblyQualifiedName;
        }

        public InstallerTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string? typeName)
        {
            _typeName = typeName;
        }

        /// <summary>
        /// Gets the type of installer associated with this attribute.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public virtual Type? InstallerType => Type.GetType(_typeName!);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return (obj is InstallerTypeAttribute other) && other._typeName == _typeName;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
