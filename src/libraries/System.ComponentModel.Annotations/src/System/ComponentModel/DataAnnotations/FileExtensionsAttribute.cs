// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class FileExtensionsAttribute : DataTypeAttribute
    {
        private string _extensions;

        public FileExtensionsAttribute()
            : base(DataType.Upload)
        {
            // Set DefaultErrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.FileExtensionsAttribute_Invalid;
        }

        public string Extensions
        {
            get
            {
                // Default file extensions match those from jquery validate.
                return string.IsNullOrWhiteSpace(_extensions) ? "png,jpg,jpeg,gif" : _extensions;
            }
            set { _extensions = value; }
        }

        private string ExtensionsFormatted
        {
            get { return ExtensionsParsed.Aggregate((left, right) => left + ", " + right); }
        }


        private string ExtensionsNormalized
        {
            get { return Extensions.Replace(" ", string.Empty).Replace(".", string.Empty).ToLowerInvariant(); }
        }

        private IEnumerable<string> ExtensionsParsed
        {
            get { return ExtensionsNormalized.Split(',').Select(e => "." + e); }
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, ExtensionsFormatted);
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            var valueAsString = value as string;
            if (valueAsString != null)
            {
                return ValidateExtension(valueAsString);
            }

            return false;
        }

        private bool ValidateExtension(string fileName)
        {
            try
            {
                return ExtensionsParsed.Contains(Path.GetExtension(fileName).ToLowerInvariant());
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
