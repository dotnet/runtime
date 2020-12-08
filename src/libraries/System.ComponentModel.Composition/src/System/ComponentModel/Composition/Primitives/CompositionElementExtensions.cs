// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Composition.Primitives
{
    internal static class CompositionElementExtensions
    {
        public static ICompositionElement? ToSerializableElement(this ICompositionElement? element)
        {
            return SerializableCompositionElement.FromICompositionElement(element);
        }

        public static ICompositionElement ToElement(this Export export)
        {
            // First try the export
            if (export is ICompositionElement element)
            {
                return element;
            }

            // Otherwise, try the definition
            return ToElement(export.Definition);
        }

        public static ICompositionElement ToElement(this ExportDefinition definition)
        {
            return ToElementCore(definition);
        }

        public static ICompositionElement ToElement(this ImportDefinition definition)
        {
            return ToElementCore(definition);
        }

        public static ICompositionElement ToElement(this ComposablePart part)
        {
            return ToElementCore(part);
        }

        public static ICompositionElement ToElement(this ComposablePartDefinition definition)
        {
            return ToElementCore(definition);
        }

        public static string GetDisplayName(this ComposablePartDefinition definition)
        {
            return GetDisplayNameCore(definition);
        }

        public static string GetDisplayName(this ComposablePartCatalog catalog)
        {
            return GetDisplayNameCore(catalog);
        }

        private static string GetDisplayNameCore(object value)
        {
            if (value is ICompositionElement element)
            {
                return element.DisplayName;
            }

            return value.ToString()!;
        }

        private static ICompositionElement ToElementCore(object value)
        {
            if (value is ICompositionElement element)
            {
                return element;
            }

            return new CompositionElement(value);
        }
    }
}
