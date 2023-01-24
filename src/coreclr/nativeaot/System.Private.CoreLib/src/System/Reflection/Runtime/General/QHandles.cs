// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Collection of "qualified handle" tuples.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.Runtime.TypeLoader;

namespace Internal.Reflection.Core
{
    [ReflectionBlocked]
    [CLSCompliant(false)]
    public struct QScopeDefinition : IEquatable<QScopeDefinition>
    {
        public QScopeDefinition(MetadataReader reader, ScopeDefinitionHandle handle)
        {
            _reader = reader;
            _handle = handle;
        }

        public MetadataReader Reader { get { return _reader; } }
        public ScopeDefinitionHandle Handle { get { return _handle; } }
        public ScopeDefinition ScopeDefinition
        {
            get
            {
                return _handle.GetScopeDefinition(_reader);
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is QScopeDefinition))
                return false;
            return Equals((QScopeDefinition)obj);
        }

        public bool Equals(QScopeDefinition other)
        {
            if (!(_reader == other._reader))
                return false;
            if (!(_handle.Equals(other._handle)))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return _handle.GetHashCode();
        }

        private readonly MetadataReader _reader;
        private readonly ScopeDefinitionHandle _handle;
    }
}

namespace System.Reflection.Runtime.General
{
    [ReflectionBlocked]
    [CLSCompliant(false)]
    public struct QHandle : IEquatable<QHandle>
    {
        public QHandle(MetadataReader reader, Handle handle)
        {
            _reader = reader;
            _handle = handle;
        }

        public MetadataReader Reader { get { return _reader; } }
        public Handle Handle { get { return _handle; } }

        public override bool Equals(object obj)
        {
            if (!(obj is QHandle))
                return false;
            return Equals((QHandle)obj);
        }

        public bool Equals(QHandle other)
        {
            if (!(_reader == other._reader))
                return false;
            if (!(_handle.Equals(other._handle)))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return _handle.GetHashCode();
        }

        private readonly MetadataReader _reader;
        private readonly Handle _handle;
    }

    [ReflectionBlocked]
    [CLSCompliant(false)]
    public partial struct QMethodDefinition
    {
        private QMethodDefinition(object reader, int token)
        {
            _reader = reader;
            _handle = token;
        }

        public static QMethodDefinition FromObjectAndInt(object reader, int token)
        {
            return new QMethodDefinition(reader, token);
        }

        public object Reader { get { return _reader; } }
        public int Token { get { return _handle; } }

        public bool IsValid { get { return _reader == null; } }

        public static QMethodDefinition Null => default;

        private readonly object _reader;
        private readonly int _handle;
    }

    [ReflectionBlocked]
    [CLSCompliant(false)]
    public partial struct QTypeDefinition
    {
        public object Reader { get { return _reader; } }
        public int Token { get { return _handle; } }

        public bool IsValid { get { return _reader == null; } }

        public static QTypeDefinition Null => default;

        private readonly object _reader;
        private readonly int _handle;
    }


    [ReflectionBlocked]
    [CLSCompliant(false)]
    public partial struct QTypeDefRefOrSpec
    {
        public object Reader { get { return _reader; } }
        public int Handle { get { return _handle; } }

        public bool IsValid { get { return _reader == null; } }

        public static QTypeDefRefOrSpec Null => default;

        private readonly object _reader;
        private readonly int _handle;
    }

    [ReflectionBlocked]
    [CLSCompliant(false)]
    public struct QGenericParameter : IEquatable<QGenericParameter>
    {
        public QGenericParameter(MetadataReader reader, GenericParameterHandle handle)
        {
            _reader = reader;
            _handle = handle;
        }

        public MetadataReader Reader { get { return _reader; } }
        public GenericParameterHandle Handle { get { return _handle; } }

        public override bool Equals(object obj)
        {
            if (!(obj is QGenericParameter))
                return false;
            return Equals((QGenericParameter)obj);
        }

        public bool Equals(QGenericParameter other)
        {
            if (!(_reader == other._reader))
                return false;
            if (!(_handle.Equals(other._handle)))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return _handle.GetHashCode();
        }

        private readonly MetadataReader _reader;
        private readonly GenericParameterHandle _handle;
    }
}
