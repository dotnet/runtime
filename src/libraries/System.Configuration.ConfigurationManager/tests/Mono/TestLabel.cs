// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// TestLabel.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;
using System.Collections.Generic;

namespace MonoTests.System.Configuration.Util
{

    public class TestLabel
    {

        List<Scope> scopes;
        string delimiter;
        Style defaultStyle;

        public enum Style
        {
            Letter,
            Number,
            HexNumer
        }

        public TestLabel(string prefix)
            : this(prefix, ".", Style.Letter)
        {
        }

        public TestLabel(string prefix, string delimiter, Style style)
        {
            if ((prefix == null) || (prefix.Equals(string.Empty)))
                throw new ArgumentException("Cannot be null or empty.", "prefix");
            if (delimiter == null)
                throw new ArgumentNullException(nameof(delimiter));

            scopes = new List<Scope>();
            scopes.Add(new Scope(prefix, style));

            this.delimiter = delimiter;
            defaultStyle = style;
        }

        class Scope
        {
            public readonly string Text;
            public readonly Style Style;
            int id;

            public Scope(string text, Style style)
            {
                Text = text;
                Style = style;
                id = 0;
            }

            public int GetID()
            {
                return ++id;
            }
        }

        public void EnterScope(string scope)
        {
            scopes.Add(new Scope(scope, defaultStyle));
        }

        public void LeaveScope()
        {
            if (scopes.Count <= 1)
                throw new InvalidOperationException();
            scopes.RemoveAt(scopes.Count - 1);
        }

        public string Get()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < scopes.Count; i++)
            {
                sb.Append(scopes[i].Text);
                sb.Append(delimiter);
            }

            var scope = scopes[scopes.Count - 1];
            var id = scope.GetID();

            switch (scope.Style)
            {
                case Style.Letter:
                    if (id <= 26)
                        sb.Append((char)('a' + id - 1));
                    else
                        goto case Style.Number;
                    break;

                case Style.Number:
                    sb.Append(id);
                    break;

                case Style.HexNumer:
                    sb.AppendFormat("{0:x2}", id);
                    break;
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < scopes.Count; i++)
            {
                if (i > 0)
                    sb.Append(delimiter);
                sb.Append(scopes[i].Text);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
