//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

/*
 *  Some parts of the code have been disabled for testing purposes.
 *  
 */

#define BINARY
namespace System.ServiceModel { 
    using System.Collections.Generic;
    using System.Runtime;
    using System.ServiceModel.Channels;
    using System.Xml;
    using System.IO; // added to use buffered stream
                     //using System.ServiceModel;



    class XmlBuffer
    {
        List<Section> sections;
        byte[] buffer;
        int offset;
        BufferedOutputStream stream; //BufferedStream - Original: BufferedOutputStream
        BufferState bufferState;
        XmlDictionaryWriter writer;
        XmlDictionaryReaderQuotas quotas;

        enum BufferState
        {
            Created,
            Writing,
            Reading,
        }

        struct Section
        {
            int offset;
            int size;
            XmlDictionaryReaderQuotas quotas;

            public Section(int offset, int size, XmlDictionaryReaderQuotas quotas)
            {
                this.offset = offset;
                this.size = size;
                this.quotas = quotas;
            }

            public int Offset
            {
                get { return this.offset; }
            }

            public int Size
            {
                get { return this.size; }
            }

            public XmlDictionaryReaderQuotas Quotas
            {
                get { return this.quotas; }
            }
        }

        public XmlBuffer(int maxBufferSize)
        {
            if (maxBufferSize < 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("maxBufferSize", maxBufferSize,
                                                                            SR.GetString(SR.ValueMustBeNonNegative)));

            int initialBufferSize = Math.Min(512, maxBufferSize);

           
            stream = new BufferManagerOutputStream(SR.XmlBufferQuotaExceeded, initialBufferSize, maxBufferSize,
                InternalBufferManager.Create(0, int.MaxValue));
                     

            //stream = new BufferedStream(, maxBufferSize); // replacing code above

            sections = new List<Section>(1);
        }

        public int BufferSize
        {
            get
            {
#if disabled

                Fx.Assert(bufferState == BufferState.Reading, "Buffer size shuold only be retrieved during Reading state");
#endif
                return buffer.Length;
            }
        }

        public int SectionCount
        {
            get { return this.sections.Count; }
        }

        public XmlDictionaryWriter OpenSection(XmlDictionaryReaderQuotas quotas)
        {
            if (bufferState != BufferState.Created)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            bufferState = BufferState.Writing;
            this.quotas = new XmlDictionaryReaderQuotas();
            quotas.CopyTo(this.quotas);
            //if (this.writer == null)
            //{

            //this.writer = XmlDictionaryWriter.CreateBinaryWriter(stream, XD.Dictionary, null, true);
            this.writer = XmlDictionaryWriter.CreateBinaryWriter(stream); // this code replaces above

            //}
            //else
            //{
            //    //((IXmlBinaryWriterInitializer)this.writer).SetOutput(stream, XD.Dictionary, null, true);
            //}
            return this.writer;
        }

        public void CloseSection()
        {
            if (bufferState != BufferState.Writing)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            this.writer.Close();
            bufferState = BufferState.Created;
            int size = (int)stream.Length - offset;
            sections.Add(new Section(offset, size, this.quotas));
            offset += size;
        }

        public void Close()
        {
            if (bufferState != BufferState.Created)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            bufferState = BufferState.Reading;
            //int bufferSize = 0;
            //buffer = stream.ToArray(out bufferSize); NOT SUPPORTED
            
            //Implementation to do the same that the line above
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            buffer = ms.ToArray();
            //-----
            writer = null;
            stream = null;
        }

        Exception CreateInvalidStateException()
        {
            return new InvalidOperationException(SR.GetString(SR.XmlBufferInInvalidState));
        }



        public XmlDictionaryReader GetReader(int sectionIndex)
        {
            if (bufferState != BufferState.Reading)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            Section section = sections[sectionIndex];
            XmlDictionaryReader reader = null;// XmlDictionaryReader.CreateBinaryReader(buffer, section.Offset, section.Size, XD.Dictionary, section.Quotas, null, null);
            reader.MoveToContent();
            return reader;
        }



        public void WriteTo(int sectionIndex, XmlWriter writer)
        {
            if (bufferState != BufferState.Reading)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            XmlDictionaryReader reader = GetReader(sectionIndex);
            try
            {
                writer.WriteNode(reader, false);
            }
            finally
            {
                reader.Close();
            }
        }
    }
}
