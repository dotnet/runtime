// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Xml.Xsl
{
    public abstract class XsltMessageEncounteredEventArgs : EventArgs
    {
        public abstract string Message { get; }
    }

    public delegate void XsltMessageEncounteredEventHandler(object sender, XsltMessageEncounteredEventArgs e);

    public class XsltArgumentList
    {
        private Hashtable _parameters = new Hashtable();
        private Hashtable _extensions = new Hashtable();

        // Used for reporting xsl:message's during execution
        internal XsltMessageEncounteredEventHandler xsltMessageEncountered = null;

        public XsltArgumentList() { }

        public object GetParam(string name, string namespaceUri)
        {
            return _parameters[new XmlQualifiedName(name, namespaceUri)];
        }

        public object GetExtensionObject(string namespaceUri)
        {
            return _extensions[namespaceUri];
        }

        public void AddParam(string name, string namespaceUri, object parameter)
        {
            CheckArgumentNull(name, nameof(name));
            CheckArgumentNull(namespaceUri, nameof(namespaceUri));
            CheckArgumentNull(parameter, nameof(parameter));

            XmlQualifiedName qname = new XmlQualifiedName(name, namespaceUri);
            qname.Verify();
            _parameters.Add(qname, parameter);
        }

        public void AddExtensionObject(string namespaceUri, object extension)
        {
            CheckArgumentNull(namespaceUri, nameof(namespaceUri));
            CheckArgumentNull(extension, nameof(extension));
            _extensions.Add(namespaceUri, extension);
        }

        public object RemoveParam(string name, string namespaceUri)
        {
            XmlQualifiedName qname = new XmlQualifiedName(name, namespaceUri);
            object parameter = _parameters[qname];
            _parameters.Remove(qname);
            return parameter;
        }

        public object RemoveExtensionObject(string namespaceUri)
        {
            object extension = _extensions[namespaceUri];
            _extensions.Remove(namespaceUri);
            return extension;
        }

        public event XsltMessageEncounteredEventHandler XsltMessageEncountered
        {
            add
            {
                xsltMessageEncountered += value;
            }
            remove
            {
                xsltMessageEncountered -= value;
            }
        }

        public void Clear()
        {
            _parameters.Clear();
            _extensions.Clear();
            xsltMessageEncountered = null;
        }

        private static void CheckArgumentNull(object param, string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
