// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
    internal partial struct ParameterProxy
    {
        public ParameterProxy(IParameterSymbol parameter, IMethodSymbol method)
        {
            Method = new(method);
            Index = (ParameterIndex)parameter.Ordinal +
                (Method.HasImplicitThis() ? 1 : 0) +
                (method.HasExtensionParameterOnType() && parameter.ContainingSymbol is IMethodSymbol ? 1 : 0);
        }

        public partial ReferenceKind GetReferenceKind()
        {
            if (IsImplicitThis)
                return Method.Method.ContainingType.IsValueType
                    ? ReferenceKind.Ref
                    : ReferenceKind.None;

            switch (ParameterSymbol!.RefKind)
            {
                case RefKind.Ref: return ReferenceKind.Ref;
                case RefKind.In: return ReferenceKind.In;
                case RefKind.Out: return ReferenceKind.Out;
                case RefKind.None: return ReferenceKind.None;
                default:
                    Debug.Fail($"Unexpected RefKind {ParameterSymbol!.RefKind} found on parameter {GetDisplayName()}");
                    return ReferenceKind.None;
            }
        }

        /// <summary>
        /// Returns the IParameterSymbol representing the parameter. Returns null for the implicit this paramter.
        /// </summary>
        public IParameterSymbol? ParameterSymbol
        {
            get
            {
                if (IsImplicitThis)
                {
                    return null;
                }

                var method = Method.Method;
                int hasExtensionParameter = 0;
                if (method.HasExtensionParameterOnType())
                {
                    if (MetadataIndex == 0)
                    {
                        return method.ContainingType.ExtensionParameter;
                    }
                    hasExtensionParameter = 1;
                }

                return method.Parameters[MetadataIndex - hasExtensionParameter];
            }
        }

        /// <summary>
        /// Returns the IParameterSymbol.Location[0] for the parameter. Returns null for the implicit this paramter.
        /// </summary>
        public Location? Location => ParameterSymbol?.Locations[0];

        public TypeProxy ParameterType => new TypeProxy(ParameterSymbol?.Type ?? Method.Method.ContainingType);

        public partial string GetDisplayName()
        {
            if (IsImplicitThis)
                return "this";
            return ParameterSymbol!.GetDisplayName();
        }

        public partial bool IsTypeOf(string typeName) => ParameterType.IsTypeOf(typeName.Substring(0, typeName.LastIndexOf('.')), typeName.Substring(1 + typeName.LastIndexOf('.')));

        public bool IsTypeOf(WellKnownType type) => ParameterType.IsTypeOf(type);
    }
}
