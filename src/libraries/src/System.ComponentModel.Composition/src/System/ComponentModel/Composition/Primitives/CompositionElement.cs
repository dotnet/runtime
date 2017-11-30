// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.ComponentModel.Composition.Primitives
{
    // Represents the ICompositionElement placeholder for an 
    // object that does not implement ICompositionElement
    [DebuggerTypeProxy(typeof(CompositionElementDebuggerProxy))]
    internal class CompositionElement : ICompositionElement
    {
        private readonly string _displayName;
        private readonly ICompositionElement _origin;
        private readonly object _underlyingObject;
        private static readonly ICompositionElement UnknownOrigin = new CompositionElement(SR.CompositionElement_UnknownOrigin, (ICompositionElement)null);

        public CompositionElement(object underlyingObject)
            : this(underlyingObject.ToString(), UnknownOrigin)
        {
            _underlyingObject = underlyingObject;
        }

        public CompositionElement(string displayName, ICompositionElement origin)
        {
            _displayName = displayName ?? string.Empty;
            _origin = origin;
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public ICompositionElement Origin
        {
            get { return _origin; }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public object UnderlyingObject
        {
            get { return _underlyingObject; }
        }
    }
}
