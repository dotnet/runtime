// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents a text node that contains CDATA.
    /// </summary>
    public class XCData : XText
    {
        /// <summary>
        /// Initializes a new instance of the XCData class.
        /// </summary>
        /// <param name="value">The string that contains the value of the XCData node.</param>
        public XCData(string value) : base(value) { }

        /// <summary>
        /// Initializes a new instance of the XCData class from another XCData object.
        /// </summary>
        /// <param name="other">Text node to copy from</param>
        public XCData(XCData other) : base(other) { }

        internal XCData(XmlReader r) : base(r) { }

        /// <summary>
        /// Gets the node type for this node.
        /// </summary>
        /// <remarks>
        /// This property will always return XmlNodeType.CDATA.
        /// </remarks>
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.CDATA;
            }
        }

        /// <summary>
        /// Write this <see cref="XCData"/> to the given <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XCData"/> to.
        /// </param>
        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteCData(text);
        }

        /// <summary>
        /// Write this <see cref="XCData"/> to the given <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XCData"/> to.
        /// </param>
        /// <param name="cancellationToken">
        /// The CancellationToken to use to request cancellation of this operation.
        /// </param>
        /// <returns>
        /// A Task that represents the eventual completion of the operation.
        /// </returns>
        public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            return writer.WriteCDataAsync(text);
        }

        internal override XNode CloneNode()
        {
            return new XCData(this);
        }
    }
}
