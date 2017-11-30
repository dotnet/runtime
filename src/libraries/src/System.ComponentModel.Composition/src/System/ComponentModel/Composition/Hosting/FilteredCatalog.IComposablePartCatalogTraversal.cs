// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class FilteredCatalog
    {
        /// <summary>
        /// This is designed to traverse a set of parts based on whatever pattern. There are no real expectations
        /// as to what the pattern is and what properties is posseses
        /// NOTE : we both with this interface - as opposed to just a simple delegate - due to minute performance reasons, 
        /// as this will be invoked very often. Also, each traversal is typically associated with a big state bag, which is
        /// easier to associte with an explicit implementation as opposed to an implicit closure.
        /// </summary>
        internal interface IComposablePartCatalogTraversal
        {
            void Initialize();
            bool TryTraverse(ComposablePartDefinition part, out IEnumerable<ComposablePartDefinition> reachableParts);
        }
    }
}
