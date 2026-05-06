// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Internal;

namespace System.ComponentModel.Composition.Primitives
{
    // Because the debugger displays only the members available on ICompositionElement
    // when viewing CompositionError.Element in the watch and data tips windows, we
    // need this proxy so that the underlying object wrapped by the CompositionElement
    // placeholder is displayed by default.
    internal sealed class CompositionElementDebuggerProxy
    {
        private readonly CompositionElement _element;

        public CompositionElementDebuggerProxy(CompositionElement element)
        {
            Requires.NotNull(element, nameof(element));

            _element = element;
        }

        public string DisplayName
        {
            get { return _element.DisplayName; }
        }

        public ICompositionElement? Origin
        {
            get { return _element.Origin; }
        }

        public object? UnderlyingObject
        {
            get { return _element.UnderlyingObject; }
        }
    }
}
