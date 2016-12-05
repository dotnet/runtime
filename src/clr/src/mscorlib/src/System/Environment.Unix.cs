// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System
{
    public static partial class Environment
    {
        private static readonly unsafe Lazy<Dictionary<string, string>> s_environ = new Lazy<Dictionary<string, string>>(() =>
        {
            // We cache on Unix as using the block isn't thread safe
            return GetRawEnvironmentVariables();
        });

        private static string GetEnvironmentVariableCore(string variable)
        {
            // Ensure variable doesn't include a null char
            int nullEnd = variable.IndexOf('\0');
            if (nullEnd != -1)
            {
                variable = variable.Substring(0, nullEnd);
            }

            // Get the value of the variable
            lock (s_environ)
            {
                string value;
                return s_environ.Value.TryGetValue(variable, out value) ? value : null;
            }
        }

        private static string GetEnvironmentVariableCore(string variable, EnvironmentVariableTarget target)
        {
            return target == EnvironmentVariableTarget.Process ?
                GetEnvironmentVariableCore(variable) :
                null;
        }

        private static IDictionary GetEnvironmentVariablesCore()
        {
            lock (s_environ)
            {
                return new Dictionary<string, string>(s_environ.Value);
            }
        }

        private static IDictionary GetEnvironmentVariablesCore(EnvironmentVariableTarget target)
        {
            return target == EnvironmentVariableTarget.Process ?
                GetEnvironmentVariablesCore() :
                new Dictionary<string, string>();
        }

        private static void SetEnvironmentVariableCore(string variable, string value)
        {
            int nullEnd;

            // Ensure variable doesn't include a null char
            nullEnd = variable.IndexOf('\0');
            if (nullEnd != -1)
            {
                variable = variable.Substring(0, nullEnd);
            }

            // Ensure value doesn't include a null char
            if (value != null)
            {
                nullEnd = value.IndexOf('\0');
                if (nullEnd != -1)
                {
                    value = value.Substring(0, nullEnd);
                }
            }

            lock (s_environ)
            {
                // Remove the entry if the value is null, otherwise add/overwrite it
                if (value == null)
                {
                    s_environ.Value.Remove(variable);
                }
                else
                {
                    s_environ.Value[variable] = value;
                }
            }
        }

        private static void SetEnvironmentVariableCore(string variable, string value, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                SetEnvironmentVariableCore(variable, value);
            }
            // other targets ignored
        }
    }
}