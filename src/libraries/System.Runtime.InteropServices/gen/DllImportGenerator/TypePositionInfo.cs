using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            this.UnmanagedIndex = UnsetIndex;
            this.UnmanagedLCIDConversionArgIndex = UnsetIndex;
        }

        public ITypeSymbol TypeSymbol { get; private set; }
        public string InstanceIdentifier { get; private set; }

        public RefKind RefKind { get; private set; }
        public string RefKindDecl { get => RefKindToString(this.RefKind); }
        public string ManagedTypeDecl { get; private set; }
        public string UnmanagedTypeDecl { get; private set; }

        public bool IsManagedReturnPosition { get => this.ManagedIndex == ReturnIndex; }
        public bool IsUnmanagedReturnPosition { get => this.UnmanagedIndex == ReturnIndex; }

        public int ManagedIndex { get; set; }
        public int UnmanagedIndex { get; set; }
        public int UnmanagedLCIDConversionArgIndex { get; private set; }

        public MarshalAsInfo MarshalAsInfo { get; private set; }

        public static TypePositionInfo CreateForParameter(IParameterSymbol paramSymbol)
        {
            var typeInfo = new TypePositionInfo()
            {
                TypeSymbol = paramSymbol.Type,
                InstanceIdentifier = paramSymbol.Name,
                ManagedTypeDecl = ComputeTypeForManaged(paramSymbol.Type, paramSymbol.RefKind),
                UnmanagedTypeDecl = ComputeTypeForUnmanaged(paramSymbol.Type, paramSymbol.RefKind),
                RefKind = paramSymbol.RefKind
            };

            UpdateWithAttributeData(paramSymbol.GetAttributes(), ref typeInfo);

            return typeInfo;
        }

        public static TypePositionInfo CreateForType(ITypeSymbol type, IEnumerable<AttributeData> attributes)
        {
            var typeInfo = new TypePositionInfo()
            {
                TypeSymbol = type,
                InstanceIdentifier = string.Empty,
                ManagedTypeDecl = ComputeTypeForManaged(type, RefKind.None),
                UnmanagedTypeDecl = ComputeTypeForUnmanaged(type, RefKind.None),
                RefKind = RefKind.None
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

        private static string ComputeTypeForManaged(ITypeSymbol type, RefKind refKind)
        {
            var typeAsString = type.SpecialType switch
            {
                SpecialType.System_Void => "void",
                SpecialType.System_Boolean => "bool",
                SpecialType.System_Char => "char",
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Byte => "byte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                SpecialType.System_Single => "float",
                SpecialType.System_Double => "double",
                SpecialType.System_String => "string",
                SpecialType.System_IntPtr => "System.IntPtr",
                SpecialType.System_UIntPtr => "System.UIntPtr",
                _ => null,
            };

            var typePrefix = string.Empty;
            if (typeAsString is null)
            {
                // Determine the namespace
                if (!(type.ContainingNamespace is null)
                    && !type.ContainingNamespace.IsGlobalNamespace)
                {
                    typePrefix = $"{type.ContainingNamespace}{Type.Delimiter}";
                }

                typeAsString = type.ToString();
            }

            string refKindAsString = RefKindToString(refKind);
            return $"{refKindAsString}{typePrefix}{typeAsString}";
        }

        private static string ComputeTypeForUnmanaged(ITypeSymbol type, RefKind refKind)
        {
#if GENERATE_FORWARDER
            return ComputeTypeForManaged(type, refKind);
#else
            if (!type.IsUnmanagedType)
            {
                return "void*";
            }

            return type.SpecialType switch
            {
                SpecialType.System_Void => "void",
                SpecialType.System_Boolean => "byte", // [TODO] Determine marshalling default C++ bool or Windows' BOOL
                SpecialType.System_Char => "ushort", // CLR character width (UTF-16)
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Byte => "byte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                SpecialType.System_Single => "float",
                SpecialType.System_Double => "double",
                SpecialType.System_String => "char*", // [TODO] Consider encoding here
                SpecialType.System_IntPtr => "void*",
                SpecialType.System_UIntPtr => "void*",
                _ => "void*",
            };
#endif
        }

        private static string RefKindToString(RefKind refKind)
        {
            return refKind switch
            {
                RefKind.In => "in ",
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.None => string.Empty,
                _ => throw new NotImplementedException("Support for some RefKind"),
            };
        }
    }
}
