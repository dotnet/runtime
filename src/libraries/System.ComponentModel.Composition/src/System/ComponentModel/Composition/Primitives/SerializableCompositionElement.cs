// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Primitives
{
    // As most objects that implement ICompositionElement (such as Export, ComposablePart, 
    // ComposablePartCatalog, etc) are not serializable, this class is used as a serializable 
    // placeholder for these types when ICompositionElement is used within serializable types,
    // such as CompositionException, CompositionIssue, etc.
    internal class SerializableCompositionElement : ICompositionElement
    {
        private readonly string _displayName;
        private readonly ICompositionElement _origin;

        public SerializableCompositionElement(string displayName, ICompositionElement origin)
        {
#if FEATURE_SERIALIZATION
            Assumes.IsTrue(origin == null || origin.GetType().IsSerializable);
#endif
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

        public static ICompositionElement FromICompositionElement(ICompositionElement element)
        {
            if (element == null)
            {   // Null is always serializable   

                return null;
            }

            ICompositionElement origin = FromICompositionElement(element.Origin);

            // Otherwise, we need to create a serializable wrapper
            return new SerializableCompositionElement(element.DisplayName, origin);
        }
    }
}
