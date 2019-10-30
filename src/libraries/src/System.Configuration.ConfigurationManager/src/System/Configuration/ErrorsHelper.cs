// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Configuration
{
    internal static class ErrorsHelper
    {
        internal static int GetErrorCount(List<ConfigurationException> errors)
        {
            return errors?.Count ?? 0;
        }

        internal static bool GetHasErrors(List<ConfigurationException> errors)
        {
            return GetErrorCount(errors) > 0;
        }

        internal static void AddError(ref List<ConfigurationException> errors, ConfigurationException e)
        {
            Debug.Assert(e != null, "e != null");

            // Create on demand
            if (errors == null) errors = new List<ConfigurationException>();

            ConfigurationErrorsException ce = e as ConfigurationErrorsException;
            if (ce == null) errors.Add(e);
            else
            {
                ICollection<ConfigurationException> col = ce.ErrorsGeneric;
                if (col.Count == 1) errors.Add(e);
                else errors.AddRange(col);
            }
        }

        internal static void AddErrors(ref List<ConfigurationException> errors, ICollection<ConfigurationException> coll)
        {
            if ((coll == null) || (coll.Count == 0))
            {
                // Nothing to do here, bail
                return;
            }

            foreach (ConfigurationException e in coll) AddError(ref errors, e);
        }

        internal static ConfigurationErrorsException GetErrorsException(List<ConfigurationException> errors)
        {
            if (errors == null) return null;

            Debug.Assert(errors.Count != 0, "errors.Count != 0");
            return new ConfigurationErrorsException(errors);
        }

        internal static void ThrowOnErrors(List<ConfigurationException> errors)
        {
            ConfigurationErrorsException e = GetErrorsException(errors);
            if (e != null) throw e;
        }
    }
}