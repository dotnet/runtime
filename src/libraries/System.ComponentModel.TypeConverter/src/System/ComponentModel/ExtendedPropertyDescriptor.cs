// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.ComponentModel
{
    /// <summary>
    /// This class wraps an PropertyDescriptor with something that looks like a property. It
    /// allows you to treat extended properties the same as regular properties.
    /// </summary>
    internal sealed class ExtendedPropertyDescriptor : PropertyDescriptor
    {
        private readonly ReflectPropertyDescriptor _extenderInfo;       // the extender property
        private readonly IExtenderProvider _provider;           // the object providing it

        /// <summary>
        /// Creates a new extended property info. Callers can then treat this as
        /// a standard property.
        /// </summary>
        public ExtendedPropertyDescriptor(ReflectPropertyDescriptor extenderInfo, Type? receiverType, IExtenderProvider provider, Attribute[]? attributes)
            : base(extenderInfo, attributes)
        {
            Debug.Assert(extenderInfo != null, "ExtendedPropertyDescriptor must have extenderInfo");
            Debug.Assert(provider != null, "ExtendedPropertyDescriptor must have provider");

            List<Attribute> attrList = new List<Attribute>(AttributeArray!)
            {
                ExtenderProvidedPropertyAttribute.Create(extenderInfo, receiverType, provider)
            };
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

            ExtenderProvidedPropertyAttribute? attr = extender.Attributes[typeof(ExtenderProvidedPropertyAttribute)] as ExtenderProvidedPropertyAttribute;

            Debug.Assert(attr != null, "The original PropertyDescriptor does not have an ExtenderProvidedPropertyAttribute");


            ReflectPropertyDescriptor? reflectDesc = attr.ExtenderProperty as ReflectPropertyDescriptor;

            Debug.Assert(reflectDesc != null, "The original PropertyDescriptor has an invalid ExtenderProperty");

            _extenderInfo = reflectDesc;
            _provider = attr.Provider!;
        }

        /// <summary>
        /// Determines if the component will allow its value to be reset.
        /// </summary>
        public override bool CanResetValue(object comp)
        {
            return _extenderInfo.ExtenderCanResetValue(_provider, comp);
        }

        /// <summary>
        /// Retrieves the type of the component this PropertyDescriptor is bound to.
        /// </summary>
        public override Type ComponentType => _extenderInfo.ComponentType;

        /// <summary>
        /// Determines if the property can be written to.
        /// </summary>
        public override bool IsReadOnly => Attributes[typeof(ReadOnlyAttribute)]!.Equals(ReadOnlyAttribute.Yes);

        /// <summary>
        /// Retrieves the data type of the property.
        /// </summary>
        public override Type PropertyType => _extenderInfo.ExtenderGetType(_provider);

        /// <summary>
        /// Retrieves the display name of the property. This is the name that will
        /// be displayed in a properties window. This will be the same as the property
        /// name for most properties.
        /// </summary>
        public override string DisplayName
        {
            get
            {
                string name = base.DisplayName;

                if (!(Attributes[typeof(DisplayNameAttribute)] is DisplayNameAttribute displayNameAttr) || displayNameAttr.IsDefaultAttribute())
                {
                    ISite? site = GetSite(_provider);
                    string? providerName = site?.Name;
                    if (providerName != null && providerName.Length > 0)
                    {
                        name = SR.Format(SR.MetaExtenderName, name, providerName);
                    }
                }
                return name;
            }
        }

        /// <summary>
        /// Retrieves the value of the property for the given component. This will
        /// throw an exception if the component does not have this property.
        /// </summary>
        public override object? GetValue(object? comp) => _extenderInfo.ExtenderGetValue(_provider, comp);

        /// <summary>
        /// Resets the value of this property on comp to the default value.
        /// </summary>
        public override void ResetValue(object comp) => _extenderInfo.ExtenderResetValue(_provider, comp, this);

        /// <summary>
        /// Sets the value of this property on the given component.
        /// </summary>
        public override void SetValue(object? component, object? value)
        {
            _extenderInfo.ExtenderSetValue(_provider, component, value, this);
        }

        /// <summary>
        /// Determines if this property should be persisted. A property is
        /// to be persisted if it is marked as persistable through a
        /// PersistableAttribute, and if the property contains something other
        /// than the default value. Note, however, that this method will
        /// return true for design time properties as well, so callers
        /// should also check to see if a property is design time only before
        /// persisting to runtime storage.
        /// </summary>
        public override bool ShouldSerializeValue(object comp)
        {
            return _extenderInfo.ExtenderShouldSerializeValue(_provider, comp);
        }
    }
}
