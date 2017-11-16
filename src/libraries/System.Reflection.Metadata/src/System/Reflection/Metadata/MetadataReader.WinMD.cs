// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    partial class MetadataReader
    {
        internal const string ClrPrefix = "<CLR>";

        internal static readonly byte[] WinRTPrefix = new[] {
            (byte)'<',
            (byte)'W',
            (byte)'i',
            (byte)'n',
            (byte)'R',
            (byte)'T',
            (byte)'>'
        };

        #region Projection Tables

        // Maps names of projected types to projection information for each type.
        // Both arrays are of the same length and sorted by the type name.
        private static string[] s_projectedTypeNames;
        private static ProjectionInfo[] s_projectionInfos;

        private readonly struct ProjectionInfo
        {
            public readonly string WinRTNamespace;
            public readonly StringHandle.VirtualIndex ClrNamespace;
            public readonly StringHandle.VirtualIndex ClrName;
            public readonly AssemblyReferenceHandle.VirtualIndex AssemblyRef;
            public readonly TypeDefTreatment Treatment;
            public readonly TypeRefSignatureTreatment SignatureTreatment;
            public readonly bool IsIDisposable;

            public ProjectionInfo(
                string winRtNamespace,
                StringHandle.VirtualIndex clrNamespace,
                StringHandle.VirtualIndex clrName,
                AssemblyReferenceHandle.VirtualIndex clrAssembly,
                TypeDefTreatment treatment = TypeDefTreatment.RedirectedToClrType,
                TypeRefSignatureTreatment signatureTreatment = TypeRefSignatureTreatment.None,
                bool isIDisposable = false)
            {
                this.WinRTNamespace = winRtNamespace;
                this.ClrNamespace = clrNamespace;
                this.ClrName = clrName;
                this.AssemblyRef = clrAssembly;
                this.Treatment = treatment;
                this.SignatureTreatment = signatureTreatment;
                this.IsIDisposable = isIDisposable;
            }
        }

        private TypeDefTreatment GetWellKnownTypeDefinitionTreatment(TypeDefinitionHandle typeDef)
        {
            InitializeProjectedTypes();

            StringHandle name = TypeDefTable.GetName(typeDef);

            int index = StringHeap.BinarySearchRaw(s_projectedTypeNames, name);
            if (index < 0)
            {
                return TypeDefTreatment.None;
            }

            StringHandle namespaceName = TypeDefTable.GetNamespace(typeDef);
            if (StringHeap.EqualsRaw(namespaceName, StringHeap.GetVirtualString(s_projectionInfos[index].ClrNamespace)))
            {
                return s_projectionInfos[index].Treatment;
            }

            // TODO: we can avoid this comparison if info.DotNetNamespace == info.WinRtNamespace 
            if (StringHeap.EqualsRaw(namespaceName, s_projectionInfos[index].WinRTNamespace))
            {
                return s_projectionInfos[index].Treatment | TypeDefTreatment.MarkInternalFlag;
            }

            return TypeDefTreatment.None;
        }

        private int GetProjectionIndexForTypeReference(TypeReferenceHandle typeRef, out bool isIDisposable)
        {
            InitializeProjectedTypes();

            int index = StringHeap.BinarySearchRaw(s_projectedTypeNames, TypeRefTable.GetName(typeRef));
            if (index >= 0 && StringHeap.EqualsRaw(TypeRefTable.GetNamespace(typeRef), s_projectionInfos[index].WinRTNamespace))
            {
                isIDisposable = s_projectionInfos[index].IsIDisposable;
                return index;
            }

            isIDisposable = false;
            return -1;
        }

        internal static AssemblyReferenceHandle GetProjectedAssemblyRef(int projectionIndex)
        {
            Debug.Assert(s_projectionInfos != null && projectionIndex >= 0 && projectionIndex < s_projectionInfos.Length);
            return AssemblyReferenceHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].AssemblyRef);
        }

        internal static StringHandle GetProjectedName(int projectionIndex)
        {
            Debug.Assert(s_projectionInfos != null && projectionIndex >= 0 && projectionIndex < s_projectionInfos.Length);
            return StringHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].ClrName);
        }

        internal static StringHandle GetProjectedNamespace(int projectionIndex)
        {
            Debug.Assert(s_projectionInfos != null && projectionIndex >= 0 && projectionIndex < s_projectionInfos.Length);
            return StringHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].ClrNamespace);
        }

        internal static TypeRefSignatureTreatment GetProjectedSignatureTreatment(int projectionIndex)
        {
            Debug.Assert(s_projectionInfos != null && projectionIndex >= 0 && projectionIndex < s_projectionInfos.Length);
            return s_projectionInfos[projectionIndex].SignatureTreatment;
        }

        private static void InitializeProjectedTypes()
        {
            if (s_projectedTypeNames == null || s_projectionInfos == null)
            {
                var systemRuntimeWindowsRuntime = AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime;
                var systemRuntime = AssemblyReferenceHandle.VirtualIndex.System_Runtime;
                var systemObjectModel = AssemblyReferenceHandle.VirtualIndex.System_ObjectModel;
                var systemRuntimeWindowsUiXaml = AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml;
                var systemRuntimeInterop = AssemblyReferenceHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime;
                var systemNumericsVectors = AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors;

                // sorted by name
                var keys = new string[50];
                var values = new ProjectionInfo[50];
                int k = 0, v = 0;

                // WARNING: Keys must be sorted by name and must only contain ASCII characters. WinRTNamespace must also be ASCII only.

                keys[k++] = "AttributeTargets"; values[v++] = new ProjectionInfo("Windows.Foundation.Metadata", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.AttributeTargets, systemRuntime);
                keys[k++] = "AttributeUsageAttribute"; values[v++] = new ProjectionInfo("Windows.Foundation.Metadata", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.AttributeUsageAttribute, systemRuntime, treatment: TypeDefTreatment.RedirectedToClrAttribute);
                keys[k++] = "Color"; values[v++] = new ProjectionInfo("Windows.UI", StringHandle.VirtualIndex.Windows_UI, StringHandle.VirtualIndex.Color, systemRuntimeWindowsRuntime);
                keys[k++] = "CornerRadius"; values[v++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.CornerRadius, systemRuntimeWindowsUiXaml);
                keys[k++] = "DateTime"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.DateTimeOffset, systemRuntime);
                keys[k++] = "Duration"; values[v++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.Duration, systemRuntimeWindowsUiXaml);
                keys[k++] = "DurationType"; values[v++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.DurationType, systemRuntimeWindowsUiXaml);
                keys[k++] = "EventHandler`1"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.EventHandler1, systemRuntime);
                keys[k++] = "EventRegistrationToken"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime, StringHandle.VirtualIndex.EventRegistrationToken, systemRuntimeInterop);
                keys[k++] = "GeneratorPosition"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Controls.Primitives", StringHandle.VirtualIndex.Windows_UI_Xaml_Controls_Primitives, StringHandle.VirtualIndex.GeneratorPosition, systemRuntimeWindowsUiXaml);
                keys[k++] = "GridLength"; values[v++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.GridLength, systemRuntimeWindowsUiXaml);
                keys[k++] = "GridUnitType"; values[v++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.GridUnitType, systemRuntimeWindowsUiXaml);
                keys[k++] = "HResult"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Exception, systemRuntime, signatureTreatment: TypeRefSignatureTreatment.ProjectedToClass);
                keys[k++] = "IBindableIterable"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections, StringHandle.VirtualIndex.IEnumerable, systemRuntime);
                keys[k++] = "IBindableVector"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections, StringHandle.VirtualIndex.IList, systemRuntime);
                keys[k++] = "IClosable"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.IDisposable, systemRuntime, isIDisposable: true);
                keys[k++] = "ICommand"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Input", StringHandle.VirtualIndex.System_Windows_Input, StringHandle.VirtualIndex.ICommand, systemObjectModel);
                keys[k++] = "IIterable`1"; values[v++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IEnumerable1, systemRuntime);
                keys[k++] = "IKeyValuePair`2"; values[v++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.KeyValuePair2, systemRuntime, signatureTreatment: TypeRefSignatureTreatment.ProjectedToValueType);
                keys[k++] = "IMapView`2"; values[v++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IReadOnlyDictionary2, systemRuntime);
                keys[k++] = "IMap`2"; values[v++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IDictionary2, systemRuntime);
                keys[k++] = "INotifyCollectionChanged"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.INotifyCollectionChanged, systemObjectModel);
                keys[k++] = "INotifyPropertyChanged"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.INotifyPropertyChanged, systemObjectModel);
                keys[k++] = "IReference`1"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Nullable1, systemRuntime, signatureTreatment: TypeRefSignatureTreatment.ProjectedToValueType);
                keys[k++] = "IVectorView`1"; values[v++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IReadOnlyList1, systemRuntime);
                keys[k++] = "IVector`1"; values[v++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IList1, systemRuntime);
                keys[k++] = "KeyTime"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.KeyTime, systemRuntimeWindowsUiXaml);
                keys[k++] = "Matrix"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Media", StringHandle.VirtualIndex.Windows_UI_Xaml_Media, StringHandle.VirtualIndex.Matrix, systemRuntimeWindowsUiXaml);
                keys[k++] = "Matrix3D"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Media.Media3D", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Media3D, StringHandle.VirtualIndex.Matrix3D, systemRuntimeWindowsUiXaml);
                keys[k++] = "Matrix3x2"; values[v++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Matrix3x2, systemNumericsVectors);
                keys[k++] = "Matrix4x4"; values[v++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Matrix4x4, systemNumericsVectors);
                keys[k++] = "NotifyCollectionChangedAction"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedAction, systemObjectModel);
                keys[k++] = "NotifyCollectionChangedEventArgs"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedEventArgs, systemObjectModel);
                keys[k++] = "NotifyCollectionChangedEventHandler"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedEventHandler, systemObjectModel);
                keys[k++] = "Plane"; values[v++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Plane, systemNumericsVectors);
                keys[k++] = "Point"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Point, systemRuntimeWindowsRuntime);
                keys[k++] = "PropertyChangedEventArgs"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.PropertyChangedEventArgs, systemObjectModel);
                keys[k++] = "PropertyChangedEventHandler"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.PropertyChangedEventHandler, systemObjectModel);
                keys[k++] = "Quaternion"; values[v++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Quaternion, systemNumericsVectors);
                keys[k++] = "Rect"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Rect, systemRuntimeWindowsRuntime);
                keys[k++] = "RepeatBehavior"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.RepeatBehavior, systemRuntimeWindowsUiXaml);
                keys[k++] = "RepeatBehaviorType"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.RepeatBehaviorType, systemRuntimeWindowsUiXaml);
                keys[k++] = "Size"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Size, systemRuntimeWindowsRuntime);
                keys[k++] = "Thickness"; values[v++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.Thickness, systemRuntimeWindowsUiXaml);
                keys[k++] = "TimeSpan"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.TimeSpan, systemRuntime);
                keys[k++] = "TypeName"; values[v++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Type, systemRuntime, signatureTreatment: TypeRefSignatureTreatment.ProjectedToClass);
                keys[k++] = "Uri"; values[v++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Uri, systemRuntime);
                keys[k++] = "Vector2"; values[v++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector2, systemNumericsVectors);
                keys[k++] = "Vector3"; values[v++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector3, systemNumericsVectors);
                keys[k++] = "Vector4"; values[v++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector4, systemNumericsVectors);

                Debug.Assert(k == keys.Length && v == keys.Length && k == v);
                AssertSorted(keys);

                s_projectedTypeNames = keys;
                s_projectionInfos = values;
            }
        }

        [Conditional("DEBUG")]
        private static void AssertSorted(string[] keys)
        {
            for (int i = 0; i < keys.Length - 1; i++)
            {
                Debug.Assert(String.CompareOrdinal(keys[i], keys[i + 1]) < 0);
            }
        }

        // test only
        internal static string[] GetProjectedTypeNames()
        {
            InitializeProjectedTypes();
            return s_projectedTypeNames;
        }

        #endregion

        private static uint TreatmentAndRowId(byte treatment, int rowId)
        {
            return ((uint)treatment << TokenTypeIds.RowIdBitCount) | (uint)rowId;
        }

        #region TypeDef

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal uint CalculateTypeDefTreatmentAndRowId(TypeDefinitionHandle handle)
        {
            Debug.Assert(_metadataKind != MetadataKind.Ecma335);

            TypeDefTreatment treatment;

            TypeAttributes flags = TypeDefTable.GetFlags(handle);
            EntityHandle extends = TypeDefTable.GetExtends(handle);

            if ((flags & TypeAttributes.WindowsRuntime) != 0)
            {
                if (_metadataKind == MetadataKind.WindowsMetadata)
                {
                    treatment = GetWellKnownTypeDefinitionTreatment(handle);
                    if (treatment != TypeDefTreatment.None)
                    {
                        return TreatmentAndRowId((byte)treatment, handle.RowId);
                    }

                    // Is this an attribute?
                    if (extends.Kind == HandleKind.TypeReference && IsSystemAttribute((TypeReferenceHandle)extends))
                    {
                        treatment = TypeDefTreatment.NormalAttribute;
                    }
                    else
                    {
                        treatment = TypeDefTreatment.NormalNonAttribute;
                    }
                }
                else if (_metadataKind == MetadataKind.ManagedWindowsMetadata && NeedsWinRTPrefix(flags, extends))
                {
                    // WinMDExp emits two versions of RuntimeClasses and Enums:
                    //
                    //    public class Foo {}            // the WinRT reference class
                    //    internal class <CLR>Foo {}     // the implementation class that we want WinRT consumers to ignore
                    //
                    // The adapter's job is to undo WinMDExp's transformations. I.e. turn the above into:
                    //
                    //    internal class <WinRT>Foo {}   // the WinRT reference class that we want CLR consumers to ignore
                    //    public class Foo {}            // the implementation class
                    //
                    // We only add the <WinRT> prefix here since the WinRT view is the only view that is marked WindowsRuntime
                    // De-mangling the CLR name is done below.


                    // tomat: The CLR adapter implements a back-compat quirk: Enums exported with an older WinMDExp have only one version
                    // not marked with tdSpecialName. These enums should *not* be mangled and flipped to private.
                    // We don't implement this flag since the WinMDs produced by the older WinMDExp are not used in the wild.

                    treatment = TypeDefTreatment.PrefixWinRTName;
                }
                else
                {
                    treatment = TypeDefTreatment.None;
                }

                // Scan through Custom Attributes on type, looking for interesting bits. We only
                // need to do this for RuntimeClasses
                if ((treatment == TypeDefTreatment.PrefixWinRTName || treatment == TypeDefTreatment.NormalNonAttribute))
                {
                    if ((flags & TypeAttributes.Interface) == 0
                        && HasAttribute(handle, "Windows.UI.Xaml", "TreatAsAbstractComposableClassAttribute"))
                    {
                        treatment |= TypeDefTreatment.MarkAbstractFlag;
                    }
                }
            }
            else if (_metadataKind == MetadataKind.ManagedWindowsMetadata && IsClrImplementationType(handle))
            {
                // <CLR> implementation classes are not marked WindowsRuntime, but still need to be modified
                // by the adapter. 
                treatment = TypeDefTreatment.UnmangleWinRTName;
            }
            else
            {
                treatment = TypeDefTreatment.None;
            }

            return TreatmentAndRowId((byte)treatment, handle.RowId);
        }

        private bool IsClrImplementationType(TypeDefinitionHandle typeDef)
        {
            var attrs = TypeDefTable.GetFlags(typeDef);

            if ((attrs & (TypeAttributes.VisibilityMask | TypeAttributes.SpecialName)) != TypeAttributes.SpecialName)
            {
                return false;
            }

            return StringHeap.StartsWithRaw(TypeDefTable.GetName(typeDef), ClrPrefix);
        }

        #endregion

        #region TypeRef

        internal uint CalculateTypeRefTreatmentAndRowId(TypeReferenceHandle handle)
        {
            Debug.Assert(_metadataKind != MetadataKind.Ecma335);

            bool isIDisposable;
            int projectionIndex = GetProjectionIndexForTypeReference(handle, out isIDisposable);
            if (projectionIndex >= 0)
            {
                return TreatmentAndRowId((byte)TypeRefTreatment.UseProjectionInfo, projectionIndex);
            }
            else
            {
                return TreatmentAndRowId((byte)GetSpecialTypeRefTreatment(handle), handle.RowId);
            }
        }

        private TypeRefTreatment GetSpecialTypeRefTreatment(TypeReferenceHandle handle)
        {
            if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System"))
            {
                StringHandle name = TypeRefTable.GetName(handle);

                if (StringHeap.EqualsRaw(name, "MulticastDelegate"))
                {
                    return TypeRefTreatment.SystemDelegate;
                }

                if (StringHeap.EqualsRaw(name, "Attribute"))
                {
                    return TypeRefTreatment.SystemAttribute;
                }
            }

            return TypeRefTreatment.None;
        }

        private bool IsSystemAttribute(TypeReferenceHandle handle)
        {
            return StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System") &&
                   StringHeap.EqualsRaw(TypeRefTable.GetName(handle), "Attribute");
        }

        private bool NeedsWinRTPrefix(TypeAttributes flags, EntityHandle extends)
        {
            if ((flags & (TypeAttributes.VisibilityMask | TypeAttributes.Interface)) != TypeAttributes.Public)
            {
                return false;
            }

            if (extends.Kind != HandleKind.TypeReference)
            {
                return false;
            }

            // Check if the type is a delegate, struct, or attribute
            TypeReferenceHandle extendsRefHandle = (TypeReferenceHandle)extends;
            if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(extendsRefHandle), "System"))
            {
                StringHandle nameHandle = TypeRefTable.GetName(extendsRefHandle);
                if (StringHeap.EqualsRaw(nameHandle, "MulticastDelegate")
                    || StringHeap.EqualsRaw(nameHandle, "ValueType")
                    || StringHeap.EqualsRaw(nameHandle, "Attribute"))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region MethodDef

        private uint CalculateMethodDefTreatmentAndRowId(MethodDefinitionHandle methodDef)
        {
            MethodDefTreatment treatment = MethodDefTreatment.Implementation;

            TypeDefinitionHandle parentTypeDef = GetDeclaringType(methodDef);
            TypeAttributes parentFlags = TypeDefTable.GetFlags(parentTypeDef);

            if ((parentFlags & TypeAttributes.WindowsRuntime) != 0)
            {
                if (IsClrImplementationType(parentTypeDef))
                {
                    treatment = MethodDefTreatment.Implementation;
                }
                else if (parentFlags.IsNested())
                {
                    treatment = MethodDefTreatment.Implementation;
                }
                else if ((parentFlags & TypeAttributes.Interface) != 0)
                {
                    treatment = MethodDefTreatment.InterfaceMethod;
                }
                else if (_metadataKind == MetadataKind.ManagedWindowsMetadata && (parentFlags & TypeAttributes.Public) == 0)
                {
                    treatment = MethodDefTreatment.Implementation;
                }
                else
                {
                    treatment = MethodDefTreatment.Other;

                    var parentBaseType = TypeDefTable.GetExtends(parentTypeDef);
                    if (parentBaseType.Kind == HandleKind.TypeReference)
                    {
                        switch (GetSpecialTypeRefTreatment((TypeReferenceHandle)parentBaseType))
                        {
                            case TypeRefTreatment.SystemAttribute:
                                treatment = MethodDefTreatment.AttributeMethod;
                                break;

                            case TypeRefTreatment.SystemDelegate:
                                treatment = MethodDefTreatment.DelegateMethod | MethodDefTreatment.MarkPublicFlag;
                                break;
                        }
                    }
                }
            }

            if (treatment == MethodDefTreatment.Other)
            {
                // we want to hide the method if it implements
                // only redirected interfaces
                // We also want to check if the methodImpl is IClosable.Close,
                // so we can change the name
                bool seenRedirectedInterfaces = false;
                bool seenNonRedirectedInterfaces = false;

                bool isIClosableClose = false;

                foreach (var methodImplHandle in new MethodImplementationHandleCollection(this, parentTypeDef))
                {
                    MethodImplementation methodImpl = GetMethodImplementation(methodImplHandle);
                    if (methodImpl.MethodBody == methodDef)
                    {
                        EntityHandle declaration = methodImpl.MethodDeclaration;

                        // See if this MethodImpl implements a redirected interface
                        // In WinMD, MethodImpl will always use MemberRef and TypeRefs to refer to redirected interfaces,
                        // even if they are in the same module.
                        if (declaration.Kind == HandleKind.MemberReference &&
                            ImplementsRedirectedInterface((MemberReferenceHandle)declaration, out isIClosableClose))
                        {
                            seenRedirectedInterfaces = true;
                            if (isIClosableClose)
                            {
                                // This method implements IClosable.Close
                                // Let's rename to IDisposable later
                                // Once we know this implements IClosable.Close, we are done
                                // looking
                                break;
                            }
                        }
                        else
                        {
                            // Now we know this implements a non-redirected interface
                            // But we need to keep looking, just in case we got a methodimpl that
                            // implements the IClosable.Close method and needs to be renamed
                            seenNonRedirectedInterfaces = true;
                        }
                    }
                }

                if (isIClosableClose)
                {
                    treatment = MethodDefTreatment.DisposeMethod;
                }
                else if (seenRedirectedInterfaces && !seenNonRedirectedInterfaces)
                {
                    // Only hide if all the interfaces implemented are redirected
                    treatment = MethodDefTreatment.HiddenInterfaceImplementation;
                }
            }

            // If treatment is other, then this is a non-managed WinRT runtime class definition
            // Find out about various bits that we apply via attributes and name parsing
            if (treatment == MethodDefTreatment.Other)
            {
                treatment |= GetMethodTreatmentFromCustomAttributes(methodDef);
            }

            return TreatmentAndRowId((byte)treatment, methodDef.RowId);
        }

        private MethodDefTreatment GetMethodTreatmentFromCustomAttributes(MethodDefinitionHandle methodDef)
        {
            MethodDefTreatment treatment = 0;

            foreach (var caHandle in GetCustomAttributes(methodDef))
            {
                StringHandle namespaceHandle, nameHandle;
                if (!GetAttributeTypeNameRaw(caHandle, out namespaceHandle, out nameHandle))
                {
                    continue;
                }

                Debug.Assert(!namespaceHandle.IsVirtual && !nameHandle.IsVirtual);

                if (StringHeap.EqualsRaw(namespaceHandle, "Windows.UI.Xaml"))
                {
                    if (StringHeap.EqualsRaw(nameHandle, "TreatAsPublicMethodAttribute"))
                    {
                        treatment |= MethodDefTreatment.MarkPublicFlag;
                    }

                    if (StringHeap.EqualsRaw(nameHandle, "TreatAsAbstractMethodAttribute"))
                    {
                        treatment |= MethodDefTreatment.MarkAbstractFlag;
                    }
                }
            }

            return treatment;
        }

        #endregion

        #region FieldDef

        /// <summary>
        /// The backing field of a WinRT enumeration type is not public although the backing fields
        /// of managed enumerations are. To allow managed languages to directly access this field,
        /// it is made public by the metadata adapter.
        /// </summary>
        private uint CalculateFieldDefTreatmentAndRowId(FieldDefinitionHandle handle)
        {
            var flags = FieldTable.GetFlags(handle);
            FieldDefTreatment treatment = FieldDefTreatment.None;

            if ((flags & FieldAttributes.RTSpecialName) != 0 && StringHeap.EqualsRaw(FieldTable.GetName(handle), "value__"))
            {
                TypeDefinitionHandle typeDef = GetDeclaringType(handle);

                EntityHandle baseTypeHandle = TypeDefTable.GetExtends(typeDef);
                if (baseTypeHandle.Kind == HandleKind.TypeReference)
                {
                    var typeRef = (TypeReferenceHandle)baseTypeHandle;

                    if (StringHeap.EqualsRaw(TypeRefTable.GetName(typeRef), "Enum") &&
                        StringHeap.EqualsRaw(TypeRefTable.GetNamespace(typeRef), "System"))
                    {
                        treatment = FieldDefTreatment.EnumValue;
                    }
                }
            }

            return TreatmentAndRowId((byte)treatment, handle.RowId);
        }

        #endregion

        #region MemberRef

        private uint CalculateMemberRefTreatmentAndRowId(MemberReferenceHandle handle)
        {
            MemberRefTreatment treatment;

            // We need to rename the MemberRef for IClosable.Close as well
            // so that the MethodImpl for the Dispose method can be correctly shown
            // as IDisposable.Dispose instead of IDisposable.Close
            bool isIDisposable;
            if (ImplementsRedirectedInterface(handle, out isIDisposable) && isIDisposable)
            {
                treatment = MemberRefTreatment.Dispose;
            }
            else
            {
                treatment = MemberRefTreatment.None;
            }

            return TreatmentAndRowId((byte)treatment, handle.RowId);
        }

        /// <summary>
        /// We want to know if a given method implements a redirected interface.
        /// For example, if we are given the method RemoveAt on a class "A" 
        /// which implements the IVector interface (which is redirected
        /// to IList in .NET) then this method would return true. The most 
        /// likely reason why we would want to know this is that we wish to hide
        /// (mark private) all methods which implement methods on a redirected 
        /// interface.
        /// </summary>
        /// <param name="memberRef">The declaration token for the method</param>
        /// <param name="isIDisposable">
        /// Returns true if the redirected interface is <see cref="IDisposable"/>.
        /// </param>
        /// <returns>True if the method implements a method on a redirected interface.
        /// False otherwise.</returns>
        private bool ImplementsRedirectedInterface(MemberReferenceHandle memberRef, out bool isIDisposable)
        {
            isIDisposable = false;

            EntityHandle parent = MemberRefTable.GetClass(memberRef);

            TypeReferenceHandle typeRef;
            if (parent.Kind == HandleKind.TypeReference)
            {
                typeRef = (TypeReferenceHandle)parent;
            }
            else if (parent.Kind == HandleKind.TypeSpecification)
            {
                BlobHandle blob = TypeSpecTable.GetSignature((TypeSpecificationHandle)parent);
                BlobReader sig = new BlobReader(BlobHeap.GetMemoryBlock(blob));

                if (sig.Length < 2 ||
                    sig.ReadByte() != (byte)CorElementType.ELEMENT_TYPE_GENERICINST ||
                    sig.ReadByte() != (byte)CorElementType.ELEMENT_TYPE_CLASS)
                {
                    return false;
                }

                EntityHandle token = sig.ReadTypeHandle();
                if (token.Kind != HandleKind.TypeReference)
                {
                    return false;
                }

                typeRef = (TypeReferenceHandle)token;
            }
            else
            {
                return false;
            }

            return GetProjectionIndexForTypeReference(typeRef, out isIDisposable) >= 0;
        }

        #endregion

        #region AssemblyRef

        private int FindMscorlibAssemblyRefNoProjection()
        {
            for (int i = 1; i <= AssemblyRefTable.NumberOfNonVirtualRows; i++)
            {
                if (StringHeap.EqualsRaw(AssemblyRefTable.GetName(i), "mscorlib"))
                {
                    return i;
                }
            }

            throw new BadImageFormatException(SR.WinMDMissingMscorlibRef);
        }

        #endregion

        #region CustomAttribute

        internal CustomAttributeValueTreatment CalculateCustomAttributeValueTreatment(CustomAttributeHandle handle)
        {
            Debug.Assert(_metadataKind != MetadataKind.Ecma335);

            var parent = CustomAttributeTable.GetParent(handle);

            // Check for Windows.Foundation.Metadata.AttributeUsageAttribute.
            // WinMD rules: 
            //   - The attribute is only applicable on TypeDefs.
            //   - Constructor must be a MemberRef with TypeRef.
            if (!IsWindowsAttributeUsageAttribute(parent, handle))
            {
                return CustomAttributeValueTreatment.None;
            }

            var targetTypeDef = (TypeDefinitionHandle)parent;
            if (StringHeap.EqualsRaw(TypeDefTable.GetNamespace(targetTypeDef), "Windows.Foundation.Metadata"))
            {
                if (StringHeap.EqualsRaw(TypeDefTable.GetName(targetTypeDef), "VersionAttribute"))
                {
                    return CustomAttributeValueTreatment.AttributeUsageVersionAttribute;
                }

                if (StringHeap.EqualsRaw(TypeDefTable.GetName(targetTypeDef), "DeprecatedAttribute"))
                {
                    return CustomAttributeValueTreatment.AttributeUsageDeprecatedAttribute;
                }
            }

            bool allowMultiple = HasAttribute(targetTypeDef, "Windows.Foundation.Metadata", "AllowMultipleAttribute");
            return allowMultiple ? CustomAttributeValueTreatment.AttributeUsageAllowMultiple : CustomAttributeValueTreatment.AttributeUsageAllowSingle;
        }

        private bool IsWindowsAttributeUsageAttribute(EntityHandle targetType, CustomAttributeHandle attributeHandle)
        {
            // Check for Windows.Foundation.Metadata.AttributeUsageAttribute.
            // WinMD rules: 
            //   - The attribute is only applicable on TypeDefs.
            //   - Constructor must be a MemberRef with TypeRef.

            if (targetType.Kind != HandleKind.TypeDefinition)
            {
                return false;
            }

            var attributeCtor = CustomAttributeTable.GetConstructor(attributeHandle);
            if (attributeCtor.Kind != HandleKind.MemberReference)
            {
                return false;
            }

            var attributeType = MemberRefTable.GetClass((MemberReferenceHandle)attributeCtor);
            if (attributeType.Kind != HandleKind.TypeReference)
            {
                return false;
            }

            var attributeTypeRef = (TypeReferenceHandle)attributeType;
            return StringHeap.EqualsRaw(TypeRefTable.GetName(attributeTypeRef), "AttributeUsageAttribute") &&
                   StringHeap.EqualsRaw(TypeRefTable.GetNamespace(attributeTypeRef), "Windows.Foundation.Metadata");
        }

        private bool HasAttribute(EntityHandle token, string asciiNamespaceName, string asciiTypeName)
        {
            foreach (var caHandle in GetCustomAttributes(token))
            {
                StringHandle namespaceName, typeName;
                if (GetAttributeTypeNameRaw(caHandle, out namespaceName, out typeName) &&
                    StringHeap.EqualsRaw(typeName, asciiTypeName) &&
                    StringHeap.EqualsRaw(namespaceName, asciiNamespaceName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool GetAttributeTypeNameRaw(CustomAttributeHandle caHandle, out StringHandle namespaceName, out StringHandle typeName)
        {
            namespaceName = typeName = default(StringHandle);

            EntityHandle typeDefOrRef = GetAttributeTypeRaw(caHandle);
            if (typeDefOrRef.IsNil)
            {
                return false;
            }

            if (typeDefOrRef.Kind == HandleKind.TypeReference)
            {
                TypeReferenceHandle typeRef = (TypeReferenceHandle)typeDefOrRef;
                var resolutionScope = TypeRefTable.GetResolutionScope(typeRef);

                if (!resolutionScope.IsNil && resolutionScope.Kind == HandleKind.TypeReference)
                {
                    // we don't need to handle nested types
                    return false;
                }

                // other resolution scopes don't affect full name

                typeName = TypeRefTable.GetName(typeRef);
                namespaceName = TypeRefTable.GetNamespace(typeRef);
            }
            else if (typeDefOrRef.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinitionHandle typeDef = (TypeDefinitionHandle)typeDefOrRef;

                if (TypeDefTable.GetFlags(typeDef).IsNested())
                {
                    // we don't need to handle nested types
                    return false;
                }

                typeName = TypeDefTable.GetName(typeDef);
                namespaceName = TypeDefTable.GetNamespace(typeDef);
            }
            else
            {
                // invalid metadata
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the type definition or reference handle of the attribute type.
        /// </summary>
        /// <returns><see cref="TypeDefinitionHandle"/> or <see cref="TypeReferenceHandle"/> or nil token if the metadata is invalid and the type can't be determined.</returns>
        private EntityHandle GetAttributeTypeRaw(CustomAttributeHandle handle)
        {
            var ctor = CustomAttributeTable.GetConstructor(handle);

            if (ctor.Kind == HandleKind.MethodDefinition)
            {
                return GetDeclaringType((MethodDefinitionHandle)ctor);
            }

            if (ctor.Kind == HandleKind.MemberReference)
            {
                // In general the parent can be MethodDef, ModuleRef, TypeDef, TypeRef, or TypeSpec.
                // For attributes only TypeDef and TypeRef are applicable.
                EntityHandle typeDefOrRef = MemberRefTable.GetClass((MemberReferenceHandle)ctor);
                HandleKind handleType = typeDefOrRef.Kind;

                if (handleType == HandleKind.TypeReference || handleType == HandleKind.TypeDefinition)
                {
                    return typeDefOrRef;
                }
            }

            return default(EntityHandle);
        }
        #endregion
    }
}
