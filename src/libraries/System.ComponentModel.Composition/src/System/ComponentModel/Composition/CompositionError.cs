// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Globalization;

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     Represents an error that occurs during composition.
    /// </summary>
    [DebuggerTypeProxy(typeof(CompositionErrorDebuggerProxy))]
    public class CompositionError
    {
        private readonly CompositionErrorId _id;
        private readonly string _description;
        private readonly Exception _exception;

        private readonly ICompositionElement _element;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionError"/> class
        ///     with the specified error message.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set the 
        ///     <see cref="Description"/> property to an empty string ("").
        /// </param>
        public CompositionError(string message)
            : this(CompositionErrorId.Unknown, message, (ICompositionElement)null, (Exception)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionError"/> class
        ///     with the specified error message and composition element that is the
        ///     cause of the composition error.
        /// </summary>
        /// <param name="element">
        ///     The <see cref="ICompositionElement"/> that is the cause of the
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set
        ///     the <see cref="CompositionError.Element"/> property to 
        ///     <see langword="null"/>.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set the 
        ///     <see cref="Description"/> property to an empty string ("").
        /// </param>
        public CompositionError(string message, ICompositionElement element)
            : this(CompositionErrorId.Unknown, message, element, (Exception)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionError"/> class 
        ///     with the specified error message and exception that is the cause of the  
        ///     composition error.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set the 
        ///     <see cref="Description"/> property to an empty string ("").
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception"/> that is the underlying cause of the 
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set
        ///     the <see cref="CompositionError.Exception"/> property to <see langword="null"/>.
        /// </param>
        public CompositionError(string message, Exception exception)
            : this(CompositionErrorId.Unknown, message, (ICompositionElement)null, exception)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionError"/> class 
        ///     with the specified error message, and composition element and exception that 
        ///     is the cause of the composition error.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set the 
        ///     <see cref="Description"/> property to an empty string ("").
        /// </param>
        /// <param name="element">
        ///     The <see cref="ICompositionElement"/> that is the cause of the
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set
        ///     the <see cref="CompositionError.Element"/> property to 
        ///     <see langword="null"/>.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception"/> that is the underlying cause of the 
        ///     <see cref="CompositionError"/>; or <see langword="null"/> to set
        ///     the <see cref="CompositionError.Exception"/> property to <see langword="null"/>.
        /// </param>
        public CompositionError(string message, ICompositionElement element, Exception exception)
            : this(CompositionErrorId.Unknown, message, element, exception)
        {
        }

        internal CompositionError(CompositionErrorId id, string description, ICompositionElement element, Exception exception)
        {
            _id = id;
            _description = description ?? string.Empty;
            _element = element;
            _exception = exception;
        }

        /// <summary>
        ///     Gets the composition element that is the cause of the error.
        /// </summary>
        /// <value>
        ///     The <see cref="ICompositionElement"/> that is the cause of the
        ///     <see cref="CompositionError"/>. The default is <see langword="null"/>.
        /// </value>
        public ICompositionElement Element
        {
            get { return _element; }
        }

        /// <summary>
        ///     Gets the message that describes the composition error.
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> containing a message that describes the
        ///     <see cref="CompositionError"/>.
        /// </value>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        ///     Gets the exception that is the underlying cause of the composition error.
        /// </summary>
        /// <value>
        ///     The <see cref="Exception"/> that is the underlying cause of the 
        ///     <see cref="CompositionError"/>. The default is <see langword="null"/>.
        /// </value>
        public Exception Exception
        {
            get { return _exception; }
        }

        internal CompositionErrorId Id
        {
            get { return _id; }
        }

        internal Exception InnerException
        {
            get { return Exception; }
        }

        /// <summary>
        ///     Returns a string representation of the composition error.
        /// </summary>
        /// <returns>
        ///     A <see cref="String"/> containing the <see cref="Description"/> property.
        /// </returns>
        public override string ToString()
        {
            return Description;
        }

        internal static CompositionError Create(CompositionErrorId id, string format, params object[] parameters)
        {
            return Create(id, (ICompositionElement)null, (Exception)null, format, parameters);
        }

        internal static CompositionError Create(CompositionErrorId id, ICompositionElement element, string format, params object[] parameters)
        {
            return Create(id, element, (Exception)null, format, parameters);
        }

        internal static CompositionError Create(CompositionErrorId id, ICompositionElement element, Exception exception, string format, params object[] parameters)
        {
            return new CompositionError(id, string.Format(CultureInfo.CurrentCulture, format, parameters), element, exception);
        }
    }
}
