// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using XmlCoreTest.Common;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public class LoadSaveAsyncTests : BridgeHelpers
    {
        [Fact]
        public static void ArgumentValidation()
        {
            // Verify that ArgumentNullExceptions are thrown when passing null to LoadAsync and SaveAsync
            Assert.Throws<ArgumentNullException>(() => { XDocument.LoadAsync((XmlReader)null, LoadOptions.None, CancellationToken.None); });
            Assert.Throws<ArgumentNullException>(() => { new XDocument().SaveAsync((XmlWriter)null, CancellationToken.None); });
            Assert.Throws<ArgumentNullException>(() => { XElement.LoadAsync((XmlReader)null, LoadOptions.None, CancellationToken.None); });
            Assert.Throws<ArgumentNullException>(() => { new XElement("Name").SaveAsync((XmlWriter)null, CancellationToken.None); });
        }

        [Fact]
        public static async Task AlreadyCanceled()
        {
            // Verify that providing an already canceled cancellation token will result in a canceled task

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => XDocument.LoadAsync(Stream.Null, LoadOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => XDocument.LoadAsync(StreamReader.Null, LoadOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => XDocument.LoadAsync(XmlReader.Create(Stream.Null), LoadOptions.None, new CancellationToken(true)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new XDocument().SaveAsync(Stream.Null, SaveOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new XDocument().SaveAsync(StreamWriter.Null, SaveOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new XDocument().SaveAsync(XmlWriter.Create(Stream.Null), new CancellationToken(true)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => XElement.LoadAsync(Stream.Null, LoadOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => XElement.LoadAsync(StreamReader.Null, LoadOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => XElement.LoadAsync(XmlReader.Create(Stream.Null), LoadOptions.None, new CancellationToken(true)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new XElement("Name").SaveAsync(Stream.Null, SaveOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new XElement("Name").SaveAsync(StreamWriter.Null, SaveOptions.None, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new XElement("Name").SaveAsync(XmlWriter.Create(Stream.Null), new CancellationToken(true)));
        }

        [Theory]
        [MemberData(nameof(RoundtripOptions_MemberData))]
        public static async Task RoundtripSyncAsyncMatches_XmlReader(bool document, LoadOptions loadOptions, SaveOptions saveOptions)
        {
            // Create reader and writer settings
            var readerSettings = new XmlReaderSettings();
            var writerSettings = new XmlWriterSettings();
            if ((saveOptions & SaveOptions.OmitDuplicateNamespaces) != 0)
            {
                writerSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            }
            if ((saveOptions & SaveOptions.DisableFormatting) != 0)
            {
                writerSettings.Indent = false;
                writerSettings.NewLineHandling = NewLineHandling.None;
            }

            // Roundtrip XML using synchronous and XmlReader/Writer
            MemoryStream syncOutput = new MemoryStream();
            using (XmlReader syncReader = XmlReader.Create(FilePathUtil.getStream(GetTestFileName()), readerSettings))
            using (XmlWriter syncWriter = XmlWriter.Create(syncOutput, writerSettings))
            {
                if (document)
                {
                    XDocument syncDoc = XDocument.Load(syncReader, loadOptions);
                    syncDoc.Save(syncWriter);
                }
                else
                {
                    XElement syncElement = XElement.Load(syncReader, loadOptions);
                    syncElement.Save(syncWriter);
                }
            }

            // Roundtrip XML using asynchronous and XmlReader/Writer
            readerSettings.Async = true;
            writerSettings.Async = true;
            MemoryStream asyncOutput = new MemoryStream();
            using (XmlReader asyncReader = XmlReader.Create(FilePathUtil.getStream(GetTestFileName()), readerSettings))
            using (XmlWriter asyncWriter = XmlWriter.Create(asyncOutput, writerSettings))
            {
                if (document)
                {
                    XDocument asyncDoc = await XDocument.LoadAsync(asyncReader, loadOptions, CancellationToken.None);
                    await asyncDoc.SaveAsync(asyncWriter, CancellationToken.None);
                }
                else
                {
                    XElement asyncElement = await XElement.LoadAsync(asyncReader, loadOptions, CancellationToken.None);
                    await asyncElement.SaveAsync(asyncWriter, CancellationToken.None);
                }
            }

            // Compare to make sure the synchronous and asynchronous results are the same
            Assert.Equal(syncOutput.ToArray(), asyncOutput.ToArray());
        }

        [Theory]
        [MemberData(nameof(RoundtripOptions_MemberData))]
        public static async Task RoundtripSyncAsyncMatches_StreamReader(bool document, LoadOptions loadOptions, SaveOptions saveOptions)
        {
            // Roundtrip XML using synchronous and StreamReader/Writer
            MemoryStream syncOutput = new MemoryStream();
            using (StreamReader syncReader = new StreamReader(FilePathUtil.getStream(GetTestFileName())))
            using (StreamWriter syncWriter = new StreamWriter(syncOutput))
            {
                if (document)
                {
                    XDocument syncDoc = XDocument.Load(syncReader, loadOptions);
                    syncDoc.Save(syncWriter, saveOptions);
                }
                else
                {
                    XElement syncElement = XElement.Load(syncReader, loadOptions);
                    syncElement.Save(syncWriter, saveOptions);
                }
            }

            // Roundtrip XML using asynchronous and StreamReader/Writer
            MemoryStream asyncOutput = new MemoryStream();
            using (StreamReader asyncReader = new StreamReader(FilePathUtil.getStream(GetTestFileName())))
            using (StreamWriter asyncWriter = new StreamWriter(asyncOutput))
            {
                if (document)
                {
                    XDocument asyncDoc = await XDocument.LoadAsync(asyncReader, loadOptions, CancellationToken.None);
                    await asyncDoc.SaveAsync(asyncWriter, saveOptions, CancellationToken.None);
                }
                else
                {
                    XElement asyncElement = await XElement.LoadAsync(asyncReader, loadOptions, CancellationToken.None);
                    await asyncElement.SaveAsync(asyncWriter, saveOptions, CancellationToken.None);
                }
            }

            // Compare to make sure the synchronous and asynchronous results are the same
            Assert.Equal(syncOutput.ToArray(), asyncOutput.ToArray());
        }

        [Theory]
        [MemberData(nameof(RoundtripOptions_MemberData))]
        public static async Task RoundtripSyncAsyncMatches_Stream(bool document, LoadOptions loadOptions, SaveOptions saveOptions)
        {
            // Roundtrip XML using synchronous and Stream
            MemoryStream syncOutput = new MemoryStream();
            using (Stream syncStream = FilePathUtil.getStream(GetTestFileName()))
            {
                if (document)
                {
                    XDocument syncDoc = XDocument.Load(syncStream, loadOptions);
                    syncDoc.Save(syncOutput, saveOptions);
                }
                else
                {
                    XElement syncElement = XElement.Load(syncStream, loadOptions);
                    syncElement.Save(syncOutput, saveOptions);
                }
            }

            // Roundtrip XML using asynchronous and Stream
            MemoryStream asyncOutput = new MemoryStream();
            using (Stream asyncStream = FilePathUtil.getStream(GetTestFileName()))
            {
                if (document)
                {
                    XDocument asyncDoc = await XDocument.LoadAsync(asyncStream, loadOptions, CancellationToken.None);
                    await asyncDoc.SaveAsync(asyncOutput, saveOptions, CancellationToken.None);
                }
                else
                {
                    XElement asyncElement = await XElement.LoadAsync(asyncStream, loadOptions, CancellationToken.None);
                    await asyncElement.SaveAsync(asyncOutput, saveOptions, CancellationToken.None);
                }
            }

            // Compare to make sure the synchronous and asynchronous results are the same
            Assert.Equal(syncOutput.ToArray(), asyncOutput.ToArray());
        }

        // Inputs to the Roundtrip* tests:
        // - Boolean for whether to test XDocument (true) or XElement (false)
        // - LoadOptions value
        // - SaveOptions value
        public static IEnumerable<object[]> RoundtripOptions_MemberData
        {
            get
            {
                foreach (bool doc in new[] { true, false })
                    foreach (LoadOptions loadOptions in Enum.GetValues(typeof(LoadOptions)))
                        foreach (SaveOptions saveOptions in Enum.GetValues(typeof(SaveOptions)))
                            yield return new object[] { doc, loadOptions, saveOptions };
            }
        }

    }
}