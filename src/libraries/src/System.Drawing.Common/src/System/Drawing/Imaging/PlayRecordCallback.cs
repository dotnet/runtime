// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <include file='doc\PlayRecordCAllback.uex' path='docs/doc[@for="PlayRecodCallBack"]/*' />
    /// <devdoc>
    /// </devdoc>
    public delegate void PlayRecordCallback(EmfPlusRecordType recordType,
                                            int flags,
                                            int dataSize,
                                            IntPtr recordData);
}


