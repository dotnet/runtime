// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the layout of the generic dictionary associated with a given canonical
    /// generic type or generic method. Maintains a bag of <see cref="GenericLookupResult"/> associated
    /// with the canonical entity.
    /// </summary>
    /// <remarks>
    /// The generic dictionary doesn't have any dependent nodes because <see cref="GenericLookupResult"/>
    /// are runtime-determined - the concrete dependency depends on the generic context the canonical
    /// entity is instantiated with.
    /// </remarks>
    public abstract class DictionaryLayoutNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeSystemEntity _owningMethodOrType;

        public DictionaryLayoutNode(TypeSystemEntity owningMethodOrType)
        {
            _owningMethodOrType = owningMethodOrType;
            Validate();
        }

        [Conditional("DEBUG")]
        private void Validate()
        {
            TypeDesc type = _owningMethodOrType as TypeDesc;
            if (type != null)
            {
                Debug.Assert(type.IsCanonicalSubtype(CanonicalFormKind.Any));
                Debug.Assert(type.IsDefType);
            }
            else
            {
                MethodDesc method = _owningMethodOrType as MethodDesc;
                Debug.Assert(method != null && method.IsSharedByGenericInstantiations);
            }
        }

        public virtual ObjectNodeSection DictionarySection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
            {
                if (_owningMethodOrType is TypeDesc)
                {
                    return ObjectNodeSection.FoldableReadOnlyDataSection;
                }
                else
                {
                    // Method dictionary serves as an identity at runtime which means they are not foldable.
                    Debug.Assert(_owningMethodOrType is MethodDesc);
                    return ObjectNodeSection.ReadOnlyDataSection;
                }
            }
            else
            {
                return ObjectNodeSection.DataSection;
            }
        }

        /// <summary>
        /// Ensure that a generic lookup result can be resolved. Used to add new lookups to a dictionary which HasUnfixedSlots
        /// Must not be used after any calls to GetSlotForEntry
        /// </summary>
        public abstract void EnsureEntry(GenericLookupResult entry);

        /// <summary>
        /// Get a slot index for a given entry. Slot indices are never expected to change once given out.
        /// </summary>
        public abstract int GetSlotForEntry(GenericLookupResult entry);

        /// <summary>
        /// Get the slot for an entry which is fixed already. Otherwise return -1
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public virtual int GetSlotForFixedEntry(GenericLookupResult entry)
        {
            return GetSlotForEntry(entry);
        }
        
        public abstract IEnumerable<GenericLookupResult> Entries
        {
            get;
        }

        public virtual IEnumerable<GenericLookupResult> FixedEntries => Entries;

        public TypeSystemEntity OwningMethodOrType => _owningMethodOrType;

        /// <summary>
        /// Gets a value indicating whether the slot assignment is determined at the node creation time.
        /// </summary>
        public abstract bool HasFixedSlots
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating if this dictionary may have non fixed slots
        /// </summary>
        public virtual bool HasUnfixedSlots => !HasFixedSlots;

        public virtual ICollection<NativeLayoutVertexNode> GetTemplateEntries(NodeFactory factory)
        {
            ArrayBuilder<NativeLayoutVertexNode> templateEntries = new ArrayBuilder<NativeLayoutVertexNode>();
            foreach (var entry in Entries)
            {
                templateEntries.Add(entry.TemplateDictionaryNode(factory));
            }

            return templateEntries.ToArray();
        }

        public virtual void EmitDictionaryData(ref ObjectDataBuilder builder, NodeFactory factory, GenericDictionaryNode dictionary, bool fixedLayoutOnly)
        {
            var context = new GenericLookupResultContext(dictionary.OwningEntity, dictionary.TypeInstantiation, dictionary.MethodInstantiation);

            IEnumerable<GenericLookupResult> entriesToEmit = fixedLayoutOnly ? FixedEntries : Entries;

            foreach (GenericLookupResult lookupResult in entriesToEmit)
            {
#if DEBUG
                int offsetBefore = builder.CountBytes;
#endif

                lookupResult.EmitDictionaryEntry(ref builder, factory, context, dictionary);

#if DEBUG
                Debug.Assert(builder.CountBytes - offsetBefore == factory.Target.PointerSize);
#endif
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            if (HasFixedSlots)
            {
                foreach (GenericLookupResult lookupResult in FixedEntries)
                {
                    foreach (DependencyNodeCore<NodeFactory> dependency in lookupResult.NonRelocDependenciesFromUsage(factory))
                    {
                        yield return new DependencyListEntry(dependency, "GenericLookupResultDependency");
                    }
                }
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            Debug.Assert(HasFixedSlots);

            NativeLayoutSavedVertexNode templateLayout;
            if (_owningMethodOrType is MethodDesc)
            {
                templateLayout = factory.NativeLayout.TemplateMethodLayout((MethodDesc)_owningMethodOrType);
            }
            else
            {
                templateLayout = factory.NativeLayout.TemplateTypeLayout((TypeDesc)_owningMethodOrType);
            }

            List<CombinedDependencyListEntry> conditionalDependencies = new List<CombinedDependencyListEntry>();

            foreach (var lookupSignature in FixedEntries)
            {
                conditionalDependencies.Add(new CombinedDependencyListEntry(lookupSignature.TemplateDictionaryNode(factory),
                                                                templateLayout,
                                                                "Type loader template"));
            }

            return conditionalDependencies;
        }

        protected override string GetName(NodeFactory factory) => $"Dictionary layout for {_owningMethodOrType.ToString()}";

        public override bool HasConditionalStaticDependencies => HasFixedSlots;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }

    public class PrecomputedDictionaryLayoutNode : DictionaryLayoutNode
    {
        private readonly GenericLookupResult[] _layout;

        public override bool HasFixedSlots => true;

        public PrecomputedDictionaryLayoutNode(TypeSystemEntity owningMethodOrType, IEnumerable<GenericLookupResult> layout)
            : base(owningMethodOrType)
        {
            ArrayBuilder<GenericLookupResult> l = new ArrayBuilder<GenericLookupResult>();
            foreach (var entry in layout)
                l.Add(entry);

            _layout = l.ToArray();
        }

        public override void EnsureEntry(GenericLookupResult entry)
        {
            int index = Array.IndexOf(_layout, entry);

            if (index == -1)
            {
                // Using EnsureEntry to add a slot to a PrecomputedDictionaryLayoutNode is not supported
                throw new NotSupportedException();
            }
        }

        public override int GetSlotForEntry(GenericLookupResult entry)
        {
            int index = Array.IndexOf(_layout, entry);

            // This entry wasn't precomputed. If this is an optimized build with scanner, it might suggest
            // the scanner didn't see the need for this. There is a discrepancy between scanning and compiling.
            // This is a fatal error to prevent bad codegen.
            if (index < 0)
                throw new InvalidOperationException($"{OwningMethodOrType}: {entry}");

            return index;
        }

        public override IEnumerable<GenericLookupResult> Entries
        {
            get
            {
                return _layout;
            }
        }
    }

    public sealed class LazilyBuiltDictionaryLayoutNode : DictionaryLayoutNode
    {
        class EntryHashTable : LockFreeReaderHashtable<GenericLookupResult, GenericLookupResult>
        {
            protected override bool CompareKeyToValue(GenericLookupResult key, GenericLookupResult value) => Object.Equals(key, value);
            protected override bool CompareValueToValue(GenericLookupResult value1, GenericLookupResult value2) => Object.Equals(value1, value2);
            protected override GenericLookupResult CreateValueFromKey(GenericLookupResult key) => key;
            protected override int GetKeyHashCode(GenericLookupResult key) => key.GetHashCode();
            protected override int GetValueHashCode(GenericLookupResult value) => value.GetHashCode();
        }

        private EntryHashTable _entries = new EntryHashTable();
        private volatile GenericLookupResult[] _layout;

        public override bool HasFixedSlots => false;

        public LazilyBuiltDictionaryLayoutNode(TypeSystemEntity owningMethodOrType)
            : base(owningMethodOrType)
        {
        }

        public override void EnsureEntry(GenericLookupResult entry)
        {
            Debug.Assert(_layout == null, "Trying to add entry but layout already computed");
            _entries.AddOrGetExisting(entry);
        }

        private void ComputeLayout()
        {
            GenericLookupResult[] layout = new GenericLookupResult[_entries.Count];
            int index = 0;
            foreach (GenericLookupResult entry in EntryHashTable.Enumerator.Get(_entries))
            {
                layout[index++] = entry;
            }

            var comparer = new GenericLookupResult.Comparer(TypeSystemComparer.Instance);
            Array.Sort(layout, comparer.Compare);

            // Only publish after the full layout is computed. Races are fine.
            _layout = layout;
        }

        public override int GetSlotForEntry(GenericLookupResult entry)
        {
            if (_layout == null)
                ComputeLayout();

            int index = Array.IndexOf(_layout, entry);

            // We never called EnsureEntry on this during compilation?
            // This is a fatal error to prevent bad codegen.
            if (index < 0)
                throw new InvalidOperationException($"{OwningMethodOrType}: {entry}");

            return index;
        }

        public override IEnumerable<GenericLookupResult> Entries
        {
            get
            {
                if (_layout == null)
                    ComputeLayout();

                return _layout;
            }
        }
    }
}
