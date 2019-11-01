// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json
{
    internal abstract class ImmutableCollectionCreator
    {
        public abstract void RegisterCreatorDelegateFromMethod(MethodInfo creator);
        public abstract bool CreateImmutableEnumerable(IList items, out IEnumerable collection);
        public abstract bool CreateImmutableDictionary(IDictionary items, out IDictionary collection);
    }

    internal sealed class ImmutableEnumerableCreator<TElement, TCollection> : ImmutableCollectionCreator
        where TCollection : IEnumerable<TElement>
    {
        private Func<IEnumerable<TElement>, TCollection> _creatorDelegate;

        public override void RegisterCreatorDelegateFromMethod(MethodInfo creator)
        {
            Debug.Assert(_creatorDelegate == null);
            _creatorDelegate = (Func<IEnumerable<TElement>, TCollection>)creator.CreateDelegate(typeof(Func<IEnumerable<TElement>, TCollection>));
        }

        public override bool CreateImmutableEnumerable(IList items, out IEnumerable collection)
        {
            Debug.Assert(_creatorDelegate != null);
            collection = _creatorDelegate(CreateGenericTElementIEnumerable(items));
            return true;
        }

        public override bool CreateImmutableDictionary(IDictionary items, out IDictionary collection)
        {
            // Shouldn't be calling this method for immutable dictionaries.
            collection = default;
            return false;
        }

        private IEnumerable<TElement> CreateGenericTElementIEnumerable(IList sourceList)
        {
            foreach (object item in sourceList)
            {
                yield return (TElement)item;
            }
        }
    }

    internal sealed class ImmutableDictionaryCreator<TElement, TCollection> : ImmutableCollectionCreator
        where TCollection : IReadOnlyDictionary<string, TElement>
    {
        private Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection> _creatorDelegate;

        public override void RegisterCreatorDelegateFromMethod(MethodInfo creator)
        {
            Debug.Assert(_creatorDelegate == null);
            _creatorDelegate = (Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>)creator.CreateDelegate(
                typeof(Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>));
        }

        public override bool CreateImmutableEnumerable(IList items, out IEnumerable collection)
        {
            // Shouldn't be calling this method for immutable non-dictionaries.
            collection = default;
            return false;
        }

        public override bool CreateImmutableDictionary(IDictionary items, out IDictionary collection)
        {
            Debug.Assert(_creatorDelegate != null);
            collection = (IDictionary)_creatorDelegate(CreateGenericTElementIDictionary(items));
            return true;
        }

        private IEnumerable<KeyValuePair<string, TElement>> CreateGenericTElementIDictionary(IDictionary sourceDictionary)
        {
            foreach (DictionaryEntry item in sourceDictionary)
            {
                yield return new KeyValuePair<string, TElement>((string)item.Key, (TElement)item.Value);
            }
        }
    }
}
