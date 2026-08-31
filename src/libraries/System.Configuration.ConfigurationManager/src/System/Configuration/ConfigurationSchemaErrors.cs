// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Configuration
{
    internal sealed class ConfigurationSchemaErrors
    {
        // All errors related to a config file are logged to this list.
        // This includes all global errors, all non-specific errors,
        // and local errors for input that applies to this config file.
        private List<ConfigurationException> _errorsAll;

        // Errors with ExceptionAction.Global are logged to this list.
        private List<ConfigurationException> _errorsGlobal;

        // Errors with ExceptionAction.Local are logged to this list.
        // This list is reset when processing of a section is complete.
        // Errors on this list may be added to the _errorsAll list
        // when RetrieveAndResetLocalErrors is called.
        private List<ConfigurationException> _errorsLocal;

        internal bool HasLocalErrors => ErrorsHelper.GetHasErrors(_errorsLocal);

        internal bool HasGlobalErrors => ErrorsHelper.GetHasErrors(_errorsGlobal);

        private bool HasAllErrors => ErrorsHelper.GetHasErrors(_errorsAll);

        internal int GlobalErrorCount => ErrorsHelper.GetErrorCount(_errorsGlobal);

        internal void AddError(ConfigurationException ce, ExceptionAction action)
        {
            switch (action)
            {
                case ExceptionAction.Global:
                    ErrorsHelper.AddError(ref _errorsAll, ce);
                    ErrorsHelper.AddError(ref _errorsGlobal, ce);
                    break;
                case ExceptionAction.NonSpecific:
                    ErrorsHelper.AddError(ref _errorsAll, ce);
                    break;
                case ExceptionAction.Local:
                    ErrorsHelper.AddError(ref _errorsLocal, ce);
                    break;
            }
        }

        internal void SetSingleGlobalError(ConfigurationException ce)
        {
            _errorsAll = null;
            _errorsLocal = null;
            _errorsGlobal = null;

            AddError(ce, ExceptionAction.Global);
        }

        internal bool HasErrors(bool ignoreLocal)
        {
            return ignoreLocal ? HasGlobalErrors : HasAllErrors;
        }

        internal void ThrowIfErrors(bool ignoreLocal)
        {
            if (!HasErrors(ignoreLocal)) return;

            if (HasGlobalErrors)
            {
                // Throw just the global errors, as they invalidate
                // all other config file parsing.
                throw new ConfigurationErrorsException(_errorsGlobal);
            }

            // Throw all errors no matter what
            throw new ConfigurationErrorsException(_errorsAll);
        }

        internal List<ConfigurationException> RetrieveAndResetLocalErrors(bool keepLocalErrors)
        {
            List<ConfigurationException> list = _errorsLocal;
            _errorsLocal = null;

            if (keepLocalErrors) ErrorsHelper.AddErrors(ref _errorsAll, list);

            return list;
        }

        internal void AddSavedLocalErrors(ICollection<ConfigurationException> coll)
        {
            ErrorsHelper.AddErrors(ref _errorsAll, coll);
        }

        internal void ResetLocalErrors()
        {
            RetrieveAndResetLocalErrors(false);
        }
    }
}
