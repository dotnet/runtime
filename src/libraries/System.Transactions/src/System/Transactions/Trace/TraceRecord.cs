// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Transactions.Diagnostics
{
    /// <summary>
    /// Base class for the team-specific traces that contain structured data.
    /// </summary>
    internal abstract class TraceRecord
    {
        internal protected const string EventIdBase = "http://schemas.microsoft.com/2004/03/Transactions/";
        internal protected const string NamespaceSuffix = "TraceRecord";

        /// <summary>
        /// Defines object layout.
        /// </summary>
        internal virtual string EventId { get { return EventIdBase + "Empty" + TraceRecord.NamespaceSuffix; } }

        public override string ToString()
        {
            PlainXmlWriter xml = new PlainXmlWriter();
            WriteTo(xml);
            return xml.ToString();
        }

        internal abstract void WriteTo(XmlWriter xml);
    }
}
