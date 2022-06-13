// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a node with non-GC static data associated with a type, along
    /// with it's class constructor context. The non-GC static data region shall be prefixed
    /// with the class constructor context if the type has a class constructor that
    /// needs to be triggered before the type members can be accessed.
    /// </summary>
    public class NonGCStaticsNode : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly MetadataType _type;
        private readonly PreinitializationManager _preinitializationManager;

        public NonGCStaticsNode(MetadataType type, PreinitializationManager preinitializationManager)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            Debug.Assert(!type.IsGenericDefinition);
            _type = type;
            _preinitializationManager = preinitializationManager;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection Section
        {
            get
            {
                if (_preinitializationManager.HasLazyStaticConstructor(_type)
                    || _preinitializationManager.IsPreinitialized(_type))
                {
                    // We have data to be emitted so this needs to be in an initialized data section
                    return ObjectNodeSection.DataSection;
                }
                else
                {
                    // This is all zeros; place this to the BSS section
                    return ObjectNodeSection.BssSection;
                }
            }
        }

        public static string GetMangledName(TypeDesc type, NameMangler nameMangler)
        {
            return nameMangler.NodeMangler.NonGCStatics(type);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.NodeMangler.NonGCStatics(_type));
        }

        int ISymbolNode.Offset => 0;

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                // Make sure the NonGCStatics symbol always points to the beginning of the data.
                if (_preinitializationManager.HasLazyStaticConstructor(_type))
                {
                    return GetClassConstructorContextStorageSize(_type.Context.Target, _type);
                }
                else
                {
                    return 0;
                }
            }
        }

        public bool HasCCtorContext => _preinitializationManager.HasLazyStaticConstructor(_type);

        public override bool IsShareable => EETypeNode.IsTypeNodeShareable(_type);

        public MetadataType Type => _type;

        public static int GetClassConstructorContextSize(TargetDetails target)
        {
            // TODO: Assert that StaticClassConstructionContext type has the expected size
            //       (need to make it a well known type?)
            return target.PointerSize * 2;
        }

        private static int GetClassConstructorContextStorageSize(TargetDetails target, MetadataType type)
        {
            int alignmentRequired = Math.Max(type.NonGCStaticFieldAlignment.AsInt, GetClassConstructorContextAlignment(target));
            return AlignmentHelper.AlignUp(GetClassConstructorContextSize(type.Context.Target), alignmentRequired);
        }

        private static int GetClassConstructorContextAlignment(TargetDetails target)
        {
            // TODO: Assert that StaticClassConstructionContext type has the expected alignment
            //       (need to make it a well known type?)
            return target.PointerSize;
        }

        public override bool StaticDependenciesAreComputed => true;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = new DependencyList();

            if (factory.PreinitializationManager.HasEagerStaticConstructor(_type))
            {
                dependencyList.Add(factory.EagerCctorIndirection(_type.GetStaticConstructor()), "Eager .cctor");
            }

            if (_type.Module.GetGlobalModuleType().GetStaticConstructor() is MethodDesc moduleCctor)
            {
                dependencyList.Add(factory.MethodEntrypoint(moduleCctor), "Static base in a module with initializer");
            }

            EETypeNode.AddDependenciesForStaticsNode(factory, _type, ref dependencyList);

            return dependencyList;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);

            // If the type has a class constructor, its non-GC statics section is prefixed  
            // by System.Runtime.CompilerServices.StaticClassConstructionContext struct.
            if (factory.PreinitializationManager.HasLazyStaticConstructor(_type))
            {
                int alignmentRequired = Math.Max(_type.NonGCStaticFieldAlignment.AsInt, GetClassConstructorContextAlignment(_type.Context.Target));
                int classConstructorContextStorageSize = GetClassConstructorContextStorageSize(factory.Target, _type);
                builder.RequireInitialAlignment(alignmentRequired);

                Debug.Assert(classConstructorContextStorageSize >= GetClassConstructorContextSize(_type.Context.Target));

                // Add padding before the context if alignment forces us to do so
                builder.EmitZeros(classConstructorContextStorageSize - GetClassConstructorContextSize(_type.Context.Target));

                // Emit the actual StaticClassConstructionContext
                MethodDesc cctorMethod = _type.GetStaticConstructor();
                builder.EmitPointerReloc(factory.ExactCallableAddress(cctorMethod));
                builder.EmitZeroPointer();
            }
            else
            {
                builder.RequireInitialAlignment(_type.NonGCStaticFieldAlignment.AsInt);
            }

            if (_preinitializationManager.IsPreinitialized(_type))
            {
                TypePreinit.PreinitializationInfo preinitInfo = _preinitializationManager.GetPreinitializationInfo(_type);
                int initialOffset = builder.CountBytes;
                foreach (FieldDesc field in _type.GetFields())
                {
                    if (!field.IsStatic || field.HasRva || field.IsLiteral || field.IsThreadStatic || field.HasGCStaticBase)
                        continue;

                    int padding = field.Offset.AsInt - builder.CountBytes + initialOffset;
                    Debug.Assert(padding >= 0);
                    builder.EmitZeros(padding);

                    TypePreinit.ISerializableValue val = preinitInfo.GetFieldValue(field);
                    int currentOffset = builder.CountBytes;
                    val.WriteFieldData(ref builder, field, factory);
                    Debug.Assert(builder.CountBytes - currentOffset == field.FieldType.GetElementSize().AsInt);
                }

                int pad = _type.NonGCStaticFieldSize.AsInt - builder.CountBytes + initialOffset;
                Debug.Assert(pad >= 0);
                builder.EmitZeros(pad);
            }
            else
            {
                builder.EmitZeros(_type.NonGCStaticFieldSize.AsInt);
            }

            builder.AddSymbol(this);

            return builder.ToObjectData();
        }

        public override int ClassCode => -1173104872;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_type, ((NonGCStaticsNode)other)._type);
        }
    }
}
