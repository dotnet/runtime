// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSMarshalAsAttributeParser : IMarshallingInfoAttributeParser, IUseSiteAttributeParser
    {
        private readonly INamedTypeSymbol _jsMarshalAsAttribute;

        public JSMarshalAsAttributeParser(Compilation compilation)
        {
            _jsMarshalAsAttribute = compilation.GetTypeByMetadataName(Constants.JSMarshalAsAttribute)!.ConstructUnboundGenericType();
        }
        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.IsGenericType && SymbolEqualityComparer.Default.Equals(_jsMarshalAsAttribute, attributeType.ConstructUnboundGenericType());
        public MarshallingInfo ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            JSTypeFlags jsType = JSTypeFlags.None;
            List<JSTypeFlags> jsTypeArguments = new List<JSTypeFlags>();
            INamedTypeSymbol? jsTypeArgs = attributeData.AttributeClass.TypeArguments[0] as INamedTypeSymbol;
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

            if (jsType == JSTypeFlags.None)
            {
                return new JSMissingMarshallingInfo(JSTypeInfo.CreateJSTypeInfoForTypeSymbol(type));
            }

            return new JSMarshallingInfo(NoMarshallingInfo.Instance, JSTypeInfo.CreateJSTypeInfoForTypeSymbol(type))
            {
                JSType = jsType,
                JSTypeArguments = jsTypeArguments.ToArray(),
            };
        }

        UseSiteAttributeData IUseSiteAttributeParser.ParseAttribute(AttributeData attributeData, IElementInfoProvider elementInfoProvider, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            return new UseSiteAttributeData(0, NoCountInfo.Instance, attributeData);
        }
    }
}
