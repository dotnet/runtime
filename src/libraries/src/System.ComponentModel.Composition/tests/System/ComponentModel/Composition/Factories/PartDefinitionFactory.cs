// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.AttributedModel;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;

namespace System.ComponentModel.Composition.Factories
{
    internal static partial class PartDefinitionFactory
    {
        public static ComposablePartDefinition AsPart(this Type type)
        {
            return CreateAttributed(type);
        }

        public static ReflectionComposablePartDefinition CreateAttributed()
        {
            return CreateAttributed(typeof(ComposablePart));
        }

        public static ReflectionComposablePartDefinition CreateAttributed(Type type)
        {
            return AttributedModelDiscovery.CreatePartDefinition(type, null, false, (ICompositionElement)null);
        }

        public static ComposablePartDefinition Create()
        {
            return new NoOverridesComposablePartDefinition();
        }

        public static ComposablePartDefinition Create(ComposablePart part)
        {
            return Create(part.Metadata, () => part, part.ImportDefinitions, part.ExportDefinitions);
        }

        public static ComposablePartDefinition Create(IDictionary<string, object> metadata,
                                              Func<ComposablePart> partCreator,
                                              IEnumerable<ImportDefinition> imports,
                                              IEnumerable<ExportDefinition> exports)
        {
            return Create(metadata, partCreator, () => imports, () => exports);
        }      

        public static ComposablePartDefinition Create(IDictionary<string, object> metadata,
                                                      Func<ComposablePart> partCreator,
                                                      Func<IEnumerable<ImportDefinition>> importsCreator,
                                                      Func<IEnumerable<ExportDefinition>> exportsCreator)
        {
            return new DerivedComposablePartDefinition(metadata, partCreator, importsCreator, exportsCreator);
        }        
    }
}
