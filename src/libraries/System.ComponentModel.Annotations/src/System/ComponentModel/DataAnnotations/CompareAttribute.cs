// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CompareAttribute : ValidationAttribute
    {
        [RequiresUnreferencedCode("The property referenced by 'otherProperty' may be trimmed. Ensure it is preserved.")]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
            Justification = "The ctor is marked with RequiresUnreferencedCode informing the caller to preserve the other property.")]
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var otherPropertyInfo = validationContext.ObjectType.GetRuntimeProperty(OtherProperty);
            if (otherPropertyInfo == null)
            {
                return new ValidationResult(SR.Format(SR.CompareAttribute_UnknownProperty, OtherProperty));
            }
            if (otherPropertyInfo.GetIndexParameters().Length > 0)
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
            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes(property, true);
            foreach (Attribute attribute in attributes)
            {
                if (attribute is DisplayAttribute display)
                {
                   return display.GetName();
                }
            }

            return OtherProperty;
        }
    }
}
