// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents generic type parameters of a method or type.
    /// </summary>
    public readonly struct GenericParameterHandleCollection : IReadOnlyList<GenericParameterHandle>
    {
        private readonly int _firstRowId;
        private readonly ushort _count;

        internal GenericParameterHandleCollection(int firstRowId, ushort count)
        {
            _firstRowId = firstRowId;
            _count = count;
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public GenericParameterHandle this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    Throw.IndexOutOfRange();
                }

                return GenericParameterHandle.FromRowId(_firstRowId + index);
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_firstRowId, _firstRowId + _count - 1);
        }

        IEnumerator<GenericParameterHandle> IEnumerable<GenericParameterHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<GenericParameterHandle>, IEnumerator
        {
            private readonly int _lastRowId; // inclusive

            // first parameter rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int firstRowId, int lastRowId)
            {
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public GenericParameterHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return GenericParameterHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Represents constraints of a generic type parameter.
    /// </summary>
    public readonly struct GenericParameterConstraintHandleCollection : IReadOnlyList<GenericParameterConstraintHandle>
    {
        private readonly int _firstRowId;
        private readonly ushort _count;

        internal GenericParameterConstraintHandleCollection(int firstRowId, ushort count)
        {
            _firstRowId = firstRowId;
            _count = count;
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public GenericParameterConstraintHandle this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    Throw.IndexOutOfRange();
                }

                return GenericParameterConstraintHandle.FromRowId(_firstRowId + index);
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_firstRowId, _firstRowId + _count - 1);
        }

        IEnumerator<GenericParameterConstraintHandle> IEnumerable<GenericParameterConstraintHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<GenericParameterConstraintHandle>, IEnumerator
        {
            private readonly int _lastRowId; // inclusive

            // first parameter rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int firstRowId, int lastRowId)
            {
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public GenericParameterConstraintHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return GenericParameterConstraintHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct CustomAttributeHandleCollection : IReadOnlyCollection<CustomAttributeHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal CustomAttributeHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
            _firstRowId = 1;
            _lastRowId = reader.CustomAttributeTable.NumberOfRows;
        }

        internal CustomAttributeHandleCollection(MetadataReader reader, EntityHandle handle)
        {
            Debug.Assert(reader != null);

            _reader = reader;
            reader.CustomAttributeTable.GetAttributeRange(handle, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<CustomAttributeHandle> IEnumerable<CustomAttributeHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<CustomAttributeHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId; // inclusive

            // first custom attribute rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public CustomAttributeHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (_reader.CustomAttributeTable.PtrTable != null)
                    {
                        return GetCurrentCustomAttributeIndirect();
                    }
                    else
                    {
                        return CustomAttributeHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                    }
                }
            }

            private CustomAttributeHandle GetCurrentCustomAttributeIndirect()
            {
                return CustomAttributeHandle.FromRowId(
                    _reader.CustomAttributeTable.PtrTable[(_currentRowId & (int)TokenTypeIds.RIDMask) - 1]);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct DeclarativeSecurityAttributeHandleCollection : IReadOnlyCollection<DeclarativeSecurityAttributeHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal DeclarativeSecurityAttributeHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
            _firstRowId = 1;
            _lastRowId = reader.DeclSecurityTable.NumberOfRows;
        }

        internal DeclarativeSecurityAttributeHandleCollection(MetadataReader reader, EntityHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            reader.DeclSecurityTable.GetAttributeRange(handle, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<DeclarativeSecurityAttributeHandle> IEnumerable<DeclarativeSecurityAttributeHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<DeclarativeSecurityAttributeHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId; // inclusive

            // first custom attribute rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public DeclarativeSecurityAttributeHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return DeclarativeSecurityAttributeHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct MethodDefinitionHandleCollection : IReadOnlyCollection<MethodDefinitionHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal MethodDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
            _firstRowId = 1;
            _lastRowId = reader.MethodDefTable.NumberOfRows;
        }

        internal MethodDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            _reader = reader;
            reader.GetMethodRange(containingType, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<MethodDefinitionHandle> IEnumerable<MethodDefinitionHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<MethodDefinitionHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId; // inclusive

            // first method rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public MethodDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (_reader.UseMethodPtrTable)
                    {
                        return GetCurrentMethodIndirect();
                    }
                    else
                    {
                        return MethodDefinitionHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                    }
                }
            }

            private MethodDefinitionHandle GetCurrentMethodIndirect()
            {
                return _reader.MethodPtrTable.GetMethodFor(_currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct FieldDefinitionHandleCollection : IReadOnlyCollection<FieldDefinitionHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal FieldDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
            _firstRowId = 1;
            _lastRowId = reader.FieldTable.NumberOfRows;
        }

        internal FieldDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            _reader = reader;
            reader.GetFieldRange(containingType, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<FieldDefinitionHandle> IEnumerable<FieldDefinitionHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<FieldDefinitionHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId; // inclusive

            // first field rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public FieldDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (_reader.UseFieldPtrTable)
                    {
                        return GetCurrentFieldIndirect();
                    }
                    else
                    {
                        return FieldDefinitionHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                    }
                }
            }

            private FieldDefinitionHandle GetCurrentFieldIndirect()
            {
                return _reader.FieldPtrTable.GetFieldFor(_currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct PropertyDefinitionHandleCollection : IReadOnlyCollection<PropertyDefinitionHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal PropertyDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
            _firstRowId = 1;
            _lastRowId = reader.PropertyTable.NumberOfRows;
        }

        internal PropertyDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            _reader = reader;
            reader.GetPropertyRange(containingType, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<PropertyDefinitionHandle> IEnumerable<PropertyDefinitionHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<PropertyDefinitionHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId; // inclusive

            // first Property rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public PropertyDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (_reader.UsePropertyPtrTable)
                    {
                        return GetCurrentPropertyIndirect();
                    }
                    else
                    {
                        return PropertyDefinitionHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                    }
                }
            }

            private PropertyDefinitionHandle GetCurrentPropertyIndirect()
            {
                return _reader.PropertyPtrTable.GetPropertyFor(_currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct EventDefinitionHandleCollection : IReadOnlyCollection<EventDefinitionHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal EventDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
            _firstRowId = 1;
            _lastRowId = reader.EventTable.NumberOfRows;
        }

        internal EventDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            _reader = reader;
            reader.GetEventRange(containingType, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<EventDefinitionHandle> IEnumerable<EventDefinitionHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<EventDefinitionHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId;

            // first rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public EventDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (_reader.UseEventPtrTable)
                    {
                        return GetCurrentEventIndirect();
                    }
                    else
                    {
                        return EventDefinitionHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                    }
                }
            }

            private EventDefinitionHandle GetCurrentEventIndirect()
            {
                return _reader.EventPtrTable.GetEventFor(_currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct MethodImplementationHandleCollection : IReadOnlyCollection<MethodImplementationHandle>
    {
        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal MethodImplementationHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);

            if (containingType.IsNil)
            {
                _firstRowId = 1;
                _lastRowId = reader.MethodImplTable.NumberOfRows;
            }
            else
            {
                reader.MethodImplTable.GetMethodImplRange(containingType, out _firstRowId, out _lastRowId);
            }
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_firstRowId, _lastRowId);
        }

        IEnumerator<MethodImplementationHandle> IEnumerable<MethodImplementationHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<MethodImplementationHandle>, IEnumerator
        {
            private readonly int _lastRowId; // inclusive

            // first impl rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int firstRowId, int lastRowId)
            {
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public MethodImplementationHandle Current
            {
                get
                {
                    return MethodImplementationHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Collection of parameters of a specified method.
    /// </summary>
    public readonly struct ParameterHandleCollection : IReadOnlyCollection<ParameterHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal ParameterHandleCollection(MetadataReader reader, MethodDefinitionHandle containingMethod)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingMethod.IsNil);
            _reader = reader;

            reader.GetParameterRange(containingMethod, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<ParameterHandle> IEnumerable<ParameterHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<ParameterHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId; // inclusive

            // first Parameter rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _lastRowId = lastRowId;
                _currentRowId = firstRowId - 1;
            }

            public ParameterHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (_reader.UseParamPtrTable)
                    {
                        return GetCurrentParameterIndirect();
                    }
                    else
                    {
                        return ParameterHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                    }
                }
            }

            private ParameterHandle GetCurrentParameterIndirect()
            {
                return _reader.ParamPtrTable.GetParamFor(_currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct InterfaceImplementationHandleCollection : IReadOnlyCollection<InterfaceImplementationHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal InterfaceImplementationHandleCollection(MetadataReader reader, TypeDefinitionHandle implementingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!implementingType.IsNil);
            _reader = reader;

            reader.InterfaceImplTable.GetInterfaceImplRange(implementingType, out _firstRowId, out _lastRowId);
        }

        public int Count
        {
            get
            {
                return _lastRowId - _firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<InterfaceImplementationHandle> IEnumerable<InterfaceImplementationHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<InterfaceImplementationHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _lastRowId; // inclusive

            // first rid - 1: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                _reader = reader;
                _currentRowId = firstRowId - 1;
                _lastRowId = lastRowId;
            }

            public InterfaceImplementationHandle Current
            {
                get
                {
                    return InterfaceImplementationHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="TypeDefinitionHandle"/>.
    /// </summary>
    public readonly struct TypeDefinitionHandleCollection : IReadOnlyCollection<TypeDefinitionHandle>
    {
        private readonly int _lastRowId;

        // Creates collection that represents the entire TypeDef table.
        internal TypeDefinitionHandleCollection(int lastRowId)
        {
            _lastRowId = lastRowId;
        }

        public int Count
        {
            get { return _lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_lastRowId);
        }

        IEnumerator<TypeDefinitionHandle> IEnumerable<TypeDefinitionHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<TypeDefinitionHandle>, IEnumerator
        {
            private readonly int _lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                _lastRowId = lastRowId;
                _currentRowId = 0;
            }

            public TypeDefinitionHandle Current
            {
                get
                {
                    return TypeDefinitionHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="TypeReferenceHandle"/>.
    /// </summary>
    public readonly struct TypeReferenceHandleCollection : IReadOnlyCollection<TypeReferenceHandle>
    {
        private readonly int _lastRowId;

        // Creates collection that represents the entire TypeRef table.
        internal TypeReferenceHandleCollection(int lastRowId)
        {
            _lastRowId = lastRowId;
        }

        public int Count
        {
            get { return _lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_lastRowId);
        }

        IEnumerator<TypeReferenceHandle> IEnumerable<TypeReferenceHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<TypeReferenceHandle>, IEnumerator
        {
            private readonly int _lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                _lastRowId = lastRowId;
                _currentRowId = 0;
            }

            public TypeReferenceHandle Current
            {
                get
                {
                    return TypeReferenceHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="TypeReferenceHandle"/>.
    /// </summary>
    public readonly struct ExportedTypeHandleCollection : IReadOnlyCollection<ExportedTypeHandle>
    {
        private readonly int _lastRowId;

        // Creates collection that represents the entire TypeRef table.
        internal ExportedTypeHandleCollection(int lastRowId)
        {
            _lastRowId = lastRowId;
        }

        public int Count
        {
            get { return _lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_lastRowId);
        }

        IEnumerator<ExportedTypeHandle> IEnumerable<ExportedTypeHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<ExportedTypeHandle>, IEnumerator
        {
            private readonly int _lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                _lastRowId = lastRowId;
                _currentRowId = 0;
            }

            public ExportedTypeHandle Current
            {
                get
                {
                    return ExportedTypeHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="MemberReferenceHandle"/>.
    /// </summary>
    public readonly struct MemberReferenceHandleCollection : IReadOnlyCollection<MemberReferenceHandle>
    {
        private readonly int _lastRowId;

        // Creates collection that represents the entire TypeRef table.
        internal MemberReferenceHandleCollection(int lastRowId)
        {
            _lastRowId = lastRowId;
        }

        public int Count
        {
            get { return _lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_lastRowId);
        }

        IEnumerator<MemberReferenceHandle> IEnumerable<MemberReferenceHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<MemberReferenceHandle>, IEnumerator
        {
            private readonly int _lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                _lastRowId = lastRowId;
                _currentRowId = 0;
            }

            public MemberReferenceHandle Current
            {
                get
                {
                    return MemberReferenceHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    public readonly struct PropertyAccessors
    {
        // Workaround: JIT doesn't generate good code for nested structures, so use uints.

        private readonly int _getterRowId;
        private readonly int _setterRowId;
        private readonly ImmutableArray<MethodDefinitionHandle> _others;

        public MethodDefinitionHandle Getter { get { return MethodDefinitionHandle.FromRowId(_getterRowId); } }
        public MethodDefinitionHandle Setter { get { return MethodDefinitionHandle.FromRowId(_setterRowId); } }
        public ImmutableArray<MethodDefinitionHandle> Others { get { return _others; } }

        internal PropertyAccessors(int getterRowId, int setterRowId, ImmutableArray<MethodDefinitionHandle> others)
        {
            _getterRowId = getterRowId;
            _setterRowId = setterRowId;
            _others = others;
        }
    }

    public readonly struct EventAccessors
    {
        // Workaround: JIT doesn't generate good code for nested structures, so use uints.

        private readonly int _adderRowId;
        private readonly int _removerRowId;
        private readonly int _raiserRowId;
        private readonly ImmutableArray<MethodDefinitionHandle> _others;

        public MethodDefinitionHandle Adder { get { return MethodDefinitionHandle.FromRowId(_adderRowId); } }
        public MethodDefinitionHandle Remover { get { return MethodDefinitionHandle.FromRowId(_removerRowId); } }
        public MethodDefinitionHandle Raiser { get { return MethodDefinitionHandle.FromRowId(_raiserRowId); } }
        public ImmutableArray<MethodDefinitionHandle> Others { get { return _others; } }

        internal EventAccessors(int adderRowId, int removerRowId, int raiserRowId, ImmutableArray<MethodDefinitionHandle> others)
        {
            _adderRowId = adderRowId;
            _removerRowId = removerRowId;
            _raiserRowId = raiserRowId;
            _others = others;
        }
    }

    /// <summary>
    /// Collection of assembly references.
    /// </summary>
    public readonly struct AssemblyReferenceHandleCollection : IReadOnlyCollection<AssemblyReferenceHandle>
    {
        private readonly MetadataReader _reader;

        internal AssemblyReferenceHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
        }

        public int Count
        {
            get
            {
                return _reader.AssemblyRefTable.NumberOfNonVirtualRows + _reader.AssemblyRefTable.NumberOfVirtualRows;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader);
        }

        IEnumerator<AssemblyReferenceHandle> IEnumerable<AssemblyReferenceHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<AssemblyReferenceHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            private int _virtualRowId;

            internal Enumerator(MetadataReader reader)
            {
                _reader = reader;
                _currentRowId = 0;
                _virtualRowId = -1;
            }

            public AssemblyReferenceHandle Current
            {
                get
                {
                    if (_virtualRowId >= 0)
                    {
                        if (_virtualRowId == EnumEnded)
                        {
                            return default(AssemblyReferenceHandle);
                        }

                        return AssemblyReferenceHandle.FromVirtualIndex((AssemblyReferenceHandle.VirtualIndex)((uint)_virtualRowId));
                    }
                    else
                    {
                        return AssemblyReferenceHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                    }
                }
            }

            public bool MoveNext()
            {
                if (_currentRowId < _reader.AssemblyRefTable.NumberOfNonVirtualRows)
                {
                    _currentRowId++;
                    return true;
                }

                if (_virtualRowId < _reader.AssemblyRefTable.NumberOfVirtualRows - 1)
                {
                    _virtualRowId++;
                    return true;
                }

                _currentRowId = EnumEnded;
                _virtualRowId = EnumEnded;
                return false;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="ManifestResourceHandle"/>.
    /// </summary>
    public readonly struct ManifestResourceHandleCollection : IReadOnlyCollection<ManifestResourceHandle>
    {
        private readonly int _lastRowId;

        // Creates collection that represents the entire ManifestResource table.
        internal ManifestResourceHandleCollection(int lastRowId)
        {
            _lastRowId = lastRowId;
        }

        public int Count
        {
            get { return _lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_lastRowId);
        }

        IEnumerator<ManifestResourceHandle> IEnumerable<ManifestResourceHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<ManifestResourceHandle>, IEnumerator
        {
            private readonly int _lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                _lastRowId = lastRowId;
                _currentRowId = 0;
            }

            public ManifestResourceHandle Current
            {
                get
                {
                    return ManifestResourceHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="AssemblyFileHandle"/>.
    /// </summary>
    public readonly struct AssemblyFileHandleCollection : IReadOnlyCollection<AssemblyFileHandle>
    {
        private readonly int _lastRowId;

        // Creates collection that represents the entire AssemblyFile table.
        internal AssemblyFileHandleCollection(int lastRowId)
        {
            _lastRowId = lastRowId;
        }

        public int Count
        {
            get { return _lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_lastRowId);
        }

        IEnumerator<AssemblyFileHandle> IEnumerable<AssemblyFileHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<AssemblyFileHandle>, IEnumerator
        {
            private readonly int _lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                _lastRowId = lastRowId;
                _currentRowId = 0;
            }

            public AssemblyFileHandle Current
            {
                get
                {
                    return AssemblyFileHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (_currentRowId >= _lastRowId)
                {
                    _currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    _currentRowId++;
                    return true;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }
}
