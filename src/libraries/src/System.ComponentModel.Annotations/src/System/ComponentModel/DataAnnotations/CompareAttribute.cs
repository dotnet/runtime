// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CompareAttribute : ValidationAttribute
    {
        public CompareAttribute(string otherProperty)
            : base(SR.CompareAttribute_MustMatch)
        {
            if (otherProperty == null)
            {
                throw new ArgumentNullException(nameof(otherProperty));
            }
            OtherProperty = otherProperty;
        }

        public string OtherProperty { get; private set; }

        public string OtherPropertyDisplayName { get; internal set; }

        public override bool RequiresValidationContext
        {
            get { return true; }
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name,
                OtherPropertyDisplayName ?? OtherProperty);
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // TODO - check that GetRuntimeProperty() returns the same as old ObjectType.GetProperty()
            // in all situations regardless of property modifiers
            var otherPropertyInfo = validationContext.ObjectType.GetRuntimeProperty(OtherProperty);
            if (otherPropertyInfo == null)
            {
                return
                    new ValidationResult(string.Format(CultureInfo.CurrentCulture,
                        SR.CompareAttribute_UnknownProperty, OtherProperty));
            }

            object otherPropertyValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance, null);
            if (!Equals(value, otherPropertyValue))
            {
                if (OtherPropertyDisplayName == null)
                {
                    OtherPropertyDisplayName = GetDisplayNameForProperty(validationContext.ObjectType, OtherProperty);
                }
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
            return null;
        }

        private static string GetDisplayNameForProperty(Type containerType, string propertyName)
        {
            var property = containerType.GetRuntimeProperties()
                .SingleOrDefault(
                    prop =>
                        IsPublic(prop) &&
                        string.Equals(propertyName, prop.Name, StringComparison.OrdinalIgnoreCase) &&
                        !prop.GetIndexParameters().Any());

            if (property == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    SR.Common_PropertyNotFound, containerType.FullName, propertyName));
            }

            var attributes = CustomAttributeExtensions.GetCustomAttributes(property, true);
            var display = attributes.OfType<DisplayAttribute>().FirstOrDefault();
            if (display != null)
            {
                return display.GetName();
            }

            return propertyName;
        }

        private static bool IsPublic(PropertyInfo p)
        {
            return (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic);
        }
    }
}
