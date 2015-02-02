// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal partial class NameManager
    {
        private NameTable _names;

        internal NameManager()
            : this(new NameTable())
        {
        }

        internal NameManager(NameTable nameTable)
        {
            _names = nameTable;
            this.InitKnownNames();
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
