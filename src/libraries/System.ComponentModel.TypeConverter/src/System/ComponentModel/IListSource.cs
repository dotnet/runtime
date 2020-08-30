// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.ComponentModel
{
    [MergableProperty(false)]
    [Editor("System.Windows.Forms.Design.DataSourceListEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public interface IListSource
    {
        bool ContainsListCollection { get; }

        IList GetList();
    }
}
