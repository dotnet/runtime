// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Permissions;
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2113:SecureLateBindingMethods", Scope = "member", Target = "System.ComponentModel.InstallerTypeAttribute.get_InstallerType():System.Type")]

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the installer
    ///       to use for a type to install components.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InstallerTypeAttribute : Attribute
    {
        private string _typeName;

        /// <summary>
        /// <para>Initializes a new instance of the System.Windows.Forms.ComponentModel.InstallerTypeAttribute class.</para>
        /// </summary>
        public InstallerTypeAttribute(Type installerType)
        {
            _typeName = installerType.AssemblyQualifiedName;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public InstallerTypeAttribute(string typeName)
        {
            _typeName = typeName;
        }

        /// <summary>
        ///    <para> Gets the
        ///       type of installer associated with this attribute.</para>
        /// </summary>
        public virtual Type InstallerType => Type.GetType(_typeName);

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            InstallerTypeAttribute other = obj as InstallerTypeAttribute;

            return (other != null) && other._typeName == _typeName;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
