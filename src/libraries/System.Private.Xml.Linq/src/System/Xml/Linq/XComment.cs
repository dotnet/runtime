// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents an XML comment.
    /// </summary>
    public class XComment : XNode
    {
        internal string value;

        /// <overloads>
        /// Initializes a new instance of the <see cref="XComment"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="XComment"/> class with the
        /// specified string content.
        /// </summary>
        /// <param name="value">
        /// The contents of the new XComment object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified value is null.
        /// </exception>
        public XComment(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            this.value = value;
        }

        /// <summary>
        /// Initializes a new comment node from an existing comment node.
        /// </summary>
        /// <param name="other">Comment node to copy from.</param>
        public XComment(XComment other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.value = other.value;
        }

        internal XComment(XmlReader r)
        {
            value = r.Value;
            r.Read();
        }

        /// <summary>
        /// Gets the node type for this node.
        /// </summary>
        /// <remarks>
        /// This property will always return XmlNodeType.Comment.
        /// </remarks>
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.Comment;
            }
        }

        /// <summary>
        /// Gets or sets the string value of this comment.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified value is null.
        /// </exception>
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                this.value = value;
                if (notify) NotifyChanged(this, XObjectChangeEventArgs.Value);
            }
        }

        /// <summary>
        /// Write this <see cref="XComment"/> to the passed in <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XComment"/> to.
        /// </param>
        public override void WriteTo(XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteComment(value);
        }

        /// <summary>
        /// Write this <see cref="XComment"/> to the passed in <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XComment"/> to.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(writer);

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            return writer.WriteCommentAsync(value);
        }

        internal override XNode CloneNode()
        {
            return new XComment(this);
        }

        internal override bool DeepEquals(XNode node)
        {
            XComment? other = node as XComment;
            return other != null && value == other.value;
        }

        internal override int GetDeepHashCode()
        {
            return value.GetHashCode();
        }
    }
}
