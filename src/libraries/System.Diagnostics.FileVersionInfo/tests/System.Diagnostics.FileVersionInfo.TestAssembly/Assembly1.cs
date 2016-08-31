// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

// Comments
[assembly:AssemblyDescriptionAttribute("Have you played a Contoso amusement device today?")]
// CompanyName
[assembly:AssemblyCompanyAttribute("The name of the company.")]
// FileDescription
[assembly:AssemblyTitleAttribute("My File")]
// FileVersion
[assembly:AssemblyFileVersionAttribute("4.3.2.1")]
// ProductVersion (overrides FileVersion to be the ProductVersion)
[assembly: AssemblyInformationalVersionAttribute("1.2.3-beta.4")]
// LegalCopyright
[assembly:AssemblyCopyrightAttribute("Copyright, you betcha!")]
// LegalTrademarks
[assembly:AssemblyTrademarkAttribute("TM")]
// Product
[assembly:AssemblyProductAttribute("The greatest product EVER")]

namespace System.Diagnostics.Tests
{
    public class Test
    {
        public static void Main()
        {
        }
    }
}

