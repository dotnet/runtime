// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Configuration
{
    [DebuggerDisplay("SectionInput {SectionXmlInfo.ConfigKey}")]
    internal sealed class SectionInput
    {
        // result can be null, so we use this object to indicate whether it has been evaluated
        private static readonly object s_unevaluated = new object();

        // accumulated errors related to this input
        private readonly List<ConfigurationException> _errors;

        // Provider to use for encryption
        private ProtectedConfigurationProvider _protectionProvider;

        internal SectionInput(SectionXmlInfo sectionXmlInfo, List<ConfigurationException> errors)
        {
            SectionXmlInfo = sectionXmlInfo;
            _errors = errors;

            Result = s_unevaluated;
            ResultRuntimeObject = s_unevaluated;
        }

        internal SectionXmlInfo SectionXmlInfo { get; }

        internal bool HasResult => Result != s_unevaluated;

        internal bool HasResultRuntimeObject => ResultRuntimeObject != s_unevaluated;

        internal object Result { get; set; }

        internal object ResultRuntimeObject { get; set; }

        internal bool IsProtectionProviderDetermined { get; private set; }

        internal ProtectedConfigurationProvider ProtectionProvider
        {
            get { return _protectionProvider; }
            set
            {
                _protectionProvider = value;
                IsProtectionProviderDetermined = true;
            }
        }

        // Errors associated with a section input.
        internal ICollection<ConfigurationException> Errors => _errors;

        internal bool HasErrors => ErrorsHelper.GetHasErrors(_errors);

        internal void ClearResult()
        {
            Result = s_unevaluated;
            ResultRuntimeObject = s_unevaluated;
        }

        internal void ThrowOnErrors()
        {
            ErrorsHelper.ThrowOnErrors(_errors);
        }
    }
}
