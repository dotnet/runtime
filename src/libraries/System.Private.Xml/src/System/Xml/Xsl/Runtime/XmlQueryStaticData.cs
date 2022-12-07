// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Xsl.IlGen;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Contains all static data that is used by the runtime.
    /// </summary>
    internal sealed class XmlQueryStaticData
    {
        // Name of the field to serialize to
        public const string DataFieldName = "staticData";
        public const string TypesFieldName = "ebTypes";

        // Format version marker to support versioning: (major << 8) | minor
        private const int CurrentFormatVersion = (0 << 8) | 0;

        private readonly XmlWriterSettings _defaultWriterSettings;
        private readonly IList<WhitespaceRule>? _whitespaceRules;
        private readonly string[]? _names;
        private readonly StringPair[][]? _prefixMappingsList;
        private readonly Int32Pair[]? _filters;
        private readonly XmlQueryType[]? _types;
        private readonly XmlCollation[]? _collations;
        private readonly string[]? _globalNames;
        private readonly EarlyBoundInfo[]? _earlyBound;

        /// <summary>
        /// Constructor.
        /// </summary>
        [RequiresUnreferencedCode("This method will create a copy that uses earlybound types which cannot be statically analyzed.")]
        public XmlQueryStaticData(XmlWriterSettings defaultWriterSettings, IList<WhitespaceRule> whitespaceRules, StaticDataManager staticData)
        {
            Debug.Assert(defaultWriterSettings != null && staticData != null);
            _defaultWriterSettings = defaultWriterSettings;
            _whitespaceRules = whitespaceRules;
            _names = staticData.Names;
            _prefixMappingsList = staticData.PrefixMappingsList;
            _filters = staticData.NameFilters;
            _types = staticData.XmlTypes;
            _collations = staticData.Collations;
            _globalNames = staticData.GlobalNames;
            _earlyBound = staticData.EarlyBound;

#if DEBUG
            // Round-trip check
            byte[] data;
            Type[]? ebTypes;
            this.GetObjectData(out data, out ebTypes);
            XmlQueryStaticData copy = new XmlQueryStaticData(data, ebTypes);

            _defaultWriterSettings = copy._defaultWriterSettings;
            _whitespaceRules = copy._whitespaceRules;
            _names = copy._names;
            _prefixMappingsList = copy._prefixMappingsList;
            _filters = copy._filters;
            _types = copy._types;
            _collations = copy._collations;
            _globalNames = copy._globalNames;
            _earlyBound = copy._earlyBound;
#endif
        }

        /// <summary>
        /// Deserialize XmlQueryStaticData object from a byte array.
        /// </summary>
        [RequiresUnreferencedCode("This method will create EarlyBoundInfo from passed in ebTypes array which cannot be statically analyzed.")]
        public XmlQueryStaticData(byte[] data, Type[]? ebTypes)
        {
            MemoryStream dataStream = new MemoryStream(data, writable: false);
            XmlQueryDataReader dataReader = new XmlQueryDataReader(dataStream);
            int length;

            // Read a format version
            int formatVersion = dataReader.Read7BitEncodedInt();

            // Changes in the major part of version are not supported
            if ((formatVersion & ~0xff) > CurrentFormatVersion)
                throw new NotSupportedException();

            // XmlWriterSettings defaultWriterSettings;
            _defaultWriterSettings = new XmlWriterSettings(dataReader);

            // IList<WhitespaceRule> whitespaceRules;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _whitespaceRules = new WhitespaceRule[length];
                for (int idx = 0; idx < length; idx++)
                {
                    _whitespaceRules[idx] = new WhitespaceRule(dataReader);
                }
            }

            // string[] names;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _names = new string[length];
                for (int idx = 0; idx < length; idx++)
                {
                    _names[idx] = dataReader.ReadString();
                }
            }

            // StringPair[][] prefixMappingsList;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _prefixMappingsList = new StringPair[length][];
                for (int idx = 0; idx < length; idx++)
                {
                    int length2 = dataReader.ReadInt32();
                    _prefixMappingsList[idx] = new StringPair[length2];
                    for (int idx2 = 0; idx2 < length2; idx2++)
                    {
                        _prefixMappingsList[idx][idx2] = new StringPair(dataReader.ReadString(), dataReader.ReadString());
                    }
                }
            }

            // Int32Pair[] filters;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _filters = new Int32Pair[length];
                for (int idx = 0; idx < length; idx++)
                {
                    _filters[idx] = new Int32Pair(dataReader.Read7BitEncodedInt(), dataReader.Read7BitEncodedInt());
                }
            }

            // XmlQueryType[] types;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _types = new XmlQueryType[length];
                for (int idx = 0; idx < length; idx++)
                {
                    _types[idx] = XmlQueryTypeFactory.Deserialize(dataReader);
                }
            }

            // XmlCollation[] collations;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _collations = new XmlCollation[length];
                for (int idx = 0; idx < length; idx++)
                {
                    _collations[idx] = new XmlCollation(dataReader);
                }
            }

            // string[] globalNames;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _globalNames = new string[length];
                for (int idx = 0; idx < length; idx++)
                {
                    _globalNames[idx] = dataReader.ReadString();
                }
            }

            // EarlyBoundInfo[] earlyBound;
            length = dataReader.ReadInt32();
            if (length != 0)
            {
                _earlyBound = new EarlyBoundInfo[length];
                for (int idx = 0; idx < length; idx++)
                {
                    _earlyBound[idx] = new EarlyBoundInfo(dataReader.ReadString(), ebTypes![idx]);
                }
            }

            Debug.Assert(formatVersion != CurrentFormatVersion || dataReader.Read() == -1, "Extra data at the end of the stream");
            dataReader.Dispose();
        }

        /// <summary>
        /// Serialize XmlQueryStaticData object into a byte array.
        /// </summary>
        public void GetObjectData(out byte[] data, out Type[]? ebTypes)
        {
            MemoryStream dataStream = new MemoryStream(4096);
            XmlQueryDataWriter dataWriter = new XmlQueryDataWriter(dataStream);

            // First put the format version
            dataWriter.Write7BitEncodedInt(CurrentFormatVersion);

            // XmlWriterSettings defaultWriterSettings;
            _defaultWriterSettings.GetObjectData(dataWriter);

            // IList<WhitespaceRule> whitespaceRules;
            if (_whitespaceRules == null)
            {
                dataWriter.Write(0);
            }
            else
            {
                dataWriter.Write(_whitespaceRules.Count);
                foreach (WhitespaceRule rule in _whitespaceRules)
                {
                    rule.GetObjectData(dataWriter);
                }
            }

            // string[] names;
            if (_names == null)
            {
                dataWriter.Write(0);
            }
            else
            {
                dataWriter.Write(_names.Length);
                foreach (string name in _names)
                {
                    dataWriter.Write(name);
                }
            }

            // StringPair[][] prefixMappingsList;
            if (_prefixMappingsList == null)
            {
                dataWriter.Write(0);
            }
            else
            {
                dataWriter.Write(_prefixMappingsList.Length);
                foreach (StringPair[] mappings in _prefixMappingsList)
                {
                    dataWriter.Write(mappings.Length);
                    foreach (StringPair mapping in mappings)
                    {
                        dataWriter.Write(mapping.Left);
                        dataWriter.Write(mapping.Right);
                    }
                }
            }

            // Int32Pair[] filters;
            if (_filters == null)
            {
                dataWriter.Write(0);
            }
            else
            {
                dataWriter.Write(_filters.Length);
                foreach (Int32Pair filter in _filters)
                {
                    dataWriter.Write7BitEncodedInt(filter.Left);
                    dataWriter.Write7BitEncodedInt(filter.Right);
                }
            }

            // XmlQueryType[] types;
            if (_types == null)
            {
                dataWriter.Write(0);
            }
            else
            {
                dataWriter.Write(_types.Length);
                foreach (XmlQueryType type in _types)
                {
                    XmlQueryTypeFactory.Serialize(dataWriter, type);
                }
            }

            // XmlCollation[] collations;
            if (_collations == null)
            {
                dataWriter.Write(0);
            }
            else
            {
                dataWriter.Write(_collations.Length);
                foreach (XmlCollation collation in _collations)
                {
                    collation.GetObjectData(dataWriter);
                }
            }

            // string[] globalNames;
            if (_globalNames == null)
            {
                dataWriter.Write(0);
            }
            else
            {
                dataWriter.Write(_globalNames.Length);
                foreach (string name in _globalNames)
                {
                    dataWriter.Write(name);
                }
            }

            // EarlyBoundInfo[] earlyBound;
            if (_earlyBound == null)
            {
                dataWriter.Write(0);
                ebTypes = null;
            }
            else
            {
                dataWriter.Write(_earlyBound.Length);
                ebTypes = new Type[_earlyBound.Length];
                int idx = 0;
                foreach (EarlyBoundInfo info in _earlyBound)
                {
                    dataWriter.Write(info.NamespaceUri);
                    ebTypes[idx++] = info.EarlyBoundType;
                }
            }

            dataWriter.Dispose();
            data = dataStream.ToArray();
        }

        /// <summary>
        /// Return the default writer settings.
        /// </summary>
        public XmlWriterSettings DefaultWriterSettings
        {
            get { return _defaultWriterSettings; }
        }

        /// <summary>
        /// Return the rules used for whitespace stripping/preservation.
        /// </summary>
        public IList<WhitespaceRule>? WhitespaceRules
        {
            get { return _whitespaceRules; }
        }

        /// <summary>
        /// Return array of names used by this query.
        /// </summary>
        public string[]? Names
        {
            get { return _names; }
        }

        /// <summary>
        /// Return array of prefix mappings used by this query.
        /// </summary>
        public StringPair[][]? PrefixMappingsList
        {
            get { return _prefixMappingsList; }
        }

        /// <summary>
        /// Return array of name filter specifications used by this query.
        /// </summary>
        public Int32Pair[]? Filters
        {
            get { return _filters; }
        }

        /// <summary>
        /// Return array of types used by this query.
        /// </summary>
        public XmlQueryType[]? Types
        {
            get { return _types; }
        }

        /// <summary>
        /// Return array of collations used by this query.
        /// </summary>
        public XmlCollation[]? Collations
        {
            get { return _collations; }
        }

        /// <summary>
        /// Return names of all global variables and parameters used by this query.
        /// </summary>
        public string[]? GlobalNames
        {
            get { return _globalNames; }
        }

        /// <summary>
        /// Return array of early bound object information related to this query.
        /// </summary>
        public EarlyBoundInfo[]? EarlyBound
        {
            get { return _earlyBound; }
        }
    }

    /// <summary>
    /// Subclass of BinaryReader used to serialize query static data.
    /// </summary>
    internal sealed class XmlQueryDataReader : BinaryReader
    {
        public XmlQueryDataReader(Stream input) : base(input) { }

        /// <summary>
        /// Read a string value from the stream. Value can be null.
        /// </summary>
        public string? ReadStringQ()
        {
            return ReadBoolean() ? ReadString() : null;
        }

        /// <summary>
        /// Read a signed byte value from the stream and check if it belongs to the given diapason.
        /// </summary>
        public sbyte ReadSByte(sbyte minValue, sbyte maxValue)
        {
            sbyte value = ReadSByte();
            if (value < minValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));
            if (maxValue < value)
                throw new ArgumentOutOfRangeException(nameof(maxValue));

            return value;
        }
    }

    /// <summary>
    /// Subclass of BinaryWriter used to deserialize query static data.
    /// </summary>
    internal sealed class XmlQueryDataWriter : BinaryWriter
    {
        public XmlQueryDataWriter(Stream output) : base(output) { }

        /// <summary>
        /// Write a string value to the stream. Value can be null.
        /// </summary>
        public void WriteStringQ(string? value)
        {
            Write(value != null);
            if (value != null)
            {
                Write(value);
            }
        }
    }
}
