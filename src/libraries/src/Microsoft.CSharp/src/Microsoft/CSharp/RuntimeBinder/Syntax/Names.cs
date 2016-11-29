// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal class Name
    {
        private readonly string _text;

        public Name(string text)
        {
            _text = text;
        }

        public string Text
        {
            get { return _text; }
        }

        public virtual PredefinedName PredefinedName
        {
            get { return PredefinedName.PN_COUNT; }
        }

        public override string ToString()
        {
            return _text;
        }
    }
}
