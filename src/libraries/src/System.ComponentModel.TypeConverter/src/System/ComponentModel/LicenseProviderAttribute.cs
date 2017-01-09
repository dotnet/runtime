// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Specifies the <see cref='System.ComponentModel.LicenseProvider'/>
    /// to use with a class.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class LicenseProviderAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Specifies the default value, which is no provider. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly LicenseProviderAttribute Default = new LicenseProviderAttribute();

        private Type _licenseProviderType;
        private string _licenseProviderName;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.LicenseProviderAttribute'/> class without a license
        ///    provider.</para>
        /// </summary>
        public LicenseProviderAttribute() : this((string)null)
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.LicenseProviderAttribute'/> class with
        ///       the specified type.
        ///    </para>
        /// </summary>
        public LicenseProviderAttribute(string typeName)
        {
            _licenseProviderName = typeName;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.LicenseProviderAttribute'/> class with
        ///       the specified type of license provider.
        ///    </para>
        /// </summary>
        public LicenseProviderAttribute(Type type)
        {
            _licenseProviderType = type;
        }

        /// <summary>
        ///    <para>Gets the license provider to use with the associated class.</para>
        /// </summary>
        public Type LicenseProvider
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2113:SecureLateBindingMethods")]
            get
            {
                if (_licenseProviderType == null && _licenseProviderName != null)
                {
                    _licenseProviderType = Type.GetType(_licenseProviderName);
                }
                return _licenseProviderType;
            }
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>
        ///       This defines a unique ID for this attribute type. It is used
        ///       by filtering algorithms to identify two attributes that are
        ///       the same type. For most attributes, this just returns the
        ///       Type instance for the attribute. LicenseProviderAttribute overrides this to include the type name and the
        ///       provider type name.
        ///    </para>
        /// </summary>
        public override object TypeId
        {
            get
            {
                string typeName = _licenseProviderName;

                if (typeName == null && _licenseProviderType != null)
                {
                    typeName = _licenseProviderType.FullName;
                }
                return GetType().FullName + typeName;
            }
        }

        /// <internalonly/>
        /// <summary>
        /// </summary>
        public override bool Equals(object value)
        {
            if (value is LicenseProviderAttribute && value != null)
            {
                Type type = ((LicenseProviderAttribute)value).LicenseProvider;
                if (type == LicenseProvider)
                {
                    return true;
                }
                else
                {
                    if (type != null && type.Equals(LicenseProvider))
                    {
                        return true;
                    }
                }
            }

            return false;
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
    }
}
