// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    internal interface RecordOutput
    {
        Processor.OutputResult RecordDone(RecordBuilder record);
        void TheEnd();
    }
}
