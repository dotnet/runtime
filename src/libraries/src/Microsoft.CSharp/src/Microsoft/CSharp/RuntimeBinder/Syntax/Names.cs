// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal sealed class Name
    {
        public Name(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public override string ToString() => Text;
    }
}
