// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal class Name
    {
        private string _text;

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
