// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal partial class NameManager
    {
        private readonly NameTable _names = new NameTable();

        internal Name Add(string key)
        {
            if (key == null)
            {
                throw Error.InternalCompilerError();
            }

            return s_knownNames.Lookup(key) ?? _names.Add(key);
        }

        internal Name Lookup(string key)
        {
            if (key == null)
            {
                throw Error.InternalCompilerError();
            }

            return s_knownNames.Lookup(key) ?? _names.Lookup(key);
        }

        internal static Name GetPredefinedName(PredefinedName id)
        {
            return s_predefinedNames[(int)id];
        }
    }
}
