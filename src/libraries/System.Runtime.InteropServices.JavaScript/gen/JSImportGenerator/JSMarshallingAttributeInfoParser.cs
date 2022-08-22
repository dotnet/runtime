// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.JavaScript
{
    public sealed class JSMarshallingAttributeInfoParser
    {
        private readonly ITypeSymbol _jsMarshalAsAttribute;
        private readonly ITypeSymbol _marshalUsingAttribute;

        public JSMarshallingAttributeInfoParser(
            Compilation compilation,
            IGeneratorDiagnostics diagnostics,
            ISymbol contextSymbol)
        {
            _jsMarshalAsAttribute = compilation.GetTypeByMetadataName(Constants.JSMarshalAsAttribute)!.ConstructUnboundGenericType();
            _marshalUsingAttribute = compilation.GetTypeByMetadataName(Constants.MarshalUsingAttribute)!;
        }

        public MarshallingInfo ParseMarshallingInfo(
            ITypeSymbol managedType,
            IEnumerable<AttributeData> useSiteAttributes,
            MarshallingInfo inner)
        {
            JSTypeFlags jsType = JSTypeFlags.None;
            List<JSTypeFlags> jsTypeArguments = new List<JSTypeFlags>();

            foreach (AttributeData useSiteAttribute in useSiteAttributes)
            {
                INamedTypeSymbol attributeClass = useSiteAttribute.AttributeClass!;
                if (attributeClass.IsGenericType && SymbolEqualityComparer.Default.Equals(_jsMarshalAsAttribute, attributeClass.ConstructUnboundGenericType()))
                {
                    INamedTypeSymbol? jsTypeArgs = attributeClass.TypeArguments[0] as INamedTypeSymbol;
                    if (jsTypeArgs.IsGenericType)
                    {
                        string gt = jsTypeArgs.ConstructUnboundGenericType().ToDisplayString();
                        string name = gt.Substring(gt.IndexOf("JSType") + "JSType.".Length);
                        name = name.Substring(0, name.IndexOf("<"));

                        Enum.TryParse(name, out jsType);

                        foreach (var ta in jsTypeArgs.TypeArguments.Cast<INamedTypeSymbol>().Select(x => x.ToDisplayString()))
                        {
                            string argName = ta.Substring(ta.IndexOf("JSType") + "JSType.".Length);
                            JSTypeFlags jsTypeArg = JSTypeFlags.None;
                            Enum.TryParse(argName, out jsTypeArg);
                            jsTypeArguments.Add(jsTypeArg);
                        }
                    }
                    else
                    {
                        string st = jsTypeArgs.ToDisplayString();
                        string name = st.Substring(st.IndexOf("JSType") + "JSType.".Length);
                        Enum.TryParse(name, out jsType);
                    }

                }
                if (SymbolEqualityComparer.Default.Equals(_marshalUsingAttribute, attributeClass))
                {
                    return new JSMarshallingInfo(inner)
                    {
                        JSType = JSTypeFlags.Array,
                        JSTypeArguments = Array.Empty<JSTypeFlags>(),
                    };
                }
            }

            if (jsType == JSTypeFlags.None)
            {
                return new JSMissingMarshallingInfo();
            }

            return new JSMarshallingInfo(inner)
            {
                JSType = jsType,
                JSTypeArguments = jsTypeArguments.ToArray(),
            };
        }
    }
}
