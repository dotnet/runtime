// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace System.Security.Cryptography.Xml
{
    // A class representing conversion from Base64 using CryptoStream
    public class XmlDsigBase64Transform : Transform
    {
        private Type[] _inputTypes = { typeof(Stream), typeof(XmlNodeList), typeof(XmlDocument) };
        private Type[] _outputTypes = { typeof(Stream) };
        private CryptoStream _cs = null;

        public XmlDsigBase64Transform()
        {
            Algorithm = SignedXml.XmlDsigBase64TransformUrl;
        }

        public override Type[] InputTypes
        {
            get { return _inputTypes; }
        }

        public override Type[] OutputTypes
        {
            get { return _outputTypes; }
        }

        public override void LoadInnerXml(XmlNodeList nodeList)
        {
        }

        protected override XmlNodeList GetInnerXml()
        {
            return null;
        }

        public override void LoadInput(object obj)
        {
            if (obj is Stream)
            {
                LoadStreamInput((Stream)obj);
                return;
            }
            if (obj is XmlNodeList)
            {
                LoadXmlNodeListInput((XmlNodeList)obj);
                return;
            }
            if (obj is XmlDocument)
            {
                LoadXmlNodeListInput(((XmlDocument)obj).SelectNodes("//."));
                return;
            }
        }

        private void LoadStreamInput(Stream inputStream)
        {
            if (inputStream == null) throw new ArgumentException("obj");
            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            do
            {
                bytesRead = inputStream.Read(buffer, 0, 1024);
                if (bytesRead > 0)
                {
                    int i = 0;
                    int j = 0;
                    while ((j < bytesRead) && (!char.IsWhiteSpace((char)buffer[j]))) j++;
                    i = j; j++;
                    while (j < bytesRead)
                    {
                        if (!char.IsWhiteSpace((char)buffer[j]))
                        {
                            buffer[i] = buffer[j];
                            i++;
                        }
                        j++;
                    }
                    ms.Write(buffer, 0, i);
                }
            } while (bytesRead > 0);
            ms.Position = 0;
            _cs = new CryptoStream(ms, new FromBase64Transform(), CryptoStreamMode.Read);
        }

        private void LoadXmlNodeListInput(XmlNodeList nodeList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (XmlNode node in nodeList)
            {
                XmlNode result = node.SelectSingleNode("self::text()");
                if (result != null)
                    sb.Append(result.OuterXml);
            }
            UTF8Encoding utf8 = new UTF8Encoding(false);
            byte[] buffer = utf8.GetBytes(sb.ToString());
            int i = 0;
            int j = 0;
            while ((j < buffer.Length) && (!char.IsWhiteSpace((char)buffer[j]))) j++;
            i = j; j++;
            while (j < buffer.Length)
            {
                if (!char.IsWhiteSpace((char)buffer[j]))
                {
                    buffer[i] = buffer[j];
                    i++;
                }
                j++;
            }
            MemoryStream ms = new MemoryStream(buffer, 0, i);
            _cs = new CryptoStream(ms, new FromBase64Transform(), CryptoStreamMode.Read);
        }

        public override object GetOutput()
        {
            return _cs;
        }

        public override object GetOutput(Type type)
        {
            if (type != typeof(Stream) && !type.IsSubclassOf(typeof(Stream)))
                throw new ArgumentException(SR.Cryptography_Xml_TransformIncorrectInputType, nameof(type));
            return _cs;
        }
    }
}
