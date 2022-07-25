// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Internal.TypeSystem
{
    // Additional extensions to MethodDesc related to interop
    partial class MethodDesc
    {
        /// <summary>
        /// Gets a value indicating whether this method is a (native unmanaged) platform invoke.
        /// Use <see cref="GetPInvokeMethodMetadata"/> to retrieve the platform invoke detail information.
        /// </summary>
        public virtual bool IsPInvoke
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// If <see cref="IsPInvoke"/> is true, retrieves the metadata related to the platform invoke.
        /// </summary>
        public virtual PInvokeMetadata GetPInvokeMethodMetadata()
        {
            return default(PInvokeMetadata);
        }

        /// <summary>
        /// Retrieves the metadata related to the parameters of the method.
        /// </summary>
        public virtual ParameterMetadata[] GetParameterMetadata()
        {
            return Array.Empty<ParameterMetadata>();
        }
    }

    [Flags]
    public enum ParameterMetadataAttributes
    {
        None = 0,
        In = 1,
        Out = 2,
        Optional = 16,
        HasDefault = 4096,
        HasFieldMarshal = 8192
    }

    public struct ParameterMetadata
    {
        private  readonly ParameterMetadataAttributes _attributes;
        public readonly MarshalAsDescriptor MarshalAsDescriptor;

        /// <summary>
        /// Gets a 1-based index of the parameter within the signature the metadata refers to.
        /// Index 0 is the return value.
        /// </summary>
        public readonly int Index;

        public bool In { get { return (_attributes & ParameterMetadataAttributes.In) == ParameterMetadataAttributes.In; } }
        public bool Out { get { return (_attributes & ParameterMetadataAttributes.Out) == ParameterMetadataAttributes.Out; } }
        public bool Return { get { return Index == 0;  } }
        public bool Optional { get { return (_attributes & ParameterMetadataAttributes.Optional) == ParameterMetadataAttributes.Optional;  } }
        public bool HasDefault { get { return (_attributes & ParameterMetadataAttributes.HasDefault) == ParameterMetadataAttributes.HasDefault; } }
        public bool HasFieldMarshal { get { return (_attributes & ParameterMetadataAttributes.HasFieldMarshal) == ParameterMetadataAttributes.HasFieldMarshal; } }


        public ParameterMetadata(int index, ParameterMetadataAttributes attributes, MarshalAsDescriptor marshalAsDescriptor)
        {
            Index = index;
            _attributes = attributes;
            MarshalAsDescriptor = marshalAsDescriptor;
        }
    }

    [Flags]
    public enum PInvokeAttributes
    {
        // These should match System.Reflection.MethodImportAttributes
        None = 0,
        ExactSpelling = 1,
        CharSetAnsi = 2,
        CharSetUnicode = 4,
        CharSetAuto = 6,
        CharSetMask = 6,
        BestFitMappingEnable = 16,
        BestFitMappingDisable = 32,
        BestFitMappingMask = 48,
        SetLastError = 64,
        CallingConventionWinApi = 256,
        CallingConventionCDecl = 512,
        CallingConventionStdCall = 768,
        CallingConventionThisCall = 1024,
        CallingConventionFastCall = 1280,
        CallingConventionMask = 1792,
        ThrowOnUnmappableCharEnable = 4096,
        ThrowOnUnmappableCharDisable = 8192,
        ThrowOnUnmappableCharMask = 12288,

        // Not actually part of MethodImportAttributes.
        // MethodImportAttributes is limited to `short`. This enum is based on int
        // and we have 16 spare bytes.
        PreserveSig = 0x10000,
    }

    public struct PInvokeFlags : IEquatable<PInvokeFlags>, IComparable<PInvokeFlags>
    {
        private PInvokeAttributes _attributes;
        public PInvokeAttributes Attributes
        {
            get
            {
                return _attributes;
            }
        }

        public PInvokeFlags(PInvokeAttributes attributes)
        {
            _attributes = attributes;
        }

        public CharSet CharSet
        {
            get
            {
                return (_attributes & PInvokeAttributes.CharSetMask) switch
                {
                    PInvokeAttributes.CharSetAnsi => CharSet.Ansi,
                    PInvokeAttributes.CharSetUnicode => CharSet.Unicode,
                    PInvokeAttributes.CharSetAuto => CharSet.Auto,
                    _ => CharSet.None
                };
            }

            set
            {
                // clear the charset bits;
                _attributes &= ~(PInvokeAttributes.CharSetMask);

                _attributes |= value switch
                {
                    CharSet.None => PInvokeAttributes.None,
                    CharSet.Ansi => PInvokeAttributes.CharSetAnsi,
                    CharSet.Unicode => PInvokeAttributes.CharSetUnicode,
                    CharSet.Auto => PInvokeAttributes.CharSetAuto,
                    (CharSet)0 => PInvokeAttributes.None,
                    _ => throw new BadImageFormatException()
                };
            }
        }

        public MethodSignatureFlags UnmanagedCallingConvention
        {
            get
            {
                switch (_attributes & PInvokeAttributes.CallingConventionMask)
                {
                    case PInvokeAttributes.CallingConventionCDecl:
                        return MethodSignatureFlags.UnmanagedCallingConventionCdecl;
                    case PInvokeAttributes.CallingConventionStdCall:
                        return MethodSignatureFlags.UnmanagedCallingConventionStdCall;
                    case PInvokeAttributes.CallingConventionThisCall:
                        return MethodSignatureFlags.UnmanagedCallingConventionThisCall;
                    case PInvokeAttributes.CallingConventionWinApi: // Platform default
                    case PInvokeAttributes.None:
                        return MethodSignatureFlags.None;
                    default:
                        ThrowHelper.ThrowBadImageFormatException();
                        return MethodSignatureFlags.None; // unreachable
                }
            }
            set
            {
                _attributes &= ~(PInvokeAttributes.CallingConventionMask);
                switch (value)
                {

                    case MethodSignatureFlags.UnmanagedCallingConventionStdCall:
                        _attributes |= PInvokeAttributes.CallingConventionStdCall;
                        break;
                    case MethodSignatureFlags.UnmanagedCallingConventionCdecl:
                        _attributes |= PInvokeAttributes.CallingConventionCDecl;
                        break;
                    case MethodSignatureFlags.UnmanagedCallingConventionThisCall:
                        _attributes |= PInvokeAttributes.CallingConventionThisCall;
                        break;
                    default:
                        System.Diagnostics.Debug.Fail("Unexpected Unmanaged Calling Convention.");
                        break;
                }
            }
        }

        public bool SetLastError
        {
            get
            {
                return (_attributes & PInvokeAttributes.SetLastError) == PInvokeAttributes.SetLastError;
            }
            set
            {
                if (value)
                {
                    _attributes |= PInvokeAttributes.SetLastError;
                }
                else
                {
                    _attributes &= ~(PInvokeAttributes.SetLastError);
                }
            }
        }

        public bool ExactSpelling
        {
            get
            {
                return (_attributes & PInvokeAttributes.ExactSpelling) == PInvokeAttributes.ExactSpelling;
            }
            set
            {
                if (value)
                {
                    _attributes |= PInvokeAttributes.ExactSpelling;
                }
                else
                {
                    _attributes &= ~(PInvokeAttributes.ExactSpelling);
                }
            }
        }

        public bool BestFitMapping
        {
            get
            {
                PInvokeAttributes mask = _attributes & PInvokeAttributes.BestFitMappingMask;
                if (mask == PInvokeAttributes.BestFitMappingDisable)
                {
                    return false;
                }
                // default value is true
                return true;
            }
            set
            {
                _attributes &= ~(PInvokeAttributes.BestFitMappingMask);
                if (value)
                {
                    _attributes |= PInvokeAttributes.BestFitMappingEnable;
                }
                else
                {
                    _attributes |= PInvokeAttributes.BestFitMappingDisable;
                }
            }
        }

        public bool ThrowOnUnmappableChar
        {
            get
            {
                PInvokeAttributes mask = _attributes & PInvokeAttributes.ThrowOnUnmappableCharMask;
                if (mask == PInvokeAttributes.ThrowOnUnmappableCharEnable)
                {
                    return true;
                }
                // default value is false
                return false;
            }
            set
            {
                _attributes &= ~(PInvokeAttributes.ThrowOnUnmappableCharMask);
                if (value)
                {
                    _attributes |= PInvokeAttributes.ThrowOnUnmappableCharEnable;
                }
                else
                {
                    _attributes |= PInvokeAttributes.ThrowOnUnmappableCharDisable;
                }
            }
        }

        public bool PreserveSig
        {
            get
            {
                return (_attributes & PInvokeAttributes.PreserveSig) != 0;
            }
            set
            {
                if (value)
                {
                    _attributes |= PInvokeAttributes.PreserveSig;
                }
                else
                {
                    _attributes &= ~PInvokeAttributes.PreserveSig;
                }
            }
        }

        public int CompareTo(PInvokeFlags other)
        {
            return Attributes.CompareTo(other.Attributes);
        }

        public bool Equals(PInvokeFlags other)
        {
            return Attributes == other.Attributes;
        }

        public override bool Equals(object obj)
        {
            return obj is PInvokeFlags other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Attributes.GetHashCode();
        }
    }

    /// <summary>
    /// Represents details about a pinvokeimpl method import.
    /// </summary>
    public readonly struct PInvokeMetadata
    {
        public readonly string Name;

        public readonly string Module;

        public readonly PInvokeFlags Flags;

        public PInvokeMetadata(string module, string entrypoint, PInvokeAttributes attributes)
        {
            Name = entrypoint;
            Module = module;
            Flags = new PInvokeFlags(attributes);
        }

        public PInvokeMetadata(string module, string entrypoint, PInvokeFlags flags)
        {
            Name = entrypoint;
            Module = module;
            Flags = flags;
        }
    }

    partial class InstantiatedMethod
    {
        public override ParameterMetadata[] GetParameterMetadata()
        {
            return _methodDef.GetParameterMetadata();
        }
    }

    partial class MethodForInstantiatedType
    {
        public override ParameterMetadata[] GetParameterMetadata()
        {
            return _typicalMethodDef.GetParameterMetadata();
        }
    }
}
