// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System.Collections;

namespace System
{
    public static partial class Environment
    {
        public static string GetEnvironmentVariable(string variable)
        {
            return EnvironmentAugments.GetEnvironmentVariable(variable);
        }

        public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            return EnvironmentAugments.GetEnvironmentVariable(variable, target);
        }

        public static IDictionary GetEnvironmentVariables()
        {
            return EnvironmentAugments.GetEnvironmentVariables();
        }

        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            return EnvironmentAugments.GetEnvironmentVariables(target);
        }

        public static void SetEnvironmentVariable(string variable, string value)
        {
            EnvironmentAugments.SetEnvironmentVariable(variable, value);
        }

        public static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target)
        {
            EnvironmentAugments.SetEnvironmentVariable(variable, value, target);
        }
    }
}
