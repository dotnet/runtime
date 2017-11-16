// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data.Common;

namespace Microsoft.SqlServer.Server
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class SqlUserDefinedAggregateAttribute : Attribute
    {
        private int _maxByteSize;
        private bool _isInvariantToDup;
        private bool _isInvariantToNulls;
        private bool _isInvariantToOrder = true;
        private bool _isNullIfEmpty;
        private Format _format;
        private string _name;

        // The maximum value for the maxbytesize field, in bytes.
        public const int MaxByteSizeValue = 8000;

        // A required attribute on all UD Aggs, used to indicate that the
        // given type is a UD Agg, and its storage format.
        public SqlUserDefinedAggregateAttribute(Format format)
        {
            switch (format)
            {
                case Format.Unknown:
                    throw ADP.NotSupportedUserDefinedTypeSerializationFormat(format, nameof(format));
                case Format.Native:
                case Format.UserDefined:
                    _format = format;
                    break;
                default:
                    throw ADP.InvalidUserDefinedTypeSerializationFormat(format);
            }
        }

        // The maximum size of this instance, in bytes. Does not have to be
        // specified for Native format serialization. The maximum value
        // for this property is specified by MaxByteSizeValue.
        public int MaxByteSize
        {
            get
            {
                return _maxByteSize;
            }
            set
            {
                // MaxByteSize of -1 means 2GB and is valid, as well as 0 to MaxByteSizeValue
                if (value < -1 || value > MaxByteSizeValue)
                {
                    throw ADP.ArgumentOutOfRange(SR.GetString(SR.SQLUDT_MaxByteSizeValue), nameof(MaxByteSize), value);
                }
                _maxByteSize = value;
            }
        }

        public bool IsInvariantToDuplicates
        {
            get
            {
                return _isInvariantToDup;
            }
            set
            {
                _isInvariantToDup = value;
            }
        }

        public bool IsInvariantToNulls
        {
            get
            {
                return _isInvariantToNulls;
            }
            set
            {
                _isInvariantToNulls = value;
            }
        }

        public bool IsInvariantToOrder
        {
            get
            {
                return _isInvariantToOrder;
            }
            set
            {
                _isInvariantToOrder = value;
            }
        }

        public bool IsNullIfEmpty
        {
            get
            {
                return _isNullIfEmpty;
            }
            set
            {
                _isNullIfEmpty = value;
            }
        }

        // The on-disk format for this type.
        public Format Format => _format;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
    }
}