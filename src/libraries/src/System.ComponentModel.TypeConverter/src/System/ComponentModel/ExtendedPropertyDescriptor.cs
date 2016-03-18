// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.ComponentModel
{
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Security.Permissions;

    /// <internalonly/>
    /// <devdoc>
    ///    <para>
    ///       This class wraps an PropertyDescriptor with something that looks like a property. It
    ///       allows you to treat extended properties the same as regular properties.
    ///    </para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    internal sealed class ExtendedPropertyDescriptor : PropertyDescriptor
    {
        private readonly ReflectPropertyDescriptor _extenderInfo;       // the extender property
        private readonly IExtenderProvider _provider;           // the guy providing it

        /// <devdoc>
        ///     Creates a new extended property info.  Callers can then treat this as
        ///     a standard property.
        /// </devdoc>
        public ExtendedPropertyDescriptor(ReflectPropertyDescriptor extenderInfo, Type receiverType, IExtenderProvider provider, Attribute[] attributes)
            : base(extenderInfo, attributes)
        {
            Debug.Assert(extenderInfo != null, "ExtendedPropertyDescriptor must have extenderInfo");
            Debug.Assert(provider != null, "ExtendedPropertyDescriptor must have provider");

            ArrayList attrList = new ArrayList(AttributeArray);
            attrList.Add(ExtenderProvidedPropertyAttribute.Create(extenderInfo, receiverType, provider));
            if (extenderInfo.IsReadOnly)
            {
                attrList.Add(ReadOnlyAttribute.Yes);
            }

            Attribute[] temp = new Attribute[attrList.Count];
            attrList.CopyTo(temp, 0);
            AttributeArray = temp;

            _extenderInfo = extenderInfo;
            _provider = provider;
        }

        public ExtendedPropertyDescriptor(PropertyDescriptor extender, Attribute[] attributes) : base(extender, attributes)
        {
            Debug.Assert(extender != null, "The original PropertyDescriptor must be non-null");

            ExtenderProvidedPropertyAttribute attr = extender.Attributes[typeof(ExtenderProvidedPropertyAttribute)] as ExtenderProvidedPropertyAttribute;

            Debug.Assert(attr != null, "The original PropertyDescriptor does not have an ExtenderProvidedPropertyAttribute");


            ReflectPropertyDescriptor reflectDesc = attr.ExtenderProperty as ReflectPropertyDescriptor;

            Debug.Assert(reflectDesc != null, "The original PropertyDescriptor has an invalid ExtenderProperty");

            _extenderInfo = reflectDesc;
            _provider = attr.Provider;
        }

        /// <devdoc>
        ///     Determines if the the component will allow its value to be reset.
        /// </devdoc>
        public override bool CanResetValue(object comp)
        {
            return _extenderInfo.ExtenderCanResetValue(_provider, comp);
        }

        /// <devdoc>
        ///     Retrieves the type of the component this PropertyDescriptor is bound to.
        /// </devdoc>
        public override Type ComponentType
        {
            get
            {
                return _extenderInfo.ComponentType;
            }
        }

        /// <devdoc>
        ///     Determines if the property can be written to.
        /// </devdoc>
        public override bool IsReadOnly
        {
            get
            {
                return Attributes[typeof(ReadOnlyAttribute)].Equals(ReadOnlyAttribute.Yes);
            }
        }

        /// <devdoc>
        ///     Retrieves the data type of the property.
        /// </devdoc>
        public override Type PropertyType
        {
            get
            {
                return _extenderInfo.ExtenderGetType(_provider);
            }
        }

        /// <devdoc>
        ///     Retrieves the display name of the property.  This is the name that will
        ///     be displayed in a properties window.  This will be the same as the property
        ///     name for most properties.
        /// </devdoc>
        public override string DisplayName
        {
            get
            {
                string name = base.DisplayName;

                DisplayNameAttribute displayNameAttr = Attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;
                if (displayNameAttr == null || displayNameAttr.IsDefaultAttribute())
                {
                    ISite site = GetSite(_provider);
                    if (site != null)
                    {
                        string providerName = site.Name;
                        if (providerName != null && providerName.Length > 0)
                        {
                            name = SR.GetString(SR.MetaExtenderName, name, providerName);
                        }
                    }
                }
                return name;
            }
        }

        /// <devdoc>
        ///     Retrieves the value of the property for the given component.  This will
        ///     throw an exception if the component does not have this property.
        /// </devdoc>
        public override object GetValue(object comp)
        {
            return _extenderInfo.ExtenderGetValue(_provider, comp);
        }

        /// <devdoc>
        ///     Resets the value of this property on comp to the default value.
        /// </devdoc>
        public override void ResetValue(object comp)
        {
            _extenderInfo.ExtenderResetValue(_provider, comp, this);
        }

        /// <devdoc>
        ///     Sets the value of this property on the given component.
        /// </devdoc>
        public override void SetValue(object component, object value)
        {
            _extenderInfo.ExtenderSetValue(_provider, component, value, this);
        }

        /// <devdoc>
        ///     Determines if this property should be persisted.  A property is
        ///     to be persisted if it is marked as persistable through a
        ///     PersistableAttribute, and if the property contains something other
        ///     than the default value.  Note, however, that this method will
        ///     return true for design time properties as well, so callers
        ///     should also check to see if a property is design time only before
        ///     persisting to runtime storage.
        /// </devdoc>
        public override bool ShouldSerializeValue(object comp)
        {
            return _extenderInfo.ExtenderShouldSerializeValue(_provider, comp);
        }

        /* 
           The following code has been removed to fix FXCOP violations.  The code
           is left here incase it needs to be resurrected in the future.

        /// <devdoc>
        ///     Creates a new extended property info.  Callers can then treat this as
        ///     a standard property.
        /// </devdoc>
        public ExtendedPropertyDescriptor(ReflectPropertyDescriptor extenderInfo, Type receiverType, IExtenderProvider provider) : this(extenderInfo, receiverType, provider, null) {
        }

        /// <devdoc>
        ///     Retrieves the object that is providing this extending property.
        /// </devdoc>
        public IExtenderProvider Provider {
            get {
                return provider;
            }
        }
        */
    }
}
