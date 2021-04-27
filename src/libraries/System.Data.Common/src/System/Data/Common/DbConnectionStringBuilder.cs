// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    public class DbConnectionStringBuilder : IDictionary, ICustomTypeDescriptor
    {
        // keyword->value currently listed in the connection string
        private Dictionary<string, object>? _currentValues;

        // cached connectionstring to avoid constant rebuilding
        // and to return a user's connectionstring as is until editing occurs
        private string? _connectionString = string.Empty;

        private PropertyDescriptorCollection? _propertyDescriptors;
        private bool _browsableConnectionString = true;
        private readonly bool _useOdbcRules;

        private static int s_objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        public DbConnectionStringBuilder()
        {
        }

        public DbConnectionStringBuilder(bool useOdbcRules)
        {
            _useOdbcRules = useOdbcRules;
        }

        private ICollection Collection
        {
            get { return CurrentValues; }
        }
        private IDictionary Dictionary
        {
            get { return CurrentValues; }
        }
        private Dictionary<string, object> CurrentValues
        {
            get
            {
                Dictionary<string, object>? values = _currentValues;
                if (null == values)
                {
                    values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    _currentValues = values;
                }
                return values;
            }
        }

        object? IDictionary.this[object keyword]
        {
            // delegate to this[string keyword]
            get { return this[ObjectToString(keyword)]; }
            set { this[ObjectToString(keyword)] = value!; }
        }

        [Browsable(false)]
        [AllowNull]
        public virtual object this[string keyword]
        {
            get
            {
                DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.get_Item|API> {0}, keyword='{1}'", ObjectID, keyword);
                ADP.CheckArgumentNull(keyword, nameof(keyword));
                object? value;
                if (CurrentValues.TryGetValue(keyword, out value))
                {
                    return value;
                }
                throw ADP.KeywordNotSupported(keyword);
            }
            set
            {
                ADP.CheckArgumentNull(keyword, nameof(keyword));
                bool flag = false;
                if (null != value)
                {
                    string keyvalue = DbConnectionStringBuilderUtil.ConvertToString(value);
                    DbConnectionOptions.ValidateKeyValuePair(keyword, keyvalue);

                    flag = CurrentValues.ContainsKey(keyword);

                    // store keyword/value pair
                    CurrentValues[keyword] = keyvalue;
                }
                else
                {
                    flag = Remove(keyword);
                }
                _connectionString = null;
                if (flag)
                {
                    _propertyDescriptors = null;
                }
            }
        }

        [Browsable(false)]
        [DesignOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool BrowsableConnectionString
        {
            get
            {
                return _browsableConnectionString;
            }
            set
            {
                _browsableConnectionString = value;
                _propertyDescriptors = null;
            }
        }

        [RefreshPropertiesAttribute(RefreshProperties.All)]
        [AllowNull]
        public string ConnectionString
        {
            get
            {
                DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.get_ConnectionString|API> {0}", ObjectID);
                string? connectionString = _connectionString;
                if (null == connectionString)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (string keyword in Keys)
                    {
                        Debug.Assert(keyword != null);
                        object? value;
                        if (ShouldSerialize(keyword) && TryGetValue(keyword, out value))
                        {
                            string? keyvalue = ConvertValueToString(value);
                            AppendKeyValuePair(builder, keyword, keyvalue, _useOdbcRules);
                        }
                    }
                    connectionString = builder.ToString();
                    _connectionString = connectionString;
                }
                return connectionString;
            }
            set
            {
                DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.set_ConnectionString|API> {0}", ObjectID);
                DbConnectionOptions constr = new DbConnectionOptions(value, null, _useOdbcRules);
                string originalValue = ConnectionString;
                Clear();
                try
                {
                    for (NameValuePair? pair = constr._keyChain; null != pair; pair = pair.Next)
                    {
                        if (null != pair.Value)
                        {
                            this[pair.Name] = pair.Value;
                        }
                        else
                        {
                            Remove(pair.Name);
                        }
                    }
                    _connectionString = null;
                }
                catch (ArgumentException)
                {
                    // restore original string
#pragma warning disable CA2011 // avoid infinite recursion, but this isn't that; it's restoring the original string
                    ConnectionString = originalValue;
#pragma warning restore CA2011
                    _connectionString = originalValue;
                    throw;
                }
            }
        }

        [Browsable(false)]
        public virtual int Count
        {
            get { return CurrentValues.Count; }
        }

        [Browsable(false)]
        public bool IsReadOnly
        {
            get { return false; }
        }

        [Browsable(false)]
        public virtual bool IsFixedSize
        {
            get { return false; }
        }
        bool ICollection.IsSynchronized
        {
            get { return Collection.IsSynchronized; }
        }

        [Browsable(false)]
        public virtual ICollection Keys
        {
            get
            {
                DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Keys|API> {0}", ObjectID);
                return Dictionary.Keys;
            }
        }

        internal int ObjectID
        {
            get
            {
                return _objectID;
            }
        }

        object ICollection.SyncRoot
        {
            get { return Collection.SyncRoot; }
        }

        [Browsable(false)]
        public virtual ICollection Values
        {
            get
            {
                DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Values|API> {0}", ObjectID);
                ICollection<string> keys = (ICollection<string>)Keys;
                IEnumerator<string> keylist = keys.GetEnumerator();
                object[] values = new object[keys.Count];
                for (int i = 0; i < values.Length; ++i)
                {
                    keylist.MoveNext();
                    values[i] = this[keylist.Current];
                    Debug.Assert(null != values[i], "null value " + keylist.Current);
                }
                return new ReadOnlyCollection<object>(values);
            }
        }

        internal virtual string? ConvertValueToString(object? value)
        {
            return (value == null) ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        void IDictionary.Add(object keyword, object? value)
        {
            Add(ObjectToString(keyword), value!);
        }
        public void Add(string keyword, object value)
        {
            this[keyword] = value;
        }

        public static void AppendKeyValuePair(StringBuilder builder, string keyword, string? value)
        {
            DbConnectionOptions.AppendKeyValuePairBuilder(builder, keyword, value, false);
        }

        public static void AppendKeyValuePair(StringBuilder builder, string keyword, string? value, bool useOdbcRules)
        {
            DbConnectionOptions.AppendKeyValuePairBuilder(builder, keyword, value, useOdbcRules);
        }

        public virtual void Clear()
        {
            DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Clear|API>");
            _connectionString = string.Empty;
            _propertyDescriptors = null;
            CurrentValues.Clear();
        }

        protected internal void ClearPropertyDescriptors()
        {
            _propertyDescriptors = null;
        }

        // does the keyword exist as a strongly typed keyword or as a stored value
        bool IDictionary.Contains(object keyword)
        {
            return ContainsKey(ObjectToString(keyword));
        }
        public virtual bool ContainsKey(string keyword)
        {
            ADP.CheckArgumentNull(keyword, nameof(keyword));
            return CurrentValues.ContainsKey(keyword);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.ICollection.CopyTo|API> {0}", ObjectID);
            Collection.CopyTo(array, index);
        }

        public virtual bool EquivalentTo(DbConnectionStringBuilder connectionStringBuilder)
        {
            ADP.CheckArgumentNull(connectionStringBuilder, nameof(connectionStringBuilder));

            DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.EquivalentTo|API> {0}, connectionStringBuilder={1}", ObjectID, connectionStringBuilder.ObjectID);
            if ((GetType() != connectionStringBuilder.GetType()) || (CurrentValues.Count != connectionStringBuilder.CurrentValues.Count))
            {
                return false;
            }
            object? value;
            foreach (KeyValuePair<string, object> entry in CurrentValues)
            {
                if (!connectionStringBuilder.CurrentValues.TryGetValue(entry.Key, out value) || !entry.Value.Equals(value))
                {
                    return false;
                }
            }
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.IEnumerable.GetEnumerator|API> {0}", ObjectID);
            return Collection.GetEnumerator();
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.IDictionary.GetEnumerator|API> {0}", ObjectID);
            return Dictionary.GetEnumerator();
        }

        private string ObjectToString(object keyword)
        {
            try
            {
                return (string)keyword;
            }
            catch (InvalidCastException)
            {
                // BUGBUG: the message is not localized.
                throw new ArgumentException("not a string", nameof(keyword));
            }
        }

        void IDictionary.Remove(object keyword)
        {
            Remove(ObjectToString(keyword));
        }
        public virtual bool Remove(string keyword)
        {
            DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Remove|API> {0}, keyword='{1}'", ObjectID, keyword);
            ADP.CheckArgumentNull(keyword, nameof(keyword));
            if (CurrentValues.Remove(keyword))
            {
                _connectionString = null;
                _propertyDescriptors = null;
                return true;
            }
            return false;
        }

        // does the keyword exist as a stored value or something that should always be persisted
        public virtual bool ShouldSerialize(string keyword)
        {
            ADP.CheckArgumentNull(keyword, nameof(keyword));
            return CurrentValues.ContainsKey(keyword);
        }

        public override string ToString()
        {
            return ConnectionString;
        }

        public virtual bool TryGetValue(string keyword, [NotNullWhen(true)] out object? value)
        {
            ADP.CheckArgumentNull(keyword, nameof(keyword));
            return CurrentValues.TryGetValue(keyword, out value);
        }

        internal Attribute[] GetAttributesFromCollection(AttributeCollection collection)
        {
            Attribute[] attributes = new Attribute[collection.Count];
            collection.CopyTo(attributes, 0);
            return attributes;
        }

        private PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptorCollection? propertyDescriptors = _propertyDescriptors;
            if (null == propertyDescriptors)
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbConnectionStringBuilder.GetProperties|INFO> {0}", ObjectID);
                try
                {
                    Hashtable descriptors = new Hashtable(StringComparer.OrdinalIgnoreCase);

                    GetProperties(descriptors);

                    PropertyDescriptor[] properties = new PropertyDescriptor[descriptors.Count];
                    descriptors.Values.CopyTo(properties, 0);
                    propertyDescriptors = new PropertyDescriptorCollection(properties);
                    _propertyDescriptors = propertyDescriptors;
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
            return propertyDescriptors;
        }

        protected virtual void GetProperties(Hashtable propertyDescriptors)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbConnectionStringBuilder.GetProperties|API> {0}", ObjectID);
            try
            {
                // show all strongly typed properties (not already added)
                // except ConnectionString iff BrowsableConnectionString
                Attribute[]? attributes;
                foreach (PropertyDescriptor reflected in TypeDescriptor.GetProperties(this, true))
                {
                    Debug.Assert(reflected != null);
                    if (ADP.ConnectionString != reflected.Name)
                    {
                        string displayName = reflected.DisplayName;
                        if (!propertyDescriptors.ContainsKey(displayName))
                        {
                            attributes = GetAttributesFromCollection(reflected.Attributes);
                            PropertyDescriptor descriptor = new DbConnectionStringBuilderDescriptor(reflected.Name,
                                    reflected.ComponentType, reflected.PropertyType, reflected.IsReadOnly, attributes);
                            propertyDescriptors[displayName] = descriptor;
                        }
                        // else added by derived class first
                    }
                    else if (BrowsableConnectionString)
                    {
                        propertyDescriptors[ADP.ConnectionString] = reflected;
                    }
                    else
                    {
                        propertyDescriptors.Remove(ADP.ConnectionString);
                    }
                }

                // all keywords in Keys list that do not have strongly typed property, ODBC case
                if (!IsFixedSize)
                {
                    attributes = null;
                    foreach (string keyword in Keys)
                    {
                        Debug.Assert(keyword != null);
                        if (!propertyDescriptors.ContainsKey(keyword))
                        {
                            object value = this[keyword];

                            Type vtype;
                            if (null != value)
                            {
                                vtype = value.GetType();
                                if (typeof(string) == vtype)
                                {
                                    int tmp1;
                                    if (int.TryParse((string)value, out tmp1))
                                    {
                                        vtype = typeof(int);
                                    }
                                    else
                                    {
                                        bool tmp2;
                                        if (bool.TryParse((string)value, out tmp2))
                                        {
                                            vtype = typeof(bool);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                vtype = typeof(string);
                            }

                            Attribute[]? useAttributes = null;
                            if (StringComparer.OrdinalIgnoreCase.Equals(DbConnectionStringKeywords.Password, keyword) ||
                                StringComparer.OrdinalIgnoreCase.Equals(DbConnectionStringSynonyms.Pwd, keyword))
                            {
                                useAttributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    PasswordPropertyTextAttribute.Yes,
                                    RefreshPropertiesAttribute.All,
                                };
                            }
                            else if (null == attributes)
                            {
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    RefreshPropertiesAttribute.All,
                                };
                                useAttributes = attributes;
                            }

                            PropertyDescriptor descriptor = new DbConnectionStringBuilderDescriptor(keyword,
                                                                    GetType(), vtype, false, useAttributes!);
                            propertyDescriptors[keyword] = descriptor;
                        }
                    }
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        [RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        private PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            PropertyDescriptorCollection propertyDescriptors = GetProperties();
            if ((null == attributes) || (0 == attributes.Length))
            {
                // Basic case has no filtering
                return propertyDescriptors;
            }

            // Create an array that is guaranteed to hold all attributes
            PropertyDescriptor[] propertiesArray = new PropertyDescriptor[propertyDescriptors.Count];

            // Create an index to reference into this array
            int index = 0;

            // Iterate over each property
            foreach (PropertyDescriptor property in propertyDescriptors)
            {
                Debug.Assert(property != null);

                // Identify if this property's attributes match the specification
                bool match = true;
                foreach (Attribute attribute in attributes)
                {
                    Attribute attr = property.Attributes[attribute.GetType()];
                    if ((attr == null && !attribute.IsDefaultAttribute()) || attr?.Match(attribute) == false)
                    {
                        match = false;
                        break;
                    }
                }

                // If this property matches, add it to the array
                if (match)
                {
                    propertiesArray[index] = property;
                    index++;
                }
            }

            // Create a new array that only contains the filtered properties
            PropertyDescriptor[] filteredPropertiesArray = new PropertyDescriptor[index];
            Array.Copy(propertiesArray, filteredPropertiesArray, index);

            return new PropertyDescriptorCollection(filteredPropertiesArray);
        }

// TODO: Enable after System.ComponentModel.TypeConverter is annotated
#nullable disable
        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }
        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }
        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }
        [RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }
        [RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }
        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }
        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return GetProperties();
        }
        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return GetProperties(attributes);
        }
        [RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }
        [RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }
        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
#nullable enable
    }
}
