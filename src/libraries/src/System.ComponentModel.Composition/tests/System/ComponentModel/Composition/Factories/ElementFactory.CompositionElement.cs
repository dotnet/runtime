// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    partial class ElementFactory
    {
        private class CompositionElement : ICompositionElement
        {
            private readonly string _displayName;
            private readonly ICompositionElement _origin;

            public CompositionElement(string displayName, ICompositionElement origin)
            {
                _displayName = displayName;
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
        }
    }
}
