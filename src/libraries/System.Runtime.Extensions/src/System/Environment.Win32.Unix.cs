// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System.Collections;
using System.Collections.Generic;

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
            // To maintain complete compatibility with prior versions we need to return a Hashtable.
            // We did ship a prior version of Core with LowLevelDictionary, which does iterate the
            // same (e.g. yields DictionaryEntry), but it is not a public type.
            //
            // While we could pass Hashtable back from CoreCLR the type is also defined here. We only
            // want to surface the local Hashtable.
            return new Hashtable(EnvironmentAugments.GetEnvironmentVariables());
        }

        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            // See comments in GetEnvironmentVariables()
            return new Hashtable(EnvironmentAugments.GetEnvironmentVariables(target));
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
