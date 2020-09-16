using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Collected MarshalAsAttribute info.
    /// </summary>
    internal sealed class MarshalAsInfo
    {
        public UnmanagedType UnmanagedType { get; set; }
        public string CustomMarshallerTypeName { get; set; }
        public string CustomMarshallerCookie { get; set; }

        public UnmanagedType UnmanagedArraySubType { get; set; }
        public int ArraySizeConst { get; set; }
        public short ArraySizeParamIndex { get; set; }
    }

    /// <summary>
    /// Positional type information involved in unmanaged/managed scenarios.
    /// </summary>
    internal sealed class TypePositionInfo
    {
        public const int UnsetIndex = int.MinValue;
        public const int ReturnIndex = UnsetIndex + 1;

        private TypePositionInfo()
        {
            this.ManagedIndex = UnsetIndex;
            this.NativeIndex = UnsetIndex;
            this.UnmanagedLCIDConversionArgIndex = UnsetIndex;
        }

        public string InstanceIdentifier { get; private set; }
        public ITypeSymbol ManagedType { get; private set; }

        public RefKind RefKind { get; private set; }
        public SyntaxKind RefKindSyntax { get; private set; }
        
        public bool IsByRef => RefKind == RefKind.Ref || RefKind == RefKind.Out;

        public bool IsManagedReturnPosition { get => this.ManagedIndex == ReturnIndex; }
        public bool IsNativeReturnPosition { get => this.NativeIndex == ReturnIndex; }

        public int ManagedIndex { get; set; }
        public int NativeIndex { get; set; }
        public int UnmanagedLCIDConversionArgIndex { get; private set; }

        public MarshalAsInfo MarshalAsInfo { get; private set; }

        public static TypePositionInfo CreateForParameter(IParameterSymbol paramSymbol, Compilation compilation)
        {
            var typeInfo = new TypePositionInfo()
            {
                ManagedType = paramSymbol.Type,
                InstanceIdentifier = paramSymbol.Name,
                RefKind = paramSymbol.RefKind,
                RefKindSyntax = RefKindToSyntax(paramSymbol.RefKind)
            };

            UpdateWithAttributeData(paramSymbol.GetAttributes(), ref typeInfo);

            return typeInfo;
        }

        public static TypePositionInfo CreateForType(ITypeSymbol type, IEnumerable<AttributeData> attributes, Compilation compilation)
        {
            var typeInfo = new TypePositionInfo()
            {
                ManagedType = type,
                InstanceIdentifier = string.Empty,
                RefKind = RefKind.None,
                RefKindSyntax = SyntaxKind.None,
            };

            UpdateWithAttributeData(attributes, ref typeInfo);

            return typeInfo;
        }

        private static void UpdateWithAttributeData(IEnumerable<AttributeData> attributes, ref TypePositionInfo typeInfo)
        {
            // Look at attributes on the type.
            foreach (var attrData in attributes)
            {
                string attributeName = attrData.AttributeClass.Name;

                if (nameof(MarshalAsAttribute).Equals(attributeName))
                {
                    // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute
                    typeInfo.MarshalAsInfo = CreateMarshalAsInfo(attrData);
                }
                else if (nameof(LCIDConversionAttribute).Equals(attributeName))
                {
                    // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.lcidconversionattribute
                    typeInfo.UnmanagedLCIDConversionArgIndex = (int)attrData.ConstructorArguments[0].Value;
                }
            }

            static MarshalAsInfo CreateMarshalAsInfo(AttributeData attrData)
            {
                var info = new MarshalAsInfo
                {
                    UnmanagedType = (UnmanagedType)attrData.ConstructorArguments[0].Value
                };

                // All other data on attribute is defined as NamedArguments.
                foreach (var namedArg in attrData.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        default:
                            Debug.Fail($"An unknown member was found on {nameof(MarshalAsAttribute)}");
                            continue;
                        case nameof(MarshalAsAttribute.SafeArraySubType):
                        case nameof(MarshalAsAttribute.SafeArrayUserDefinedSubType):
                        case nameof(MarshalAsAttribute.IidParameterIndex):
                            // [TODO] Report not supported
                            break;
                        case nameof(MarshalAsAttribute.MarshalTypeRef):
                        case nameof(MarshalAsAttribute.MarshalType):
                            // Call ToString() to handle INamedTypeSymbol as well.
                            info.CustomMarshallerTypeName = namedArg.Value.Value.ToString();
                            break;
                        case nameof(MarshalAsAttribute.MarshalCookie):
                            info.CustomMarshallerCookie = (string)namedArg.Value.Value;
                            break;
                        case nameof(MarshalAsAttribute.ArraySubType):
                            info.UnmanagedArraySubType = (UnmanagedType)namedArg.Value.Value;
                            break;
                        case nameof(MarshalAsAttribute.SizeConst):
                            info.ArraySizeConst = (int)namedArg.Value.Value;
                            break;
                        case nameof(MarshalAsAttribute.SizeParamIndex):
                            info.ArraySizeParamIndex = (short)namedArg.Value.Value;
                            break;
                    }
                }

                return info;
            }
        }

        private static SyntaxKind RefKindToSyntax(RefKind refKind)
        {
            return refKind switch
            {
                RefKind.In => SyntaxKind.InKeyword,
                RefKind.Ref => SyntaxKind.RefKeyword,
                RefKind.Out => SyntaxKind.OutKeyword,
                RefKind.None => SyntaxKind.None,
                _ => throw new NotImplementedException("Support for some RefKind"),
            };
        }
    }
}
