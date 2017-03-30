// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal partial class NameManager
    {
        private readonly NameTable _names;

        internal NameManager()
            : this(new NameTable())
        {
        }

        private NameManager(NameTable nameTable)
        {
            _names = nameTable;
            InitKnownNames();
        }

        internal Name Add(string key)
        {
            if (key == null)
            {
                throw Error.InternalCompilerError();
            }
            Name name = s_knownNames.Lookup(key);
            if (name == null)
            {
                name = _names.Add(key);
            }
            return name;
        }

        internal Name Lookup(string key)
        {
            if (key == null)
            {
                throw Error.InternalCompilerError();
            }
            Name name = s_knownNames.Lookup(key);
            if (name == null)
            {
                name = _names.Lookup(key);
            }
            return name;
        }

        internal Name GetPredefinedName(PredefinedName id)
        {
            return s_predefinedNames[(int)id];
        }

        internal Name GetPredefName(PredefinedName id)
        {
            return GetPredefinedName(id);
        }
    }
}
