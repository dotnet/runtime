// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// A context stack is an object that can be used by serializers
    /// to push various context objects. Serialization is often
    /// a deeply nested operation, involving many different 
    /// serialization classes. These classes often need additional
    /// context information when performing serialization. As
    /// an example, an object with a property named "Enabled" may have
    /// a data type of System.Boolean. If a serializer is writing
    /// this value to a data stream it may want to know what property
    /// it is writing. It won't have this information, however, because
    /// it is only instructed to write the boolean value. In this 
    /// case the parent serializer may push a PropertyDescriptor
    /// pointing to the "Enabled" property on the context stack.
    /// What objects get pushed on this stack are up to the
    /// individual serializer objects.
    /// </summary>
    public sealed class ContextStack
    {
        private ArrayList _contextStack;

        /// <summary>
        /// Retrieves the current object on the stack, or null
        /// if no objects have been pushed.
        /// </summary>
        public object Current
        {
            get
            {
                if (_contextStack != null && _contextStack.Count > 0)
                {
                    return _contextStack[_contextStack.Count - 1];
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieves the object on the stack at the given
        /// level, or null if no object exists at that level.
        /// </summary>
        public object this[int level]
        {
            get
            {
                if (level < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(level));
                }
                if (_contextStack != null && level < _contextStack.Count)
                {
                    return _contextStack[_contextStack.Count - 1 - level];
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieves the first object on the stack that 
        /// inherits from or implements the given type, or
        /// null if no object on the stack implements the type.
        /// </summary>
        public object this[Type type]
        {
            get
            {
                if (type == null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                if (_contextStack != null)
                {
                    int level = _contextStack.Count;
                    while (level > 0)
                    {
                        object value = _contextStack[--level];
                        if (type.IsInstanceOfType(value))
                        {
                            return value;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Appends an object to the end of the stack, rather than pushing it
        /// onto the top of the stack. This method allows a serializer to communicate
        /// with other serializers by adding contextual data that does not have to
        /// be popped in order. There is no way to remove an object that was 
        /// appended to the end of the stack without popping all other objects.
        /// </summary>
        public void Append(object context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_contextStack == null)
            {
                _contextStack = new ArrayList();
            }
            _contextStack.Insert(0, context);
        }

        /// <summary>
        /// Pops the current object off of the stack, returning
        /// its value.
        /// </summary>
        public object Pop()
        {
            object context = null;

            if (_contextStack != null && _contextStack.Count > 0)
            {
                int idx = _contextStack.Count - 1;
                context = _contextStack[idx];
                _contextStack.RemoveAt(idx);
            }

            return context;
        }

        /// <summary>
        /// Pushes the given object onto the stack.
        /// </summary>
        public void Push(object context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_contextStack == null)
            {
                _contextStack = new ArrayList();
            }
            _contextStack.Add(context);
        }
    }
}

