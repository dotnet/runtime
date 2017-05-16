// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization
{
    public class ObjectIDGenerator
    {
        private const int NumBins = 4;

        // Table of prime numbers to use as hash table sizes. Each entry is the
        // smallest prime number larger than twice the previous entry.
        private static readonly int[] s_sizes = 
        {
            5, 11, 29, 47, 97, 197, 397, 797, 1597, 3203, 6421, 12853, 25717, 51437,
            102877, 205759, 411527, 823117, 1646237, 3292489, 6584983
        };

        internal int _currentCount;
        internal int _currentSize;
        internal long[] _ids;
        internal object[] _objs;

        // Constructs a new ObjectID generator, initializing all of the necessary variables.
        public ObjectIDGenerator()
        {
            _currentCount = 1;
            _currentSize = s_sizes[0];
            _ids = new long[_currentSize * NumBins];
            _objs = new object[_currentSize * NumBins];
        }

        // Determines where element obj lives, or should live, 
        // within the table. It calculates the hashcode and searches all of the
        // bins where the given object could live.  If it's not found within the bin, 
        // we rehash and go look for it in another bin.  If we find the object, we
        // set found to true and return it's position.  If we can't find the object,
        // we set found to false and return the position where the object should be inserted.
        private int FindElement(object obj, out bool found)
        {
            int hashcode = RuntimeHelpers.GetHashCode(obj);
            int hashIncrement = (1 + ((hashcode & 0x7FFFFFFF) % (_currentSize - 2)));
            do
            {
                int pos = ((hashcode & 0x7FFFFFFF) % _currentSize) * NumBins;
                for (int i = pos; i < pos + NumBins; i++)
                {
                    if (_objs[i] == null)
                    {
                        found = false;
                        return i;
                    }
                    if (_objs[i] == obj)
                    {
                        found = true;
                        return i;
                    }
                }
                hashcode += hashIncrement;
                //the seemingly infinite loop must be revisited later. Currently it is assumed that
                //always the array will be expanded (Rehash) when it is half full
            } while (true);
        }

        // Gets the id for a particular object, generating one if needed.  GetID calls
        // FindElement to find out where the object lives or should live.  If we didn't
        // find the element, we generate an object id for it and insert the pair into the
        // table.  We return an Int64 for the object id.  The out parameter firstTime
        // is set to true if this is the first time that we have seen this object.
        public virtual long GetId(object obj, out bool firstTime)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            bool found;
            int pos = FindElement(obj, out found);

            //We pull out foundID so that rehashing doesn't cause us to lose track of the id that we just found.
            long foundID;
            if (!found)
            {
                //We didn't actually find the object, so we should need to insert it into
                //the array and assign it an object id.
                _objs[pos] = obj;
                _ids[pos] = _currentCount++;
                foundID = _ids[pos];
                if (_currentCount > (_currentSize * NumBins) / 2)
                {
                    Rehash();
                }
            }
            else
            {
                foundID = _ids[pos];
            }
            firstTime = !found;

            return foundID;
        }

        // Checks to see if obj has already been assigned an id.  If it has,
        // we return that id, otherwise we return 0.
        public virtual long HasId(object obj, out bool firstTime)
        {
            bool found;

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            int pos = FindElement(obj, out found);
            if (found)
            {
                firstTime = false;
                return _ids[pos];
            }

            firstTime = true;
            return 0;
        }

        // Rehashes the table by finding the next larger size in the list provided,
        // allocating two new arrays of that size and rehashing all of the elements in
        // the old arrays into the new ones.  Expensive but necessary.
        private void Rehash()
        {
            int i = 0;
            for (int currSize = _currentSize; i < s_sizes.Length && s_sizes[i] <= currSize; i++) ;
            if (i == s_sizes.Length)
            {
                // We just walked off the end of the array.
                throw new SerializationException(SR.Serialization_TooManyElements);
            }
            _currentSize = s_sizes[i];

            long[] newIds = new long[_currentSize * NumBins];
            object[] newObjs = new object[_currentSize * NumBins];

            long[] oldIds = _ids;
            object[] oldObjs = _objs;

            _ids = newIds;
            _objs = newObjs;

            for (int j = 0; j < oldObjs.Length; j++)
            {
                if (oldObjs[j] != null)
                {
                    bool found;
                    int pos = FindElement(oldObjs[j], out found);
                    _objs[pos] = oldObjs[j];
                    _ids[pos] = oldIds[j];
                }
            }
        }
    }
}
