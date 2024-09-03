// plist-cil - An open source library to parse and generate property lists for .NET
// Copyright (C) 2015 Natalia Portillo
//
// This code is based on:
// plist - An open source library to parse and generate property lists
// Copyright (C) 2014 Daniel Dreibrodt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Claunia.PropertyList
{
    /// <summary>
    ///     <para>
    ///         A NSDictionary is a collection of keys and values, essentially a Dictionary. The keys are simple Strings
    ///         whereas the values can be any kind of NSObject.
    ///     </para>
    ///     <para>You can access the keys through the function <see cref="Keys" />.</para>
    ///     <para>Access to the objects stored for each key is given through the function <see cref="ObjectForKey" />.</para>
    /// </summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public class NSDictionary : NSObject, IDictionary<string, NSObject>
    {
        readonly Dictionary<string, NSObject> dict;

        // Maps the keys in this dictionary to their NSString equivalent. Makes sure the NSString
        // object remains constant across calls to AssignIDs and ToBinary
        readonly Dictionary<string, NSString> keys;

        /// <summary>Creates a new empty NSDictionary with a specific capacity.</summary>
        /// <param name="capacity">The capacity of the dictionary.</param>
        public NSDictionary(int capacity)
        {
            dict = new Dictionary<string, NSObject>(capacity);
            keys = new Dictionary<string, NSString>(capacity);
        }

        /// <summary>Creates a new empty NSDictionary.</summary>
        public NSDictionary() : this(0) {}

        /// <summary>Gets a value indicating whether this instance is empty.</summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty => dict.Count == 0;

        #region IEnumerable implementation
        /// <summary>Gets the enumerator.</summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<string, NSObject>> GetEnumerator() => dict.GetEnumerator();
        #endregion

        #region IEnumerable implementation
        IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();
        #endregion

        /// <summary>
        ///     Gets the hashmap which stores the keys and values of this dictionary. Changes to the hashmap's contents are
        ///     directly reflected in this dictionary.
        /// </summary>
        /// <returns>The hashmap which is used by this dictionary to store its contents.</returns>
        public Dictionary<string, NSObject> GetDictionary() => dict;

        /// <summary>Gets the NSObject stored for the given key.</summary>
        /// <returns>The object.</returns>
        /// <param name="key">The key.</param>
        public NSObject ObjectForKey(string key)
        {
            NSObject nso;

            return dict.TryGetValue(key, out nso) ? nso : null;
        }

        /// <summary>Checks if the specified object key is contained in the current instance.</summary>
        /// <returns><c>true</c>, if key is contained, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public bool ContainsKey(object key) => key is string s && dict.ContainsKey(s);

        /// <summary>Removes the item corresponding to the specified key from the current instance, if found.</summary>
        /// <param name="key">Key.</param>
        /// <returns><c>true</c>, if  removed, <c>false</c> otherwise.</returns>
        public bool Remove(object key) => key is string s && dict.Remove(s);

        /// <summary>Gets the <see cref="NSObject" /> corresponding to the specified key from the current instance.</summary>
        /// <param name="key">Key.</param>
        /// <returns>The object corresponding to the specified key, null if not found in the current instance.</returns>
        public NSObject Get(object key)
        {
            if(key is string s)
                return ObjectForKey(s);

            return null;
        }

        /// <summary>Checks if the current instance contains the object corresponding to the specified key.</summary>
        /// <returns><c>true</c>, if value is contained, <c>false</c> otherwise.</returns>
        /// <param name="value">Object to search up in the current instance.</param>
        public bool ContainsValue(object value)
        {
            if(value == null)
                return false;

            NSObject wrap = Wrap(value);

            return dict.ContainsValue(wrap);
        }

        /// <summary>
        ///     Puts a new key-value pair into this dictionary. If the value is null, no operation will be performed on the
        ///     dictionary.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="obj">
        ///     The value. Supported object types are numbers, byte-arrays, dates, strings and arrays or sets of
        ///     those.
        /// </param>
        public void Add(string key, object obj)
        {
            if(obj == null)
                return;

            Add(key, Wrap(obj));
        }

        /// <summary>Puts a new key-value pair into this dictionary.</summary>
        /// <param name="key">The key.</param>
        /// <param name="obj">The value.</param>
        public void Add(string key, long obj) => Add(key, new NSNumber(obj));

        /// <summary>Puts a new key-value pair into this dictionary.</summary>
        /// <param name="key">The key.</param>
        /// <param name="obj">The value.</param>
        public void Add(string key, double obj) => Add(key, new NSNumber(obj));

        /// <summary>Puts a new key-value pair into this dictionary.</summary>
        /// <param name="key">The key.</param>
        /// <param name="obj">The value.</param>
        public void Add(string key, bool obj) => Add(key, new NSNumber(obj));

        /// <summary>Checks whether a given value is contained in this dictionary.</summary>
        /// <param name="val">The value that will be searched for.</param>
        /// <returns>Whether the key is contained in this dictionary.</returns>
        public bool ContainsValue(string val)
        {
            foreach(NSObject o in dict.Values)
                if(o is NSString str &&
                   str.Content.Equals(val))
                    return true;

            return false;
        }

        /// <summary>Checks whether a given value is contained in this dictionary.</summary>
        /// <param name="val">The value that will be searched for.</param>
        /// <returns>Whether the key is contained in this dictionary.</returns>
        public bool ContainsValue(long val)
        {
            foreach(NSObject o in dict.Values)
                if(o is NSNumber num &&
                   num.isInteger()   &&
                   num.ToInt() == val)
                    return true;

            return false;
        }

        /// <summary>Checks whether a given value is contained in this dictionary.</summary>
        /// <param name="val">The value that will be searched for.</param>
        /// <returns>Whether the key is contained in this dictionary.</returns>
        public bool ContainsValue(double val)
        {
            foreach(NSObject o in dict.Values)
                if(o is NSNumber num &&
                   num.isReal()      &&
                   num.ToDouble() == val)
                    return true;

            return false;
        }

        /// <summary>Checks whether a given value is contained in this dictionary.</summary>
        /// <param name="val">The value that will be searched for.</param>
        /// <returns>Whether the key is contained in this dictionary.</returns>
        public bool ContainsValue(bool val)
        {
            foreach(NSObject o in dict.Values)
                if(o is NSNumber num &&
                   num.isBoolean()   &&
                   num.ToBool() == val)
                    return true;

            return false;
        }

        /// <summary>Checks whether a given value is contained in this dictionary.</summary>
        /// <param name="val">The value that will be searched for.</param>
        /// <returns>Whether the key is contained in this dictionary.</returns>
        public bool ContainsValue(DateTime val)
        {
            foreach(NSObject o in dict.Values)
                if(o is NSDate dat &&
                   dat.Date.Equals(val))
                    return true;

            return false;
        }

        /// <summary>Checks whether a given value is contained in this dictionary.</summary>
        /// <param name="val">The value that will be searched for.</param>
        /// <returns>Whether the key is contained in this dictionary.</returns>
        public bool ContainsValue(byte[] val)
        {
            foreach(NSObject o in dict.Values)
                if(o is NSData dat &&
                   ArrayEquals(dat.Bytes, val))
                    return true;

            return false;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSDictionary" />.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="Claunia.PropertyList.NSObject" /> to compare with the current
        ///     <see cref="Claunia.PropertyList.NSDictionary" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSDictionary" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(NSObject obj)
        {
            if(obj is not NSDictionary dictionary)
                return false;

            if(dictionary.dict.Count != dict.Count)
                return false;

            foreach(KeyValuePair<string, NSObject> kvp in dict)
            {
                bool found = dictionary.dict.TryGetValue(kvp.Key, out NSObject nsoB);

                if(!found)
                    return false;

                if(!kvp.Value.Equals(nsoB))
                    return false;
            }

            return true;
        }

        /// <summary>Serves as a hash function for a <see cref="Claunia.PropertyList.NSDictionary" /> object.</summary>
        /// <returns>
        ///     A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        ///     hash table.
        /// </returns>
        public override int GetHashCode()
        {
            int hash = 7;
            hash = (83 * hash) + (dict != null ? dict.GetHashCode() : 0);

            return hash;
        }

        internal override void ToXml(StringBuilder xml, int level)
        {
            Indent(xml, level);
            xml.Append("<dict>");
            xml.Append(NEWLINE);

            foreach(KeyValuePair<string, NSObject> kvp in dict)
            {
                Indent(xml, level + 1);
                xml.Append("<key>");

                //According to http://www.w3.org/TR/REC-xml/#syntax node values must not
                //contain the characters < or &. Also the > character should be escaped.
                if(kvp.Key.Contains("&") ||
                   kvp.Key.Contains("<") ||
                   kvp.Key.Contains(">"))
                {
                    xml.Append("<![CDATA[");
                    xml.Append(kvp.Key.Replace("]]>", "]]]]><![CDATA[>"));
                    xml.Append("]]>");
                }
                else
                    xml.Append(kvp.Key);

                xml.Append("</key>");
                xml.Append(NEWLINE);
                kvp.Value.ToXml(xml, level + 1);
                xml.Append(NEWLINE);
            }

            Indent(xml, level);
            xml.Append("</dict>");
        }

        internal override void AssignIDs(BinaryPropertyListWriter outPlist)
        {
            base.AssignIDs(outPlist);

            foreach(KeyValuePair<string, NSObject> entry in dict)
                keys[entry.Key].AssignIDs(outPlist);

            foreach(KeyValuePair<string, NSObject> entry in dict)
                entry.Value.AssignIDs(outPlist);
        }

        internal override void ToBinary(BinaryPropertyListWriter outPlist)
        {
            outPlist.WriteIntHeader(0xD, dict.Count);

            foreach(KeyValuePair<string, NSObject> entry in dict)
                outPlist.WriteID(outPlist.GetID(keys[entry.Key]));

            foreach(KeyValuePair<string, NSObject> entry in dict)
                outPlist.WriteID(outPlist.GetID(entry.Value));
        }

        /// <summary>
        ///     Generates a valid ASCII property list which has this NSDictionary as its root object. The generated property
        ///     list complies with the format as described in
        ///     https://developer.apple.com/library/mac/#documentation/Cocoa/Conceptual/PropertyLists/OldStylePlists/OldStylePLists.html
        ///     Property List Programming Guide - Old-Style ASCII Property Lists.
        /// </summary>
        /// <returns>ASCII representation of this object.</returns>
        public string ToASCIIPropertyList()
        {
            var ascii = new StringBuilder();
            ToASCII(ascii, 0);
            ascii.Append(NEWLINE);

            return ascii.ToString();
        }

        /// <summary>
        ///     Generates a valid ASCII property list in GnuStep format which has this NSDictionary as its root object. The
        ///     generated property list complies with the format as described in
        ///     http://www.gnustep.org/resources/documentation/Developer/Base/Reference/NSPropertyList.html GnuStep -
        ///     NSPropertyListSerialization class documentation.
        /// </summary>
        /// <returns>GnuStep ASCII representation of this object.</returns>
        public string ToGnuStepASCIIPropertyList()
        {
            var ascii = new StringBuilder();
            ToASCIIGnuStep(ascii, 0);
            ascii.Append(NEWLINE);

            return ascii.ToString();
        }

        internal override void ToASCII(StringBuilder ascii, int level)
        {
            Indent(ascii, level);
            ascii.Append(ASCIIPropertyListParser.DICTIONARY_BEGIN_TOKEN);
            ascii.Append(NEWLINE);

            foreach(string key in Keys)
            {
                NSObject val = ObjectForKey(key);
                Indent(ascii, level + 1);
                ascii.Append("\"");
                ascii.Append(NSString.EscapeStringForASCII(key));
                ascii.Append("\" =");

                if(val is NSDictionary or NSArray or NSData)
                {
                    ascii.Append(NEWLINE);
                    val.ToASCII(ascii, level + 2);
                }
                else
                {
                    ascii.Append(" ");
                    val.ToASCII(ascii, 0);
                }

                ascii.Append(ASCIIPropertyListParser.DICTIONARY_ITEM_DELIMITER_TOKEN);
                ascii.Append(NEWLINE);
            }

            Indent(ascii, level);
            ascii.Append(ASCIIPropertyListParser.DICTIONARY_END_TOKEN);
        }

        internal override void ToASCIIGnuStep(StringBuilder ascii, int level)
        {
            Indent(ascii, level);
            ascii.Append(ASCIIPropertyListParser.DICTIONARY_BEGIN_TOKEN);
            ascii.Append(NEWLINE);

            foreach(string key in Keys)
            {
                NSObject val = ObjectForKey(key);
                Indent(ascii, level + 1);
                ascii.Append("\"");
                ascii.Append(NSString.EscapeStringForASCII(key));
                ascii.Append("\" =");

                if(val is NSDictionary or NSArray or NSData)
                {
                    ascii.Append(NEWLINE);
                    val.ToASCIIGnuStep(ascii, level + 2);
                }
                else
                {
                    ascii.Append(" ");
                    val.ToASCIIGnuStep(ascii, 0);
                }

                ascii.Append(ASCIIPropertyListParser.DICTIONARY_ITEM_DELIMITER_TOKEN);
                ascii.Append(NEWLINE);
            }

            Indent(ascii, level);
            ascii.Append(ASCIIPropertyListParser.DICTIONARY_END_TOKEN);
        }

        #region IDictionary implementation
        /// <summary>Add the specified key and value.</summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void Add(string key, NSObject value)
        {
            dict.Add(key, value);
            keys.Add(key, new NSString(key));
        }

        /// <summary>Checks if there is any item contained in the current instance corresponding with the specified key.</summary>
        /// <returns><c>true</c>, if key was contained, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public bool ContainsKey(string key) => dict.ContainsKey(key);

        /// <summary>Checks if there is any item contained in the current instance corresponding with the specified value.</summary>
        /// <returns><c>true</c>, if value is contained, <c>false</c> otherwise.</returns>
        /// <param name="value">Key.</param>
        public bool ContainsValue(NSObject value) => dict.ContainsValue(value);

        /// <summary>Removes the item belonging to the specified key.</summary>
        /// <param name="key">Key.</param>
        public bool Remove(string key)
        {
            keys.Remove(key);

            return dict.Remove(key);
        }

        /// <summary>Tries to get the item corresponding to the specified key</summary>
        /// <returns><c>true</c>, if get value was successfully found and retrieved, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        /// <param name="value">Where to store the value.</param>
        public bool TryGetValue(string key, out NSObject value) => dict.TryGetValue(key, out value);

        /// <summary>Gets or sets the <see cref="Claunia.PropertyList.NSObject" /> at the specified index.</summary>
        /// <param name="index">Index.</param>
        public NSObject this[string index]
        {
            get => dict[index];
            set
            {
                if(!keys.ContainsKey(index))
                    keys.Add(index, new NSString(index));

                dict[index] = value;
            }
        }

        /// <summary>Gets an array with all the keys contained in the current instance.</summary>
        /// <value>The keys.</value>
        public ICollection<string> Keys => dict.Keys;

        /// <summary>Gets an array with all the objects contained in the current instance.</summary>
        /// <value>The objects.</value>
        public ICollection<NSObject> Values => dict.Values;
        #endregion

        #region ICollection implementation
        /// <summary>Adds the specified item.</summary>
        /// <param name="item">Item.</param>
        public void Add(KeyValuePair<string, NSObject> item)
        {
            keys.Add(item.Key, new NSString(item.Key));
            dict.Add(item.Key, item.Value);
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            keys.Clear();
            dict.Clear();
        }

        /// <summary>Checks if the current instance contains the specified item.</summary>
        /// <param name="item">Item.</param>
        /// <returns><c>true</c> if it is found, <c>false</c> otherwise.</returns>
        public bool Contains(KeyValuePair<string, NSObject> item) => dict.ContainsKey(item.Key);

        /// <summary>
        ///     Copies the <see cref="Dictionary{TKey, TValue}.ValueCollection" /> elements to an existing one-dimensional
        ///     <see cref="Array" />, starting at the specified array index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="Array" /> that is the destination of the elements copied from
        ///     <see cref="Dictionary{TKey, TValue}.ValueCollection" />. The <see cref="Array" /> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, NSObject>[] array, int arrayIndex)
        {
            ICollection<KeyValuePair<string, NSObject>> coll = dict;
            coll.CopyTo(array, arrayIndex);
        }

        /// <summary>Removes the specified item.</summary>
        /// <param name="item">Item to remove.</param>
        /// <returns><c>true</c> if successfully removed, <c>false</c> if not, or if item is not in current instance.</returns>
        public bool Remove(KeyValuePair<string, NSObject> item)
        {
            keys.Remove(item.Key);

            return dict.Remove(item.Key);
        }

        /// <summary>Gets the count of items in the current instance.</summary>
        /// <value>How many items are contained in the current instance.</value>
        public int Count => dict.Count;

        /// <summary>Gets a value indicating whether this instance is read only.</summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly => false;
        #endregion
    }
}