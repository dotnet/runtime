// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail
{
    public class AlternateView : AttachmentBase
    {
        private LinkedResourceCollection _linkedResources;

        internal AlternateView()
        { }


        public AlternateView(string fileName) :
            base(fileName)
        { }

        public AlternateView(string fileName, string mediaType) :
            base(fileName, mediaType)
        { }

        public AlternateView(string fileName, ContentType contentType) :
            base(fileName, contentType)
        { }

        public AlternateView(Stream contentStream) :
            base(contentStream)
        { }

        public AlternateView(Stream contentStream, string mediaType) :
            base(contentStream, mediaType)
        { }

        public AlternateView(Stream contentStream, ContentType contentType) :
            base(contentStream, contentType)
        { }

        public LinkedResourceCollection LinkedResources
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }


                if (_linkedResources == null)
                {
                    _linkedResources = new LinkedResourceCollection();
                }
                return _linkedResources;
            }
        }

        public Uri BaseUri
        {
            get
            {
                return ContentLocation;
            }
            set
            {
                ContentLocation = value;
            }
        }

        public static AlternateView CreateAlternateViewFromString(string content)
        {
            AlternateView a = new AlternateView();
            a.SetContentFromString(content, null, string.Empty);
            return a;
        }

        public static AlternateView CreateAlternateViewFromString(string content, Encoding contentEncoding, string mediaType)
        {
            AlternateView a = new AlternateView();
            a.SetContentFromString(content, contentEncoding, mediaType);
            return a;
        }

        public static AlternateView CreateAlternateViewFromString(string content, ContentType contentType)
        {
            AlternateView a = new AlternateView();
            a.SetContentFromString(content, contentType);
            return a;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing && _linkedResources != null)
            {
                _linkedResources.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
