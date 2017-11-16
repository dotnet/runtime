// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies whether an installer should be invoked during
    ///       installation of an assembly.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RunInstallerAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.RunInstallerAttribute'/> class.
        ///    </para>
        /// </summary>
        public RunInstallerAttribute(bool runInstaller)
        {
            RunInstaller = runInstaller;
        }


        /// <summary>
        ///    <para>
        ///       Gets a value indicating whether an installer should be
        ///       invoked during installation of an assembly.
        ///    </para>
        /// </summary>
        public bool RunInstaller { get; }


        /// <summary>
        ///    <para>
        ///       Specifies that a
        ///       component is visible in a visual designer. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly RunInstallerAttribute Yes = new RunInstallerAttribute(true);


        /// <summary>
        ///    <para>
        ///       Specifies that a
        ///       component
        ///       is not visible in a visual designer. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly RunInstallerAttribute No = new RunInstallerAttribute(false);


        /// <summary>
        ///    <para>
        ///       Specifies the default visibility, which is <see cref='System.ComponentModel.RunInstallerAttribute.No'/>. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly RunInstallerAttribute Default = No;


        /// <internalonly/>
        /// <summary>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            RunInstallerAttribute other = obj as RunInstallerAttribute;
            return other != null && other.RunInstaller == RunInstaller;
        }


        /// <summary>
        ///    <para>
        ///       Returns the hashcode for this object.
        ///    </para>
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        /// <internalonly/>
        /// <summary>
        /// </summary>
        public override bool IsDefaultAttribute()
        {
            return (Equals(Default));
        }
    }
}
