// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Drawing.Imaging;

namespace System.Drawing.Printing
{
    public partial class PreviewPrintController : PrintController
    {
        private readonly IList _list = new ArrayList();

        public override bool IsPreview => true;

        public virtual bool UseAntiAlias { get; set; }

        public PreviewPageInfo[] GetPreviewPageInfo()
        {
            var temp = new PreviewPageInfo[_list.Count];
            _list.CopyTo(temp, 0);
            return temp;
        }
    }
}
