// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct DocumentHandleCollection : IReadOnlyCollection<DocumentHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal DocumentHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;

            _firstRowId = 1;
            _lastRowId = reader.DocumentTable.NumberOfRows;
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

        IEnumerator<DocumentHandle> IEnumerable<DocumentHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<DocumentHandle>, IEnumerator
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

            public DocumentHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return DocumentHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
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

    public readonly struct MethodDebugInformationHandleCollection : IReadOnlyCollection<MethodDebugInformationHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal MethodDebugInformationHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;

            _firstRowId = 1;
            _lastRowId = reader.MethodDebugInformationTable.NumberOfRows;
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

        IEnumerator<MethodDebugInformationHandle> IEnumerable<MethodDebugInformationHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<MethodDebugInformationHandle>, IEnumerator
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

            public MethodDebugInformationHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return MethodDebugInformationHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
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

    public readonly struct LocalScopeHandleCollection : IReadOnlyCollection<LocalScopeHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal LocalScopeHandleCollection(MetadataReader reader, int methodDefinitionRowId)
        {
            Debug.Assert(reader != null);
            _reader = reader;

            if (methodDefinitionRowId == 0)
            {
                _firstRowId = 1;
                _lastRowId = reader.LocalScopeTable.NumberOfRows;
            }
            else
            {
                reader.LocalScopeTable.GetLocalScopeRange(methodDefinitionRowId, out _firstRowId, out _lastRowId);
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
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<LocalScopeHandle> IEnumerable<LocalScopeHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<LocalScopeHandle>, IEnumerator
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

            public LocalScopeHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return LocalScopeHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
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

        public struct ChildrenEnumerator : IEnumerator<LocalScopeHandle>, IEnumerator
        {
            private readonly MetadataReader _reader;
            private readonly int _parentEndOffset;
            private readonly int _parentRowId;
            private readonly MethodDefinitionHandle _parentMethodRowId;

            // parent rid: initial state
            // EnumEnded: enumeration ended
            private int _currentRowId;

            // greater than any RowId and with last 24 bits clear, so that Current returns nil token
            private const int EnumEnded = (int)TokenTypeIds.RIDMask + 1;

            internal ChildrenEnumerator(MetadataReader reader, int parentRowId)
            {
                _reader = reader;
                _parentEndOffset = reader.LocalScopeTable.GetEndOffset(parentRowId);
                _parentMethodRowId = reader.LocalScopeTable.GetMethod(parentRowId);
                _currentRowId = 0;
                _parentRowId = parentRowId;
            }

            public LocalScopeHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return LocalScopeHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
                }
            }

            public bool MoveNext()
            {
                int currentRowId = _currentRowId;
                if (currentRowId == EnumEnded)
                {
                    return false;
                }

                int currentEndOffset;
                int nextRowId;
                if (currentRowId == 0)
                {
                    currentEndOffset = -1;
                    nextRowId = _parentRowId + 1;
                }
                else
                {
                    currentEndOffset = _reader.LocalScopeTable.GetEndOffset(currentRowId);
                    nextRowId = currentRowId + 1;
                }

                int rowCount = _reader.LocalScopeTable.NumberOfRows;

                while (true)
                {
                    if (nextRowId > rowCount ||
                        _parentMethodRowId != _reader.LocalScopeTable.GetMethod(nextRowId))
                    {
                        _currentRowId = EnumEnded;
                        return false;
                    }

                    int nextEndOffset = _reader.LocalScopeTable.GetEndOffset(nextRowId);

                    // If the end of the next scope is lesser than or equal the current end 
                    // then it's nested into the current scope and thus not a child of 
                    // the current scope parent.
                    if (nextEndOffset > currentEndOffset)
                    {
                        // If the end of the next scope is greater than the parent end,
                        // then we ran out of the children.
                        if (nextEndOffset > _parentEndOffset)
                        {
                            _currentRowId = EnumEnded;
                            return false;
                        }

                        _currentRowId = nextRowId;
                        return true;
                    }

                    nextRowId++;
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

    public readonly struct LocalVariableHandleCollection : IReadOnlyCollection<LocalVariableHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal LocalVariableHandleCollection(MetadataReader reader, LocalScopeHandle scope)
        {
            Debug.Assert(reader != null);
            _reader = reader;

            if (scope.IsNil)
            {
                _firstRowId = 1;
                _lastRowId = reader.LocalVariableTable.NumberOfRows;
            }
            else
            {
                reader.GetLocalVariableRange(scope, out _firstRowId, out _lastRowId);
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
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<LocalVariableHandle> IEnumerable<LocalVariableHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<LocalVariableHandle>, IEnumerator
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

            public LocalVariableHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return LocalVariableHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
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

    public readonly struct LocalConstantHandleCollection : IReadOnlyCollection<LocalConstantHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal LocalConstantHandleCollection(MetadataReader reader, LocalScopeHandle scope)
        {
            Debug.Assert(reader != null);
            _reader = reader;

            if (scope.IsNil)
            {
                _firstRowId = 1;
                _lastRowId = reader.LocalConstantTable.NumberOfRows;
            }
            else
            {
                reader.GetLocalConstantRange(scope, out _firstRowId, out _lastRowId);
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
            return new Enumerator(_reader, _firstRowId, _lastRowId);
        }

        IEnumerator<LocalConstantHandle> IEnumerable<LocalConstantHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<LocalConstantHandle>, IEnumerator
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

            public LocalConstantHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return LocalConstantHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
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

    public readonly struct ImportScopeCollection : IReadOnlyCollection<ImportScopeHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal ImportScopeCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;

            _firstRowId = 1;
            _lastRowId = reader.ImportScopeTable.NumberOfRows;
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

        IEnumerator<ImportScopeHandle> IEnumerable<ImportScopeHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<ImportScopeHandle>, IEnumerator
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

            public ImportScopeHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return ImportScopeHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
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

    public readonly struct CustomDebugInformationHandleCollection : IReadOnlyCollection<CustomDebugInformationHandle>
    {
        private readonly MetadataReader _reader;

        private readonly int _firstRowId;
        private readonly int _lastRowId;

        internal CustomDebugInformationHandleCollection(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;

            _firstRowId = 1;
            _lastRowId = reader.CustomDebugInformationTable.NumberOfRows;
        }

        internal CustomDebugInformationHandleCollection(MetadataReader reader, EntityHandle handle)
        {
            Debug.Assert(reader != null);

            _reader = reader;
            reader.CustomDebugInformationTable.GetRange(handle, out _firstRowId, out _lastRowId);
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

        IEnumerator<CustomDebugInformationHandle> IEnumerable<CustomDebugInformationHandle>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<CustomDebugInformationHandle>, IEnumerator
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

            public CustomDebugInformationHandle Current
            {
                get
                {
                    // PERF: keep this code small to enable inlining.
                    return CustomDebugInformationHandle.FromRowId((int)(_currentRowId & TokenTypeIds.RIDMask));
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
}
