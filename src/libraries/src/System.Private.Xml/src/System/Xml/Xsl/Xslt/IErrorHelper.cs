// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl
{
    internal interface IErrorHelper
    {
        void ReportError(string res, params string[] args);

        void ReportWarning(string res, params string[] args);
    }
}
