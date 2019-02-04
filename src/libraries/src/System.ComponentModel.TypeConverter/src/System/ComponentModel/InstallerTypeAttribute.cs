// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Security", "CA2113:SecureLateBindingMethods", Scope = "member", Target = "System.ComponentModel.InstallerTypeAttribute.get_InstallerType():System.Type")]

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the installer to use for a type to install components.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InstallerTypeAttribute : Attribute
    {
        private string _typeName;

        /// <summary>
        /// Initializes a new instance of the System.Windows.Forms.ComponentModel.InstallerTypeAttribute class.
        /// </summary>
        public InstallerTypeAttribute(Type installerType)
        {
            if (installerType == null)
            {
                throw new ArgumentNullException(nameof(installerType));
            }

            _typeName = installerType.AssemblyQualifiedName;
        }

        public InstallerTypeAttribute(string typeName)
        {
            _typeName = typeName;
        }

        /// <summary>
        /// Gets the type of installer associated with this attribute.
        /// </summary>
        public virtual Type InstallerType => Type.GetType(_typeName);

        public override bool Equals(object obj)
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
