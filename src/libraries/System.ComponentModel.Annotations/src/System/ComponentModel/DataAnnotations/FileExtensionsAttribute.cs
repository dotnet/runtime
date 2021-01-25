// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private string? _extensions;

        public FileExtensionsAttribute()
            : base(DataType.Upload)
        {
            // Set DefaultErrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.FileExtensionsAttribute_Invalid;
        }

        public string Extensions
        {
            // Default file extensions match those from jquery validate.
            get => string.IsNullOrWhiteSpace(_extensions) ? "png,jpg,jpeg,gif" : _extensions;
            set => _extensions = value;
        }

        private string ExtensionsFormatted => ExtensionsParsed.Aggregate((left, right) => left + ", " + right);

        private string ExtensionsNormalized =>
            Extensions.Replace(" ", string.Empty).Replace(".", string.Empty).ToLowerInvariant();

        private IEnumerable<string> ExtensionsParsed
        {
            get { return ExtensionsNormalized.Split(',').Select(e => "." + e); }
        }

        public override string FormatErrorMessage(string name) =>
            string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, ExtensionsFormatted);

        public override bool IsValid(object? value) =>
            value == null || value is string valueAsString && ValidateExtension(valueAsString);

        private bool ValidateExtension(string fileName)
        {
            return ExtensionsParsed.Contains(Path.GetExtension(fileName).ToLowerInvariant());
        }
    }
}
