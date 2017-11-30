// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace System.ComponentModel.Composition.Factories
{
    partial class CatalogFactory
    {
        public class MutableComposablePartCatalog : ComposablePartCatalog, INotifyComposablePartCatalogChanged
        {
            private readonly HashSet<ComposablePartDefinition> _definitions;

            public MutableComposablePartCatalog(IEnumerable<ComposablePartDefinition> definitions)
            {
                _definitions = new HashSet<ComposablePartDefinition>(definitions);
            }

            public void AddDefinition(ComposablePartDefinition definition)
            {
                OnDefinitionsChanged(definition, true);
            }

            public void RemoveDefinition(ComposablePartDefinition definition)
            {
                OnDefinitionsChanged(definition, false);
            }

            public override IQueryable<ComposablePartDefinition> Parts
            {
                get { return _definitions.AsQueryable(); }
            }

            private void OnDefinitionsChanged(ComposablePartDefinition definition, bool added)
            {
                ComposablePartDefinition[] addedDefinitions = added ? new ComposablePartDefinition[] { definition } : new ComposablePartDefinition[0];
                ComposablePartDefinition[] removeDefinitions = added ? new ComposablePartDefinition[0] : new ComposablePartDefinition[] { definition };

                var e = new ComposablePartCatalogChangeEventArgs(addedDefinitions, removeDefinitions, null);
                Changing(this, e);

                if (added)
                {
                    _definitions.Add(definition);
                }
                else
                {
                    _definitions.Remove(definition);
                }

                if (Changed != null)
                {
                    Changed(this, e);
                }
            }

            public event EventHandler<ComposablePartCatalogChangeEventArgs> Changed;

            public event EventHandler<ComposablePartCatalogChangeEventArgs> Changing;
        }
    }
}
