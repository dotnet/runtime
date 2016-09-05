// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.ComponentModel
{
    /// <summary>
    /// These extension methods are used to mimic behavior from the .NET Framework where System.Attribute
    /// had extra methods. For compatibility reasons, they are implemented with interfaces on the attributes
    /// that need them, and then exposed via extension methods off of System.Attribute. The only difference
    /// is that Attribute.TypeId is now accessed as Attribute.GetTypeId() as extension properties don't exist.
    /// </summary>
    internal static class ComponentModelExtensions
    {
        /// <summary>
        /// This work around attempts to mimic the known cases where IsDefaultAttribute is true for System.ComponentModel
        /// attributes. This does not account for derived attributes, which a few may be (most are sealed). The derived
        /// attributes will most likely not be default as they will be overriding the default properties anyway, so this
        /// heuristic will cover the majority of cases while allowing correct layering with System.ComponentModel.Primitives
        /// </summary>
        public static bool IsDefaultAttribute(this Attribute attribute)
        {
            Func<Attribute, bool> isDefaultAttribute;
            return s_defaultAttributes.TryGetValue(attribute.GetType(), out isDefaultAttribute) && isDefaultAttribute(attribute);
        }

        /// <summary>
        /// This defines a unique ID for this attribute type. It is used by filtering algorithms to identify two attributes that
        /// are the same type. For most attributes, this just returns the Type instance for the attribute. DesignerAttribute
        /// overrides this to include the name of the category
        ///
        /// This is a work around as System.Attribute does not contain the property TypeId in .NET Core. This attempts to mimic
        /// the known cases where TypeId is is not just the type for System.ComponentModel attributes. There are two cases of this,
        /// and both are sealed, so this lookup will cover all cases.
        /// </summary>
        public static object GetTypeId(this Attribute attribute)
        {
            Func<Attribute, object> typeId;
            return s_typeId.TryGetValue(attribute.GetType(), out typeId)
                ? typeId(attribute)
                : attribute.GetType();
        }

        /// <summary>
        /// System.Attribute in .NET Core does not have a Match method, so this provides an abstraction for it when it is used
        /// in the TypeDescriptor classes in case anything ends up needing to customize it similar to GetTypeId or IsDefaultAttribute
        /// </summary>
        public static bool Match(this Attribute attribute, object obj)
        {
            return attribute.Equals(obj);
        }

        private static readonly Dictionary<Type, Func<Attribute, object>> s_typeId = new Dictionary<Type, Func<Attribute, object>>
        {
            { typeof(DesignerCategoryAttribute), attr => attr.GetType().FullName + ((DesignerCategoryAttribute)attr).Category },
            { typeof(ProvidePropertyAttribute), attr => attr.GetType().FullName + ((ProvidePropertyAttribute)attr).PropertyName },
        };

        private static readonly Dictionary<Type, Func<Attribute, bool>> s_defaultAttributes = new Dictionary<Type, Func<Attribute, bool>>
        {
            { typeof(BrowsableAttribute), attr => attr.Equals(BrowsableAttribute.Default) },
            { typeof(CategoryAttribute), attr => ((CategoryAttribute)attr).Category.Equals(CategoryAttribute.Default.Category) },
            { typeof(DescriptionAttribute), attr => attr.Equals(DescriptionAttribute.Default) },
            { typeof(DesignOnlyAttribute), attr => ((DesignOnlyAttribute)attr).IsDesignOnly == DesignOnlyAttribute.Default.IsDesignOnly },
            { typeof(DisplayNameAttribute), attr => attr.Equals(DisplayNameAttribute.Default) },
            { typeof(ImmutableObjectAttribute), attr => attr.Equals(ImmutableObjectAttribute.Default) },
            { typeof(LocalizableAttribute), attr => ((LocalizableAttribute)attr).IsLocalizable == LocalizableAttribute.Default.IsLocalizable },
            { typeof(MergablePropertyAttribute), attr => attr.Equals(MergablePropertyAttribute.Default) },
            { typeof(NotifyParentPropertyAttribute), attr => attr.Equals(NotifyParentPropertyAttribute.Default) },
            { typeof(ParenthesizePropertyNameAttribute), attr => attr.Equals(ParenthesizePropertyNameAttribute.Default) },
            { typeof(ReadOnlyAttribute), attr => ((ReadOnlyAttribute)attr).IsReadOnly == ReadOnlyAttribute.Default.IsReadOnly },
            { typeof(RefreshPropertiesAttribute), attr => attr.Equals(RefreshPropertiesAttribute.Default) },
            { typeof(DesignerSerializationVisibilityAttribute), attr => attr.Equals(DesignerSerializationVisibilityAttribute.Default) },
            { typeof(ExtenderProvidedPropertyAttribute), attr => ((ExtenderProvidedPropertyAttribute)attr).ReceiverType == null },
            { typeof(DesignerCategoryAttribute), attr => attr.Equals(DesignerCategoryAttribute.Default) }
        };
    }
}