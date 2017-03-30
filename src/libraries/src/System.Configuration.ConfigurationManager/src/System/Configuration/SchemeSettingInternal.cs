// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Configuration
{
    internal sealed class SchemeSettingInternal
    {
        private string name;
        private GenericUriParserOptions options;

        public SchemeSettingInternal(string name, GenericUriParserOptions options)
        {
            Debug.Assert(name != null, "'name' must not be null.");

            this.name = name.ToLowerInvariant();
            this.options = options;
        }

        public string Name
        {
            get { return name; }
        }

        public GenericUriParserOptions Options
        {
            get { return options; }
        }
    }
}
