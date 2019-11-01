// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Opt-In flag to look for resources in the same assembly but with the "bitmapSuffix" config setting.
    /// i.e. System.Web.UI.WebControl.Button.bmp -> System.Web.UI.WebControl.Button.VisualStudio.11.0.bmp
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BitmapSuffixInSameAssemblyAttribute : Attribute
    {
    }
}
