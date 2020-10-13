// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the <see cref='System.ComponentModel.LicenseProvider'/>
    /// to use with a class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class LicenseProviderAttribute : Attribute
    {
        /// <summary>
        /// Specifies the default value, which is no provider. This <see langword='static '/>field is read-only.
        /// </summary>
        public static readonly LicenseProviderAttribute Default = new LicenseProviderAttribute();

        private Type _licenseProviderType;
        private readonly string _licenseProviderName;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LicenseProviderAttribute'/> class without a license
        /// provider.
        /// </summary>
        public LicenseProviderAttribute() : this((string)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LicenseProviderAttribute'/> class with
        /// the specified type.
        /// </summary>
        public LicenseProviderAttribute(string typeName)
        {
            _licenseProviderName = typeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LicenseProviderAttribute'/> class with
        /// the specified type of license provider.
        /// </summary>
        public LicenseProviderAttribute(Type type)
        {
            _licenseProviderType = type;
        }

        /// <summary>
        /// Gets the license provider to use with the associated class.
        /// </summary>
        public Type LicenseProvider
        {
            get
            {
                if (_licenseProviderType is null && _licenseProviderName is not null)
                {
                    _licenseProviderType = Type.GetType(_licenseProviderName);
                }
                return _licenseProviderType;
            }
        }

        /// <summary>
        /// This defines a unique ID for this attribute type. It is used
        /// by filtering algorithms to identify two attributes that are
        /// the same type. For most attributes, this just returns the
        /// Type instance for the attribute. LicenseProviderAttribute overrides this to include the type name and the
        /// provider type name.
        /// </summary>
        public override object TypeId
        {
            get
            {
                string typeName = _licenseProviderName;

                if (typeName is null && _licenseProviderType is not null)
                {
                    typeName = _licenseProviderType.FullName;
                }
                return GetType().FullName + typeName;
            }
        }

        public override bool Equals(object value)
        {
            if (value is LicenseProviderAttribute && value is not null)
            {
                Type type = ((LicenseProviderAttribute)value).LicenseProvider;
                if (type == LicenseProvider)
                {
                    return true;
                }
                else
                {
                    if (type is not null && type.Equals(LicenseProvider))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the hashcode for this object.
        /// </summary>
        public override int GetHashCode() => base.GetHashCode();
    }
}
