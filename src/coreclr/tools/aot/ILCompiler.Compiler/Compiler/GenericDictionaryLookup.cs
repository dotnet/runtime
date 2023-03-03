// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Structure that specifies how a generic dictionary lookup should be performed.
    /// </summary>
    public struct GenericDictionaryLookup
    {
        private const short UseHelperOffset = -1;

        private readonly object _helperObject;

        private readonly short _offset1;
        private readonly short _offset2;
        private readonly bool _indirectLastOffset;

        /// <summary>
        /// Gets the information about the source of the generic context for shared code.
        /// </summary>
        public readonly GenericContextSource ContextSource;

        /// <summary>
        /// Gets the target object of the lookup. Only valid when <see cref="UseHelper"/> is true.
        /// This is typically a <see cref="TypeDesc"/> whose <see cref="TypeDesc.IsRuntimeDeterminedSubtype"/>
        /// is true, a <see cref="FieldDesc"/> on a runtime determined type, a <see cref="MethodDesc"/>, or
        /// a <see cref="DelegateCreationInfo"/>.
        /// </summary>
        public object HelperObject
        {
            get
            {
                Debug.Assert(_offset1 == UseHelperOffset);
                return _helperObject;
            }
        }

        /// <summary>
        /// Gets the ID of the helper to use if <see cref="UseHelper"/> is true.
        /// </summary>
        public ReadyToRunHelperId HelperId
        {
            get
            {
                Debug.Assert(_offset1 == UseHelperOffset);
                return (ReadyToRunHelperId)_offset2;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the lookup needs to be performed by calling a helper method.
        /// </summary>
        public bool UseHelper
        {
            get
            {
                return _offset1 == UseHelperOffset;
            }
        }

        /// <summary>
        /// Gets the number of indirections to follow. Only valid if <see cref="UseHelper"/> is false.
        /// </summary>
        public int NumberOfIndirections
        {
            get
            {
                Debug.Assert(!UseHelper);
                return ContextSource == GenericContextSource.MethodParameter ? 1 : 2;
            }
        }

        public int this[int index]
        {
            get
            {
                Debug.Assert(!UseHelper);
                Debug.Assert(index < NumberOfIndirections);
                switch (index)
                {
                    case 0:
                        return _offset1;
                    case 1:
                        return _offset2;
                }

                // Should be unreachable.
                throw new NotSupportedException();
            }
        }

        public bool IndirectLastOffset
        {
            get
            {
                Debug.Assert(!UseHelper);
                return _indirectLastOffset;
            }
        }

        private GenericDictionaryLookup(GenericContextSource contextSource, int offset1, int offset2, object helperObject, bool indirectLastOffset)
        {
            ContextSource = contextSource;
            _offset1 = checked((short)offset1);
            _offset2 = checked((short)offset2);
            _helperObject = helperObject;
            _indirectLastOffset = indirectLastOffset;
        }

        public static GenericDictionaryLookup CreateFixedLookup(GenericContextSource contextSource, int offset1, int offset2 = UseHelperOffset, bool indirectLastOffset = false)
        {
            Debug.Assert(offset1 != UseHelperOffset);
            return new GenericDictionaryLookup(contextSource, offset1, offset2, null, indirectLastOffset);
        }

        public static GenericDictionaryLookup CreateHelperLookup(GenericContextSource contextSource, ReadyToRunHelperId helperId, object helperObject)
        {
            return new GenericDictionaryLookup(contextSource, UseHelperOffset, checked((short)helperId), helperObject, indirectLastOffset: false);
        }
    }

    /// <summary>
    /// Specifies to source of the generic context.
    /// </summary>
    public enum GenericContextSource
    {
        /// <summary>
        /// Generic context is specified by a hidden parameter that has a method dictionary.
        /// </summary>
        MethodParameter,

        /// <summary>
        /// Generic context is specified by a hidden parameter that has a type.
        /// </summary>
        TypeParameter,

        /// <summary>
        /// Generic context is specified implicitly by the `this` object.
        /// </summary>
        ThisObject,
    }
}
