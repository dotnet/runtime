// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.ComponentModel.Composition.Primitives
{
    [Serializable]
    internal class SerializableCompositionElement : ICompositionElement
    {
        private readonly string _displayName;
        private readonly ICompositionElement _origin;

        public SerializableCompositionElement(string displayName, ICompositionElement origin)
        {
            _displayName = displayName;
            _origin = origin;
        }

        public override string ToString() => _displayName;

        public string DisplayName => _displayName;

        public ICompositionElement Origin => _origin;

        public static ICompositionElement FromICompositionElement(ICompositionElement element)
        {
            if (element == null)
            {
                return null;
            }

            ICompositionElement origin = FromICompositionElement(element.Origin);

            // Otherwise, we need to create a serializable wrapper
            return new SerializableCompositionElement(element.DisplayName, origin);
        }
    }
}
