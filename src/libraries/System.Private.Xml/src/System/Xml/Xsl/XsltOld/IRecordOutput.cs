// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    internal interface IRecordOutput
    {
        Processor.OutputResult RecordDone(RecordBuilder record);
        void TheEnd();
    }
}
