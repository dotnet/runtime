// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal static class SignatureBindingHelpers
    {
        public static string CreateSignaturesArgument(ImmutableArray<TypePositionInfo> elements, StubCodeContext context)
        {
            var writer = new IndentedTextWriter();
            writer.Write('[');
            bool first = true;
            foreach (TypePositionInfo element in elements.Where(static element => element.NativeIndex != TypePositionInfo.UnsetIndex).OrderBy(static element => element.NativeIndex))
            {
                if (!first)
                {
                    writer.Write(", ");
                }
                first = false;

                var (baseType, subTypes) = JSGeneratorResolver.GetMarshallerTypeForBinding(element, context);
                writer.Write(MarshalerTypeName(baseType));
                if (subTypes is not null)
                {
                    writer.Write('(');
                    writer.Write(string.Join(", ", subTypes.Select(MarshalerTypeName)));
                    writer.Write(')');
                }
            }
            writer.Write(']');
            return writer.ToString();
        }

        private static string MarshalerTypeName(MarshalerType marshalerType)
        {
            return Constants.JSMarshalerTypeGlobalDot + marshalerType;
        }
    }
}
