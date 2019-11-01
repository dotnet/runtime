// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    internal static partial class ElementFactory
    {
        public static ICompositionElement Create()
        {
            return Create("Unknown Display Name", (ICompositionElement)null);
        }

        public static ICompositionElement Create(string displayName)
        {
            return Create(displayName, (ICompositionElement)null);
        }

        public static ICompositionElement Create(ICompositionElement origin)
        {
            return Create("Unknown Display Name", origin);
        }

        public static ICompositionElement Create(string displayName, ICompositionElement origin)
        {
            return new CompositionElement(displayName, origin);
        }

        public static ICompositionElement CreateChain(int count)
        {
            ICompositionElement previousElement = null;

            for (int i = 0; i < count; i++)
            {
                previousElement = Create((count - i).ToString(), previousElement);
            }

            return previousElement;
        }
    }
}
