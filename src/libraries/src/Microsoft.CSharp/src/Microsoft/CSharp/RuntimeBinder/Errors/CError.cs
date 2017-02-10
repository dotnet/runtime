// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    ////////////////////////////////////////////////////////////////////////////////
    // CError
    //
    // This object is the implementation of ICSError for all compiler errors,
    // including lexer, parser, and compiler errors.

    internal sealed class CError
    {
        private string _text;

        private static string ComputeString(ErrorCode code, string[] args)
        {
            return String.Format(CultureInfo.InvariantCulture, ErrorFacts.GetMessage(code), args);
        }

        public void Initialize(ErrorCode code, string[] args)
        {
            _text = ComputeString(code, args);
        }

        public string Text { get { return _text; } }
    }
}
