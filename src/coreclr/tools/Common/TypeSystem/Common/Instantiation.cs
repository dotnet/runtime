// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents a generic instantiation - a collection of generic parameters
    /// or arguments of a generic type or a generic method.
    /// </summary>
    public struct Instantiation : IEquatable<Instantiation>
    {
        private TypeDesc[] _genericParameters;

        public Instantiation(params TypeDesc[] genericParameters)
        {
            _genericParameters = genericParameters;
        }

        [IndexerName("GenericParameters")]
        public TypeDesc this[int index]
        {
            get
            {
                return _genericParameters[index];
            }
        }

        public static implicit operator ReadOnlySpan<TypeDesc>(Instantiation instantiation)
        {
            return instantiation._genericParameters;
        }

        public int Length
        {
            get
            {
                return _genericParameters.Length;
            }
        }

        public bool IsNull
        {
            get
            {
                return _genericParameters == null;
            }
        }

        /// <summary>
        /// Combines the given generic definition's hash code with the hashes
        /// of the generic parameters in this instantiation
        /// </summary>
        public int ComputeGenericInstanceHashCode(int genericDefinitionHashCode)
        {
            return Internal.NativeFormat.TypeHashingAlgorithms.ComputeGenericInstanceHashCode(genericDefinitionHashCode, _genericParameters);
        }

        public static readonly Instantiation Empty = new Instantiation(TypeDesc.EmptyTypes);

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_genericParameters);
        }

        public override string ToString()
        {
            if (_genericParameters == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _genericParameters.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(_genericParameters[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Enumerator for iterating over the types in an instantiation
        /// </summary>
        public struct Enumerator
        {
            private TypeDesc[] _collection;
            private int _currentIndex;

            public Enumerator(TypeDesc[] collection)
            {
                _collection = collection;
                _currentIndex = -1;
            }

            public TypeDesc Current
            {
                get
                {
                    return _collection[_currentIndex];
                }
            }

            public bool MoveNext()
            {
                _currentIndex++;
                if (_currentIndex >= _collection.Length)
                {
                    return false;
                }
                return true;
            }
        }

        public bool Equals(Instantiation other)
        {
            if (_genericParameters.Length != other._genericParameters.Length)
                return false;

            for (int i = 0; i < _genericParameters.Length; i++)
            {
                if (_genericParameters[i] != other._genericParameters[i])
                    return false;
            }
            return true;
        }
        public override bool Equals(object o)
        {
            if (o is Instantiation inst)
                return Equals(inst);
            return false;
        }
        public override int GetHashCode() => ComputeGenericInstanceHashCode(1);
    }
}
