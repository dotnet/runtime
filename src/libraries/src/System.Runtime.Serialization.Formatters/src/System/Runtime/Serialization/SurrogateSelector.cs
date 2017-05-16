// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    public class SurrogateSelector : ISurrogateSelector
    {
        internal readonly SurrogateHashtable _surrogates = new SurrogateHashtable(32);
        internal ISurrogateSelector _nextSelector;

        public virtual void AddSurrogate(Type type, StreamingContext context, ISerializationSurrogate surrogate)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (surrogate == null)
            {
                throw new ArgumentNullException(nameof(surrogate));
            }

            var key = new SurrogateKey(type, context);
            _surrogates.Add(key, surrogate); // Hashtable does duplicate checking.
        }

        private static bool HasCycle(ISurrogateSelector selector)
        {
            Debug.Assert(selector != null, "[HasCycle]selector!=null");

            ISurrogateSelector head = selector, tail = selector;
            while (head != null)
            {
                head = head.GetNextSelector();
                if (head == null)
                {
                    return true;
                }
                if (head == tail)
                {
                    return false;
                }
                head = head.GetNextSelector();
                tail = tail.GetNextSelector();

                if (head == tail)
                {
                    return false;
                }
            }

            return true;
        }

        // Adds another selector to check if we don't have  match within this selector.
        // The logic is:"Add this onto the list as the first thing that you check after yourself."
        public virtual void ChainSelector(ISurrogateSelector selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            // Verify that we don't try and add ourself twice.
            if (selector == this)
            {
                throw new SerializationException(SR.Serialization_DuplicateSelector);
            }

            // Verify that the argument doesn't contain a cycle.
            if (!HasCycle(selector))
            {
                throw new ArgumentException(SR.Serialization_SurrogateCycleInArgument, nameof(selector));
            }

            // Check for a cycle that would lead back to this.  We find the end of the list that we're being asked to 
            // insert for use later.
            ISurrogateSelector tempCurr = selector.GetNextSelector();
            ISurrogateSelector tempEnd = selector;
            while (tempCurr != null && tempCurr != this)
            {
                tempEnd = tempCurr;
                tempCurr = tempCurr.GetNextSelector();
            }
            if (tempCurr == this)
            {
                throw new ArgumentException(SR.Serialization_SurrogateCycle, nameof(selector));
            }

            // Check for a cycle later in the list which would be introduced by this insertion.
            tempCurr = selector;
            ISurrogateSelector tempPrev = selector;
            while (tempCurr != null)
            {
                if (tempCurr == tempEnd)
                {
                    tempCurr = GetNextSelector();
                }
                else
                {
                    tempCurr = tempCurr.GetNextSelector();
                }
                if (tempCurr == null)
                {
                    break;
                }
                if (tempCurr == tempPrev)
                {
                    throw new ArgumentException(SR.Serialization_SurrogateCycle, nameof(selector));
                }

                if (tempCurr == tempEnd)
                {
                    tempCurr = GetNextSelector();
                }
                else
                {
                    tempCurr = tempCurr.GetNextSelector();
                }


                if (tempPrev == tempEnd)
                {
                    tempPrev = GetNextSelector();
                }
                else
                {
                    tempPrev = tempPrev.GetNextSelector();
                }
                if (tempCurr == tempPrev)
                {
                    throw new ArgumentException(SR.Serialization_SurrogateCycle, nameof(selector));
                }
            }

            // Add the new selector and it's entire chain of selectors as the next thing that we check.  
            ISurrogateSelector temp = _nextSelector;
            _nextSelector = selector;
            if (temp != null)
            {
                tempEnd.ChainSelector(temp);
            }
        }

        // Get the next selector on the chain of selectors.
        public virtual ISurrogateSelector GetNextSelector() => _nextSelector;

        // Gets the surrogate for a particular type.  If this selector can't
        // provide a surrogate, it checks with all of it's children before returning null.
        public virtual ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            selector = this;

            SurrogateKey key = new SurrogateKey(type, context);
            ISerializationSurrogate temp = (ISerializationSurrogate)_surrogates[key];
            if (temp != null)
            {
                return temp;
            }
            if (_nextSelector != null)
            {
                return _nextSelector.GetSurrogate(type, context, out selector);
            }
            return null;
        }

        // Removes the surrogate associated with a given type.  Does not
        // check chained surrogates.  
        public virtual void RemoveSurrogate(Type type, StreamingContext context)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            SurrogateKey key = new SurrogateKey(type, context);
            _surrogates.Remove(key);
        }
    }

    internal sealed class SurrogateKey
    {
        internal readonly Type _type;
        internal readonly StreamingContext _context;

        internal SurrogateKey(Type type, StreamingContext context)
        {
            Debug.Assert(type != null);
            _type = type;
            _context = context;
        }

        public override int GetHashCode() => _type.GetHashCode();
    }

    // Subclass to override KeyEquals.
    internal sealed class SurrogateHashtable : Hashtable
    {
        internal SurrogateHashtable(int size) : base(size)
        {
        }

        // Must return true if the context to serialize for (givenContext)
        // is a subset of the context for which the serialization selector is provided (presentContext)
        // Note: This is done by overriding KeyEquals rather than overriding Equals() in the SurrogateKey
        // class because Equals() method must be commutative. 
        protected override bool KeyEquals(object key, object item)
        {
            SurrogateKey givenValue = (SurrogateKey)item;
            SurrogateKey presentValue = (SurrogateKey)key;
            return presentValue._type == givenValue._type &&
                   (presentValue._context.State & givenValue._context.State) == givenValue._context.State &&
                   presentValue._context.Context == givenValue._context.Context;
        }
    }
}
