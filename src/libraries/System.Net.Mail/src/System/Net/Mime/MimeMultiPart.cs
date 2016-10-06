// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;

namespace System.Net.Mime
{
    internal class MimeMultiPart : MimeBasePart
    {
        private Collection<MimeBasePart> _parts;
        private static int s_boundary;
        private AsyncCallback _mimePartSentCallback;
        private bool _allowUnicode;

        internal MimeMultiPart(MimeMultiPartType type)
        {
            MimeMultiPartType = type;
        }

        internal MimeMultiPartType MimeMultiPartType
        {
            set
            {
                if (value > MimeMultiPartType.Related || value < MimeMultiPartType.Mixed)
                {
                    throw new NotSupportedException(value.ToString());
                }
                SetType(value);
            }
        }

        private void SetType(MimeMultiPartType type)
        {
            ContentType.MediaType = "multipart" + "/" + type.ToString().ToLower(CultureInfo.InvariantCulture);
            ContentType.Boundary = GetNextBoundary();
        }

        internal Collection<MimeBasePart> Parts
        {
            get
            {
                if (_parts == null)
                {
                    _parts = new Collection<MimeBasePart>();
                }
                return _parts;
            }
        }

        internal void Complete(IAsyncResult result, Exception e)
        {
            //if we already completed and we got called again,
            //it mean's that there was an exception in the callback and we
            //should just rethrow it.

            MimePartContext context = (MimePartContext)result.AsyncState;

            if (context.completed)
            {
                throw e;
            }

            try
            {
                context.outputStream.Close();
            }
            catch (Exception ex)
            {
                if (e == null)
                {
                    e = ex;
                }
            }
            context.completed = true;
            context.result.InvokeCallback(e);
        }

        internal void MimeWriterCloseCallback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            ((MimePartContext)result.AsyncState).completedSynchronously = false;

            try
            {
                MimeWriterCloseCallbackHandler(result);
            }
            catch (Exception e)
            {
                Complete(result, e);
            }
        }

        private void MimeWriterCloseCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState;
            ((MimeWriter)context.writer).EndClose(result);
            Complete(result, null);
        }

        internal void MimePartSentCallback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            ((MimePartContext)result.AsyncState).completedSynchronously = false;

            try
            {
                MimePartSentCallbackHandler(result);
            }
            catch (Exception e)
            {
                Complete(result, e);
            }
        }

        private void MimePartSentCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState;
            MimeBasePart part = (MimeBasePart)context.partsEnumerator.Current;
            part.EndSend(result);

            if (context.partsEnumerator.MoveNext())
            {
                part = (MimeBasePart)context.partsEnumerator.Current;
                IAsyncResult sendResult = part.BeginSend(context.writer, _mimePartSentCallback, _allowUnicode, context);
                if (sendResult.CompletedSynchronously)
                {
                    MimePartSentCallbackHandler(sendResult);
                }
                return;
            }
            else
            {
                IAsyncResult closeResult = ((MimeWriter)context.writer).BeginClose(new AsyncCallback(MimeWriterCloseCallback), context);
                if (closeResult.CompletedSynchronously)
                {
                    MimeWriterCloseCallbackHandler(closeResult);
                }
            }
        }

        internal void ContentStreamCallback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            ((MimePartContext)result.AsyncState).completedSynchronously = false;

            try
            {
                ContentStreamCallbackHandler(result);
            }
            catch (Exception e)
            {
                Complete(result, e);
            }
        }

        private void ContentStreamCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState;
            context.outputStream = context.writer.EndGetContentStream(result);
            context.writer = new MimeWriter(context.outputStream, ContentType.Boundary);
            if (context.partsEnumerator.MoveNext())
            {
                MimeBasePart part = (MimeBasePart)context.partsEnumerator.Current;

                _mimePartSentCallback = new AsyncCallback(MimePartSentCallback);
                IAsyncResult sendResult = part.BeginSend(context.writer, _mimePartSentCallback, _allowUnicode, context);
                if (sendResult.CompletedSynchronously)
                {
                    MimePartSentCallbackHandler(sendResult);
                }
                return;
            }
            else
            {
                IAsyncResult closeResult = ((MimeWriter)context.writer).BeginClose(new AsyncCallback(MimeWriterCloseCallback), context);
                if (closeResult.CompletedSynchronously)
                {
                    MimeWriterCloseCallbackHandler(closeResult);
                }
            }
        }

        internal override IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, bool allowUnicode,
            object state)
        {
            _allowUnicode = allowUnicode;
            PrepareHeaders(allowUnicode);
            writer.WriteHeaders(Headers, allowUnicode);
            MimePartAsyncResult result = new MimePartAsyncResult(this, state, callback);
            MimePartContext context = new MimePartContext(writer, result, Parts.GetEnumerator());
            IAsyncResult contentResult = writer.BeginGetContentStream(new AsyncCallback(ContentStreamCallback), context);
            if (contentResult.CompletedSynchronously)
            {
                ContentStreamCallbackHandler(contentResult);
            }
            return result;
        }

        internal class MimePartContext
        {
            internal MimePartContext(BaseWriter writer, LazyAsyncResult result, IEnumerator<MimeBasePart> partsEnumerator)
            {
                this.writer = writer;
                this.result = result;
                this.partsEnumerator = partsEnumerator;
            }

            internal IEnumerator<MimeBasePart> partsEnumerator;
            internal Stream outputStream;
            internal LazyAsyncResult result;
            internal BaseWriter writer;
            internal bool completed;
            internal bool completedSynchronously = true;
        }

        internal override void Send(BaseWriter writer, bool allowUnicode)
        {
            PrepareHeaders(allowUnicode);
            writer.WriteHeaders(Headers, allowUnicode);
            Stream outputStream = writer.GetContentStream();
            MimeWriter mimeWriter = new MimeWriter(outputStream, ContentType.Boundary);

            foreach (MimeBasePart part in Parts)
            {
                part.Send(mimeWriter, allowUnicode);
            }

            mimeWriter.Close();
            outputStream.Close();
        }

        internal string GetNextBoundary()
        {
            int b = Interlocked.Increment(ref s_boundary) - 1;
            string boundaryString = "--boundary_" + b.ToString(CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString(null, CultureInfo.InvariantCulture);


            return boundaryString;
        }
    }
}
