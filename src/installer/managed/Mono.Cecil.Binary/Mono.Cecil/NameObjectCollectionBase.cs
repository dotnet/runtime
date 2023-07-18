using System;
using System.Collections;
using System.Runtime.Serialization;

#if NO_SYSTEM_DLL
namespace System.Collections.Specialized
{
	[Serializable]
	internal abstract class NameObjectCollectionBase : ICollection, IEnumerable, ISerializable, IDeserializationCallback
	{
		private Hashtable m_ItemsContainer;
		/// <summary>
		/// Extends Hashtable based Items container to support storing null-key pairs
		/// </summary>
		private _Item m_NullKeyItem;
		private ArrayList m_ItemsArray;
		private IHashCodeProvider m_hashprovider;
		private IComparer m_comparer;
		private int m_defCapacity;
		private bool m_readonly;
		SerializationInfo infoCopy;
		private KeysCollection keyscoll;
		private IEqualityComparer equality_comparer;

		internal IEqualityComparer EqualityComparer {
			get { return equality_comparer; }
		}
		internal IComparer Comparer
		{
			get { return m_comparer; }
		}

		internal IHashCodeProvider HashCodeProvider
		{
			get { return m_hashprovider; }
		}

		internal class _Item
		{
			public string key;
			public object value;
			public _Item(string key, object value)
			{
				this.key = key;
				this.value = value;
			}
		}
		/// <summary>
		/// Implements IEnumerable interface for KeysCollection
		/// </summary>
		[Serializable]
		internal class _KeysEnumerator : IEnumerator
		{
			private NameObjectCollectionBase m_collection;
			private int m_position;

			internal _KeysEnumerator(NameObjectCollectionBase collection)
			{
				m_collection = collection;
				Reset();
			}
			public object Current
			{

				get
				{
					if ((m_position < m_collection.Count) || (m_position < 0))
						return m_collection.BaseGetKey(m_position);
					else
						throw new InvalidOperationException();
				}

			}
			public bool MoveNext()
			{
				return ((++m_position) < m_collection.Count);
			}
			public void Reset()
			{
				m_position = -1;
			}
		}

		/// <summary>
		/// SDK: Represents a collection of the String keys of a collection.
		/// </summary>
		[Serializable]
		internal class KeysCollection : ICollection, IEnumerable
		{
			private NameObjectCollectionBase m_collection;

			internal KeysCollection(NameObjectCollectionBase collection)
			{
				this.m_collection = collection;
			}

			public virtual string Get(int index)
			{
				return m_collection.BaseGetKey(index);
			}

			// ICollection methods -----------------------------------
			void ICollection.CopyTo(Array array, int arrayIndex)
			{
				ArrayList items = m_collection.m_ItemsArray;
				if (null == array)
					throw new ArgumentNullException ("array");

				if (arrayIndex < 0)
					throw new ArgumentOutOfRangeException ("arrayIndex");

				if ((array.Length > 0) && (arrayIndex >= array.Length))
					throw new ArgumentException ("arrayIndex is equal to or greater than array.Length");

				if (arrayIndex + items.Count > array.Length)
					throw new ArgumentException ("Not enough room from arrayIndex to end of array for this KeysCollection");

				if (array != null && array.Rank > 1)
					throw new ArgumentException("array is multidimensional");

				object[] objArray = (object[])array;
				for (int i = 0; i < items.Count; i++, arrayIndex++)
					objArray[arrayIndex] = ((_Item)items[i]).key;
			}

			bool ICollection.IsSynchronized
			{
				get
				{
					return false;
				}
			}
			object ICollection.SyncRoot
			{
				get
				{
					return m_collection;
				}
			}
			/// <summary>
			/// Gets the number of keys in the NameObjectCollectionBase.KeysCollection
			/// </summary>
			public int Count
			{
				get
				{
					return m_collection.Count;
				}
			}

			public string this[int index]
			{
				get { return Get(index); }
			}

			// IEnumerable methods --------------------------------
			/// <summary>
			/// SDK: Returns an enumerator that can iterate through the NameObjectCollectionBase.KeysCollection.
			/// </summary>
			/// <returns></returns>
			public IEnumerator GetEnumerator()
			{
				return new _KeysEnumerator(m_collection);
			}
		}

		//--------------- Protected Instance Constructors --------------

		/// <summary>
		/// SDK: Initializes a new instance of the NameObjectCollectionBase class that is empty.
		/// </summary>
		protected NameObjectCollectionBase()
		{
			m_readonly = false;
#if NET_1_0
			m_hashprovider = CaseInsensitiveHashCodeProvider.Default;
			m_comparer = CaseInsensitiveComparer.Default;
#else
			m_hashprovider = CaseInsensitiveHashCodeProvider.DefaultInvariant;
			m_comparer = CaseInsensitiveComparer.DefaultInvariant;
#endif
			m_defCapacity = 0;
			Init();
		}

		protected NameObjectCollectionBase(int capacity)
		{
			m_readonly = false;
#if NET_1_0
			m_hashprovider = CaseInsensitiveHashCodeProvider.Default;
			m_comparer = CaseInsensitiveComparer.Default;
#else
			m_hashprovider = CaseInsensitiveHashCodeProvider.DefaultInvariant;
			m_comparer = CaseInsensitiveComparer.DefaultInvariant;
#endif
			m_defCapacity = capacity;
			Init();
		}


		internal NameObjectCollectionBase (IEqualityComparer equalityComparer, IComparer comparer, IHashCodeProvider hcp)
		{
			equality_comparer = equalityComparer;
			m_comparer = comparer;
			m_hashprovider = hcp;
			m_readonly = false;
			m_defCapacity = 0;
			Init ();
		}

		protected NameObjectCollectionBase (IEqualityComparer equalityComparer) : this( (equalityComparer == null ? StringComparer.InvariantCultureIgnoreCase : equalityComparer), null, null)
		{
		}

		[Obsolete ("Use NameObjectCollectionBase(IEqualityComparer)")]
		protected NameObjectCollectionBase(IHashCodeProvider hashProvider, IComparer comparer)
		{
			m_comparer = comparer;
			m_hashprovider = hashProvider;
			m_readonly = false;
			m_defCapacity = 0;
			Init();
		}

		protected NameObjectCollectionBase(SerializationInfo info, StreamingContext context)
		{
			infoCopy = info;
		}

		protected NameObjectCollectionBase (int capacity, IEqualityComparer equalityComparer)
		{
			m_readonly = false;
			equality_comparer = (equalityComparer == null ? StringComparer.InvariantCultureIgnoreCase : equalityComparer);
			m_defCapacity = capacity;
			Init();
		}

		[Obsolete ("Use NameObjectCollectionBase(int,IEqualityComparer)")]
		protected NameObjectCollectionBase(int capacity, IHashCodeProvider hashProvider, IComparer comparer)
		{
			m_readonly = false;

			m_hashprovider = hashProvider;
			m_comparer = comparer;
			m_defCapacity = capacity;
			Init();
		}

		private void Init()
		{
			if (equality_comparer != null)
				m_ItemsContainer = new Hashtable (m_defCapacity, equality_comparer);
			else
				m_ItemsContainer = new Hashtable (m_defCapacity, m_hashprovider, m_comparer);
			m_ItemsArray = new ArrayList();
			m_NullKeyItem = null;
		}

		//--------------- Public Instance Properties -------------------

		public virtual NameObjectCollectionBase.KeysCollection Keys
		{
			get
			{
				if (keyscoll == null)
					keyscoll = new KeysCollection(this);
				return keyscoll;
			}
		}

		//--------------- Public Instance Methods ----------------------
		//
		/// <summary>
		/// SDK: Returns an enumerator that can iterate through the NameObjectCollectionBase.
		///
		/// <remark>This enumerator returns the keys of the collection as strings.</remark>
		/// </summary>
		/// <returns></returns>
		public
		virtual
 IEnumerator GetEnumerator()
		{
			return new _KeysEnumerator(this);
		}

		// ISerializable
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			int count = Count;
			string[] keys = new string[count];
			object[] values = new object[count];
			int i = 0;
			foreach (_Item item in m_ItemsArray)
			{
				keys[i] = item.key;
				values[i] = item.value;
				i++;
			}

			if (equality_comparer != null) {
				info.AddValue ("KeyComparer", equality_comparer, typeof (IEqualityComparer));
				info.AddValue ("Version", 4, typeof (int));
			} else {
				info.AddValue ("HashProvider", m_hashprovider, typeof (IHashCodeProvider));
				info.AddValue ("Comparer", m_comparer, typeof (IComparer));
				info.AddValue ("Version", 2, typeof (int));
			}
			info.AddValue("ReadOnly", m_readonly);
			info.AddValue("Count", count);
			info.AddValue("Keys", keys, typeof(string[]));
			info.AddValue("Values", values, typeof(object[]));
		}

		// ICollection
		public virtual int Count
		{
			get
			{
				return m_ItemsArray.Count;
			}
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { return this; }
		}

		void ICollection.CopyTo(Array array, int index)
		{
			((ICollection)Keys).CopyTo(array, index);
		}

		// IDeserializationCallback
		public virtual void OnDeserialization(object sender)
		{
			SerializationInfo info = infoCopy;

			// If a subclass overrides the serialization constructor
			// and inplements its own serialization process, infoCopy will
			// be null and we can ignore this callback.
			if (info == null)
				return;

			infoCopy = null;
			m_hashprovider = (IHashCodeProvider)info.GetValue("HashProvider",
										typeof(IHashCodeProvider));
			if (m_hashprovider == null) {
				equality_comparer = (IEqualityComparer) info.GetValue ("KeyComparer", typeof (IEqualityComparer));
			} else {
				m_comparer = (IComparer) info.GetValue ("Comparer", typeof (IComparer));
				if (m_comparer == null)
					throw new SerializationException ("The comparer is null");
			}
			m_readonly = info.GetBoolean("ReadOnly");
			string[] keys = (string[])info.GetValue("Keys", typeof(string[]));
			if (keys == null)
				throw new SerializationException("keys is null");

			object[] values = (object[])info.GetValue("Values", typeof(object[]));
			if (values == null)
				throw new SerializationException("values is null");

			Init();
			int count = keys.Length;
			for (int i = 0; i < count; i++)
				BaseAdd(keys[i], values[i]);
		}

		//--------------- Protected Instance Properties ----------------
		/// <summary>
		/// SDK: Gets or sets a value indicating whether the NameObjectCollectionBase instance is read-only.
		/// </summary>
		protected bool IsReadOnly
		{
			get
			{
				return m_readonly;
			}
			set
			{
				m_readonly = value;
			}
		}

		//--------------- Protected Instance Methods -------------------
		/// <summary>
		/// Adds an Item with the specified key and value into the <see cref="NameObjectCollectionBase"/>NameObjectCollectionBase instance.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		protected void BaseAdd(string name, object value)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException("Collection is read-only");

			_Item newitem = new _Item(name, value);

			if (name == null)
			{
				//todo: consider nullkey entry
				if (m_NullKeyItem == null)
					m_NullKeyItem = newitem;
			}
			else
				if (m_ItemsContainer[name] == null)
				{
					m_ItemsContainer.Add(name, newitem);
				}
			m_ItemsArray.Add(newitem);
		}

		protected void BaseClear()
		{
			if (this.IsReadOnly)
				throw new NotSupportedException("Collection is read-only");
			Init();
		}

		/// <summary>
		/// SDK: Gets the value of the entry at the specified index of the NameObjectCollectionBase instance.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		protected object BaseGet(int index)
		{
			return ((_Item)m_ItemsArray[index]).value;
		}

		/// <summary>
		/// SDK: Gets the value of the first entry with the specified key from the NameObjectCollectionBase instance.
		/// </summary>
		/// <remark>CAUTION: The BaseGet method does not distinguish between a null reference which is returned because the specified key is not found and a null reference which is returned because the value associated with the key is a null reference.</remark>
		/// <param name="name"></param>
		/// <returns></returns>
		protected object BaseGet(string name)
		{
			_Item item = FindFirstMatchedItem(name);
			/// CAUTION: The BaseGet method does not distinguish between a null reference which is returned because the specified key is not found and a null reference which is returned because the value associated with the key is a null reference.
			if (item == null)
				return null;
			else
				return item.value;
		}

		/// <summary>
		/// SDK:Returns a String array that contains all the keys in the NameObjectCollectionBase instance.
		/// </summary>
		/// <returns>A String array that contains all the keys in the NameObjectCollectionBase instance.</returns>
		protected string[] BaseGetAllKeys()
		{
			int cnt = m_ItemsArray.Count;
			string[] allKeys = new string[cnt];
			for (int i = 0; i < cnt; i++)
				allKeys[i] = BaseGetKey(i);//((_Item)m_ItemsArray[i]).key;

			return allKeys;
		}

		/// <summary>
		/// SDK: Returns an Object array that contains all the values in the NameObjectCollectionBase instance.
		/// </summary>
		/// <returns>An Object array that contains all the values in the NameObjectCollectionBase instance.</returns>
		protected object[] BaseGetAllValues()
		{
			int cnt = m_ItemsArray.Count;
			object[] allValues = new object[cnt];
			for (int i = 0; i < cnt; i++)
				allValues[i] = BaseGet(i);

			return allValues;
		}

		protected object[] BaseGetAllValues(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("'type' argument can't be null");
			int cnt = m_ItemsArray.Count;
			object[] allValues = (object[])Array.CreateInstance(type, cnt);
			for (int i = 0; i < cnt; i++)
				allValues[i] = BaseGet(i);

			return allValues;
		}

		protected string BaseGetKey(int index)
		{
			return ((_Item)m_ItemsArray[index]).key;
		}

		/// <summary>
		/// Gets a value indicating whether the NameObjectCollectionBase instance contains entries whose keys are not a null reference
		/// </summary>
		/// <returns>true if the NameObjectCollectionBase instance contains entries whose keys are not a null reference otherwise, false.</returns>
		protected bool BaseHasKeys()
		{
			return (m_ItemsContainer.Count > 0);
		}

		protected void BaseRemove(string name)
		{
			int cnt = 0;
			String key;
			if (this.IsReadOnly)
				throw new NotSupportedException("Collection is read-only");
			if (name != null)
			{
				m_ItemsContainer.Remove(name);
			}
			else
			{
				m_NullKeyItem = null;
			}

			cnt = m_ItemsArray.Count;
			for (int i = 0; i < cnt; )
			{
				key = BaseGetKey(i);
				if (Equals(key, name))
				{
					m_ItemsArray.RemoveAt(i);
					cnt--;
				}
				else
					i++;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		/// <LAME>This function implemented the way Microsoft implemented it -
		/// item is removed from hashtable and array without considering the case when there are two items with the same key but different values in array.
		/// E.g. if
		/// hashtable is [("Key1","value1")] and array contains [("Key1","value1")("Key1","value2")] then
		/// after RemoveAt(1) the collection will be in following state:
		/// hashtable:[]
		/// array: [("Key1","value1")]
		/// It's ok only then the key is uniquely assosiated with the value
		/// To fix it a comparsion of objects stored under the same key in the hashtable and in the arraylist should be added
		/// </LAME>>
		protected void BaseRemoveAt(int index)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException("Collection is read-only");
			string key = BaseGetKey(index);
			if (key != null)
			{
				// TODO: see LAME description above
				m_ItemsContainer.Remove(key);
			}
			else
				m_NullKeyItem = null;
			m_ItemsArray.RemoveAt(index);
		}

		/// <summary>
		/// SDK: Sets the value of the entry at the specified index of the NameObjectCollectionBase instance.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		protected void BaseSet(int index, object value)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException("Collection is read-only");
			_Item item = (_Item)m_ItemsArray[index];
			item.value = value;
		}

		/// <summary>
		/// Sets the value of the first entry with the specified key in the NameObjectCollectionBase instance, if found; otherwise, adds an entry with the specified key and value into the NameObjectCollectionBase instance.
		/// </summary>
		/// <param name="name">The String key of the entry to set. The key can be a null reference </param>
		/// <param name="value">The Object that represents the new value of the entry to set. The value can be a null reference</param>
		protected void BaseSet(string name, object value)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException("Collection is read-only");
			_Item item = FindFirstMatchedItem(name);
			if (item != null)
				item.value = value;
			else
				BaseAdd(name, value);
		}

		private _Item FindFirstMatchedItem(string name)
		{
			if (name != null)
				return (_Item)m_ItemsContainer[name];
			else
			{
				//TODO: consider null key case
				return m_NullKeyItem;
			}
		}

		internal bool Equals(string s1, string s2)
		{
			if (m_comparer != null)
				return (m_comparer.Compare (s1, s2) == 0);
			else
				return equality_comparer.Equals (s1, s2);
		}
	}
}
#endif
