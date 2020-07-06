// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CompareAttribute : ValidationAttribute
    {
        public CompareAttribute(string otherProperty) : base(SR.CompareAttribute_MustMatch)
        {
            OtherProperty = otherProperty ?? throw new ArgumentNullException(nameof(otherProperty));
        }

        public string OtherProperty { get; }

        public string? OtherPropertyDisplayName { get; internal set; }

        public override bool RequiresValidationContext => true;

        public override string FormatErrorMessage(string name) =>
            string.Format(
                CultureInfo.CurrentCulture, ErrorMessageString, name, OtherPropertyDisplayName ?? OtherProperty);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var otherPropertyInfo = validationContext.ObjectType.GetRuntimeProperty(OtherProperty);
            if (otherPropertyInfo == null)
            {
                return new ValidationResult(SR.Format(SR.CompareAttribute_UnknownProperty, OtherProperty));
            }
            if (otherPropertyInfo.GetIndexParameters().Any())
            {
                throw new ArgumentException(SR.Format(SR.Common_PropertyNotFound, validationContext.ObjectType.FullName, OtherProperty));
            }

            object? otherPropertyValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance, null);
            if (!Equals(value, otherPropertyValue))
            {
                if (OtherPropertyDisplayName == null)
                {
                    OtherPropertyDisplayName = GetDisplayNameForProperty(otherPropertyInfo);
                }

                string[]? memberNames = validationContext.MemberName != null
                   ? new[] { validationContext.MemberName }
                   : null;
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
            }

            return null;
        }

        private string? GetDisplayNameForProperty(PropertyInfo property)
        {
            var attributes = CustomAttributeExtensions.GetCustomAttributes(property, true);
            var display = attributes.OfType<DisplayAttribute>().FirstOrDefault();
            if (display != null)
            {
                // TODO-NULLABLE: This will return null if [DisplayName] is specified but no Name has been defined - probably a bug.
                // Should fall back to OtherProperty in this case instead.
                return display.GetName();
            }

            return OtherProperty;
        }
    }
}
