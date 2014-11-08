// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents generic type parameters of a method or type.
    /// </summary>
    public struct GenericParameterHandleCollection : IReadOnlyList<GenericParameterHandle>
    {
        private readonly int firstRowId;
        private readonly ushort count;

        internal GenericParameterHandleCollection(int firstRowId, ushort count)
        {
            this.firstRowId = firstRowId;
            this.count = count;
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public GenericParameterHandle this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    ThrowIndexOutOfRange();
                }

                return GenericParameterHandle.FromRowId((uint)(firstRowId + index));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(firstRowId, firstRowId + count - 1);
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
            private readonly int lastRowId; // inclusive

            // first parameter rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int firstRowId, int lastRowId)
            {
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public GenericParameterHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return GenericParameterHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
    public struct GenericParameterConstraintHandleCollection : IReadOnlyList<GenericParameterConstraintHandle>
    {
        private readonly int firstRowId;
        private readonly ushort count;

        internal GenericParameterConstraintHandleCollection(int firstRowId, ushort count)
        {
            this.firstRowId = firstRowId;
            this.count = count;
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public GenericParameterConstraintHandle this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    ThrowIndexOutOfRange();
                }

                return GenericParameterConstraintHandle.FromRowId((uint)(firstRowId + index));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(firstRowId, firstRowId + count - 1);
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
            private readonly int lastRowId; // inclusive

            // first parameter rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int firstRowId, int lastRowId)
            {
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public GenericParameterConstraintHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return GenericParameterConstraintHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct CustomAttributeHandleCollection : IReadOnlyCollection<CustomAttributeHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal CustomAttributeHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
            this.firstRowId = 1;
            this.lastRowId = (int)reader.CustomAttributeTable.NumberOfRows;
        }

        internal CustomAttributeHandleCollection(MetadataReader reader, Handle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            reader.CustomAttributeTable.GetAttributeRange(handle, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId; // inclusive

            // first custom attribute rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public CustomAttributeHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (reader.CustomAttributeTable.PtrTable != null)
                    {
                        return GetCurrentCustomAttributeIndirect();
                    }
                    else
                    {
                        return CustomAttributeHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                    }
                }
            }

            private CustomAttributeHandle GetCurrentCustomAttributeIndirect()
            {
                return CustomAttributeHandle.FromRowId(
                    reader.CustomAttributeTable.PtrTable[(currentRowId & (int)TokenTypeIds.RIDMask) - 1]);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct DeclarativeSecurityAttributeHandleCollection : IReadOnlyCollection<DeclarativeSecurityAttributeHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal DeclarativeSecurityAttributeHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
            this.firstRowId = 1;
            this.lastRowId = (int)reader.DeclSecurityTable.NumberOfRows;
        }

        internal DeclarativeSecurityAttributeHandleCollection(MetadataReader reader, Handle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            reader.DeclSecurityTable.GetAttributeRange(handle, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId; // inclusive

            // first custom attribute rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public DeclarativeSecurityAttributeHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return DeclarativeSecurityAttributeHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct MethodDefinitionHandleCollection : IReadOnlyCollection<MethodDefinitionHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal MethodDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
            this.firstRowId = 1;
            this.lastRowId = (int)reader.MethodDefTable.NumberOfRows;
        }

        internal MethodDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            this.reader = reader;
            reader.GetMethodRange(containingType, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId; // inclusive

            // first method rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public MethodDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (reader.UseMethodPtrTable)
                    {
                        return GetCurrentMethodIndirect();
                    }
                    else
                    {
                        return MethodDefinitionHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                    }
                }
            }

            private MethodDefinitionHandle GetCurrentMethodIndirect()
            {
                return reader.MethodPtrTable.GetMethodFor(currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct FieldDefinitionHandleCollection : IReadOnlyCollection<FieldDefinitionHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal FieldDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
            this.firstRowId = 1;
            this.lastRowId = (int)reader.FieldTable.NumberOfRows;
        }

        internal FieldDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            this.reader = reader;
            reader.GetFieldRange(containingType, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId; // inclusive

            // first field rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public FieldDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (reader.UseFieldPtrTable)
                    {
                        return GetCurrentFieldIndirect();
                    }
                    else
                    {
                        return FieldDefinitionHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                    }
                }
            }

            private FieldDefinitionHandle GetCurrentFieldIndirect()
            {
                return reader.FieldPtrTable.GetFieldFor(currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct PropertyDefinitionHandleCollection : IReadOnlyCollection<PropertyDefinitionHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal PropertyDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
            this.firstRowId = 1;
            this.lastRowId = (int)reader.PropertyTable.NumberOfRows;
        }

        internal PropertyDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            this.reader = reader;
            reader.GetPropertyRange(containingType, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId; // inclusive

            // first Property rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public PropertyDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (reader.UsePropertyPtrTable)
                    {
                        return GetCurrentPropertyIndirect();
                    }
                    else
                    {
                        return PropertyDefinitionHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                    }
                }
            }

            private PropertyDefinitionHandle GetCurrentPropertyIndirect()
            {
                return reader.PropertyPtrTable.GetPropertyFor(currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct EventDefinitionHandleCollection : IReadOnlyCollection<EventDefinitionHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal EventDefinitionHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
            this.firstRowId = 1;
            this.lastRowId = (int)reader.EventTable.NumberOfRows;
        }

        internal EventDefinitionHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingType.IsNil);

            this.reader = reader;
            reader.GetEventRange(containingType, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId;

            // first rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public EventDefinitionHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (reader.UseEventPtrTable)
                    {
                        return GetCurrentEventIndirect();
                    }
                    else
                    {
                        return EventDefinitionHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                    }
                }
            }

            private EventDefinitionHandle GetCurrentEventIndirect()
            {
                return reader.EventPtrTable.GetEventFor(currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct MethodImplementationHandleCollection : IReadOnlyCollection<MethodImplementationHandle>
    {
        private readonly int firstRowId;
        private readonly int lastRowId;

        internal MethodImplementationHandleCollection(MetadataReader reader, TypeDefinitionHandle containingType)
        {
            Debug.Assert(reader != null);

            if (containingType.IsNil)
            {
                this.firstRowId = 1;
                this.lastRowId = (int)reader.MethodImplTable.NumberOfRows;
            }
            else
            {
                reader.MethodImplTable.GetMethodImplRange(containingType, out firstRowId, out lastRowId);
            }
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(firstRowId, lastRowId);
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
            private readonly int lastRowId; // inclusive

            // first impl rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int firstRowId, int lastRowId)
            {
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public MethodImplementationHandle Current
            {
                get
                {
                    return MethodImplementationHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
    public struct ParameterHandleCollection : IReadOnlyCollection<ParameterHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal ParameterHandleCollection(MetadataReader reader, MethodDefinitionHandle containingMethod)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!containingMethod.IsNil);
            this.reader = reader;

            reader.GetParameterRange(containingMethod, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId; // inclusive

            // first Parameter rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.lastRowId = lastRowId;
                this.currentRowId = firstRowId - 1;
            }

            public ParameterHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.

                    if (reader.UseParamPtrTable)
                    {
                        return GetCurrentParameterIndirect();
                    }
                    else
                    {
                        return ParameterHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                    }
                }
            }

            private ParameterHandle GetCurrentParameterIndirect()
            {
                return reader.ParamPtrTable.GetParamFor(currentRowId & (int)TokenTypeIds.RIDMask);
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct InterfaceImplementationHandleCollection : IReadOnlyCollection<InterfaceImplementationHandle>
    {
        private readonly MetadataReader reader;

        private readonly int firstRowId;
        private readonly int lastRowId;

        internal InterfaceImplementationHandleCollection(MetadataReader reader, TypeDefinitionHandle implementingType)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!implementingType.IsNil);
            this.reader = reader;

            reader.InterfaceImplTable.GetInterfaceImplRange(implementingType, out firstRowId, out lastRowId);
        }

        public int Count
        {
            get
            {
                return lastRowId - firstRowId + 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader, firstRowId, lastRowId);
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
            private readonly MetadataReader reader;
            private readonly int lastRowId; // inclusive

            // first rid - 1: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
            {
                this.reader = reader;
                this.currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
            }

            public InterfaceImplementationHandle Current
            {
                get
                {
                    return InterfaceImplementationHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this code small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
    public struct TypeDefinitionHandleCollection : IReadOnlyCollection<TypeDefinitionHandle>
    {
        private readonly int lastRowId;

        // Creates collection that represents the entire TypeDef table.
        internal TypeDefinitionHandleCollection(int lastRowId)
        {
            this.lastRowId = lastRowId;
        }

        public int Count
        {
            get { return lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(lastRowId);
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
            private readonly int lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                this.lastRowId = lastRowId;
                this.currentRowId = 0;
            }

            public TypeDefinitionHandle Current
            {
                get
                {
                    return TypeDefinitionHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
    public struct TypeReferenceHandleCollection : IReadOnlyCollection<TypeReferenceHandle>
    {
        private readonly int lastRowId;

        // Creates collection that represents the entire TypeRef table.
        internal TypeReferenceHandleCollection(int lastRowId)
        {
            this.lastRowId = lastRowId;
        }

        public int Count
        {
            get { return lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(lastRowId);
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
            private readonly int lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                this.lastRowId = lastRowId;
                this.currentRowId = 0;
            }

            public TypeReferenceHandle Current
            {
                get
                {
                    return TypeReferenceHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
    public struct ExportedTypeHandleCollection : IReadOnlyCollection<ExportedTypeHandle>
    {
        private readonly int lastRowId;

        // Creates collection that represents the entire TypeRef table.
        internal ExportedTypeHandleCollection(int lastRowId)
        {
            this.lastRowId = lastRowId;
        }

        public int Count
        {
            get { return lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(lastRowId);
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
            private readonly int lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                this.lastRowId = lastRowId;
                this.currentRowId = 0;
            }

            public ExportedTypeHandle Current
            {
                get
                {
                    return ExportedTypeHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
    public struct MemberReferenceHandleCollection : IReadOnlyCollection<MemberReferenceHandle>
    {
        private readonly int lastRowId;

        // Creates collection that represents the entire TypeRef table.
        internal MemberReferenceHandleCollection(int lastRowId)
        {
            this.lastRowId = lastRowId;
        }

        public int Count
        {
            get { return lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(lastRowId);
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
            private readonly int lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                this.lastRowId = lastRowId;
                this.currentRowId = 0;
            }

            public MemberReferenceHandle Current
            {
                get
                {
                    return MemberReferenceHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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

    public struct PropertyAccessors
    {
        // Workaround: JIT doesn't generate good code for nested structures, so use uints.

        private readonly uint getterRowId;
        private readonly uint setterRowId;

        public MethodDefinitionHandle Getter { get { return MethodDefinitionHandle.FromRowId(getterRowId); } }
        public MethodDefinitionHandle Setter { get { return MethodDefinitionHandle.FromRowId(setterRowId); } }

        internal PropertyAccessors(uint getterRowId, uint setterRowId)
        {
            this.getterRowId = getterRowId;
            this.setterRowId = setterRowId;
        }
    }

    public struct EventAccessors
    {
        // Workaround: JIT doesn't generate good code for nested structures, so use uints.

        private readonly uint adderRowId;
        private readonly uint removerRowId;
        private readonly uint raiserRowId;

        public MethodDefinitionHandle Adder { get { return MethodDefinitionHandle.FromRowId(adderRowId); } }
        public MethodDefinitionHandle Remover { get { return MethodDefinitionHandle.FromRowId(removerRowId); } }
        public MethodDefinitionHandle Raiser { get { return MethodDefinitionHandle.FromRowId(raiserRowId); } }

        internal EventAccessors(uint adderRowId, uint removerRowId, uint raiserRowId)
        {
            this.adderRowId = adderRowId;
            this.removerRowId = removerRowId;
            this.raiserRowId = raiserRowId;
        }
    }

    /// <summary>
    /// Collection of assembly references.
    /// </summary>
    public struct AssemblyReferenceHandleCollection : IReadOnlyCollection<AssemblyReferenceHandle>
    {
        private readonly MetadataReader reader;

        internal AssemblyReferenceHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
        }

        public int Count
        {
            get
            {
                return reader.AssemblyRefTable.NumberOfNonVirtualRows + reader.AssemblyRefTable.NumberOfVirtualRows;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(reader);
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
            private readonly MetadataReader reader;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            private int virtualRowId;

            internal Enumerator(MetadataReader reader)
            {
                this.reader = reader;
                this.currentRowId = 0;
                this.virtualRowId = -1;
            }

            public AssemblyReferenceHandle Current
            {
                get
                {
                    if (virtualRowId >= 0)
                    {
                        if (virtualRowId == EnumEnded)
                        {
                            return default(AssemblyReferenceHandle);
                        }

                        return AssemblyReferenceHandle.FromVirtualIndex((AssemblyReferenceHandle.VirtualIndex)((uint)virtualRowId));
                    }
                    else
                    {
                        return AssemblyReferenceHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                    }
                }
            }

            public bool MoveNext()
            {
                if (this.currentRowId < reader.AssemblyRefTable.NumberOfNonVirtualRows)
                {
                    this.currentRowId++;
                    return true;
                }

                if (this.virtualRowId < reader.AssemblyRefTable.NumberOfVirtualRows - 1)
                {
                    this.virtualRowId++;
                    return true;
                }

                this.currentRowId = EnumEnded;
                this.virtualRowId = EnumEnded;
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
    public struct ManifestResourceHandleCollection : IReadOnlyCollection<ManifestResourceHandle>
    {
        private readonly int lastRowId;

        // Creates collection that represents the entire ManifestResource table.
        internal ManifestResourceHandleCollection(int lastRowId)
        {
            this.lastRowId = lastRowId;
        }

        public int Count
        {
            get { return lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(lastRowId);
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
            private readonly int lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                this.lastRowId = lastRowId;
                this.currentRowId = 0;
            }

            public ManifestResourceHandle Current
            {
                get
                {
                    return ManifestResourceHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
    public struct AssemblyFileHandleCollection : IReadOnlyCollection<AssemblyFileHandle>
    {
        private readonly int lastRowId;

        // Creates collection that represents the entire AssemblyFile table.
        internal AssemblyFileHandleCollection(int lastRowId)
        {
            this.lastRowId = lastRowId;
        }

        public int Count
        {
            get { return lastRowId; }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(lastRowId);
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
            private readonly int lastRowId;

            // 0: initial state
            // EnumEnded: enumeration ended
            private int currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal Enumerator(int lastRowId)
            {
                this.lastRowId = lastRowId;
                this.currentRowId = 0;
            }

            public AssemblyFileHandle Current
            {
                get
                {
                    return AssemblyFileHandle.FromRowId((uint)currentRowId & TokenTypeIds.RIDMask);
                }
            }

            public bool MoveNext()
            {
                // PERF: keep this method small to enable inlining.

                if (this.currentRowId >= lastRowId)
                {
                    this.currentRowId = EnumEnded;
                    return false;
                }
                else
                {
                    this.currentRowId++;
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
