// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;


namespace System.Xml.Serialization.LegacyNetCF
{
    ///<internalonly/>
    public abstract class XmlMapping
    {
        /// <summary>
        /// Whether this mapping represents encoded semantics.  (as in rpc/encoded)
        /// </summary>
        private bool _isSoap;
        internal XmlSerializationReflector serializationReflector;
        internal LogicalType logicalType;

        internal XmlMapping(LogicalType lType, XmlSerializationReflector reflector)
            :
            this(lType, reflector, false)
        {
        }

        internal XmlMapping(LogicalType lType, XmlSerializationReflector reflector, bool isSoap)
        {
            System.Diagnostics.Debug.Assert(null != reflector, "The reflector parameter can not be null.");
            System.Diagnostics.Debug.Assert(null != lType, "The logicalType parameter can not be null.");

            serializationReflector = reflector;
            logicalType = lType;
        }

        /// <summary>
        /// The reflector that was used to generate this mapping.
        /// </summary>
        internal XmlSerializationReflector Reflector
        {
            get { return serializationReflector; }
        }

        /// <summary>
        /// The root type described by this mapping.
        /// </summary>
        internal LogicalType LogicalType
        {
            get { return logicalType; }
        }

        /// <summary>
        /// The name of the root element according to its TypeAccessor.
        /// </summary>
        public string ElementName
        {
            get { return logicalType.TypeAccessor.Name; }
        }

        /// <summary>
        /// The namespace of the root element according to its TypeAccessor.
        /// </summary>
        public string Namespace
        {
            get { return logicalType.TypeAccessor.Namespace; }
        }

        /// <summary>
        /// Whether this mapping represents encoded semantics.  (as in rpc/encoded)
        /// </summary>
        internal bool IsSoap
        {
            get { return _isSoap; }
            set { _isSoap = value; }
        }

        public string XsdElementName
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public void SetKey(string key)
        {
            throw new NotSupportedException();
        }
    }
}
