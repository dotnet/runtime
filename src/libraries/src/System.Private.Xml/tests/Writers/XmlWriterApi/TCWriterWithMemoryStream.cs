// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCWriterWithMemoryStream : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCWriterWithMemoryStream
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UnicodeWriterIndent)
            {
                return;
            }
            
            this.AddChild(new CVariation(TFS_661130) { Attribute = new Variation("XmlWellFormedWriter.Close() throws IndexOutOfRangeException") });
            this.AddChild(new CVariation(XmlWellFormedWriterCloseThrowsIndexOutOfRangeException) { Attribute = new Variation("XmlWellFormedWriter.Close() throws IndexOutOfRangeException") });
            this.AddChild(new CVariation(TFS_661130b) { Attribute = new Variation("XmlWellFormedWriter.Close() throws IndexOutOfRangeException") });
            this.AddChild(new CVariation(TFS_860167) { Attribute = new Variation("IPublisher.PublishPackage crashes due to disposed.MS") { Param = "FileStream" } });
            this.AddChild(new CVariation(TFS_860167a) { Attribute = new Variation("IPublisher.PublishPackage crashes due to disposed.FS") });
            this.AddChild(new CVariation(TFS_860167e) { Attribute = new Variation("IPublisher.PublishPackage crashes due to disposed.BS with MS") });
            this.AddChild(new CVariation(TFS_860167f) { Attribute = new Variation("IPublisher.PublishPackage crashes due to disposed.BS with FS") });
            this.AddChild(new CVariation(TFS_860167b) { Attribute = new Variation("IPublisher.PublishPackage crashes due to dispose.MS.WriteRaw") });
            this.AddChild(new CVariation(TFS_860167c) { Attribute = new Variation("IPublisher.PublishPackage crashes due to dispose.MS.WriteComment") });
            this.AddChild(new CVariation(TFS_860167d) { Attribute = new Variation("IPublisher.PublishPackage crashes due to dispose.MS.WriteCData") });
        }
    }
}
