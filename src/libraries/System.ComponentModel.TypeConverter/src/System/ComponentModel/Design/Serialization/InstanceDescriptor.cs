// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    ///     EventArgs for the ResolveNameEventHandler.  This event is used
    ///     by the serialization process to match a name to an object
    ///     instance.
    /// </summary>
    public sealed class InstanceDescriptor
    {
        private MemberInfo _member;
        private ICollection _arguments;
        private bool _isComplete;

        /// <summary>
        ///     Creates a new InstanceDescriptor.
        /// </summary>
        public InstanceDescriptor(MemberInfo member, ICollection arguments) : this(member, arguments, true)
        {
        }

        /// <summary>
        ///     Creates a new InstanceDescriptor.
        /// </summary>
        public InstanceDescriptor(MemberInfo member, ICollection arguments, bool isComplete)
        {
            _member = member;
            _isComplete = isComplete;

            if (arguments == null)
            {
                _arguments = new object[0];
            }
            else
            {
                object[] args = new object[arguments.Count];
                arguments.CopyTo(args, 0);
                _arguments = args;
            }

            if (member is FieldInfo)
            {
                FieldInfo fi = (FieldInfo)member;
                if (!fi.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeStatic);
                }
                if (_arguments.Count != 0)
                {
                    throw new ArgumentException(SR.InstanceDescriptorLengthMismatch);
                }
            }
            else if (member is ConstructorInfo)
            {
                ConstructorInfo ci = (ConstructorInfo)member;
                if (ci.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorCannotBeStatic);
                }
                if (_arguments.Count != ci.GetParameters().Length)
                {
                    throw new ArgumentException(SR.InstanceDescriptorLengthMismatch);
                }
            }
            else if (member is MethodInfo)
            {
                MethodInfo mi = (MethodInfo)member;
                if (!mi.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeStatic);
                }
                if (_arguments.Count != mi.GetParameters().Length)
                {
                    throw new ArgumentException(SR.InstanceDescriptorLengthMismatch);
                }
            }
            else if (member is PropertyInfo)
            {
                PropertyInfo pi = (PropertyInfo)member;
                if (!pi.CanRead)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeReadable);
                }
                MethodInfo mi = pi.GetGetMethod();
                if (mi != null && !mi.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeStatic);
                }
            }
        }

        /// <summary>
        ///     The collection of arguments that should be passed to
        ///     MemberInfo in order to create an instance.
        /// </summary>
        public ICollection Arguments
        {
            get
            {
                return _arguments;
            }
        }

        /// <summary>
        ///     Determines if the contents of this instance descriptor completely identify the instance.
        ///     This will normally be the case, but some objects may be too complex for a single method
        ///     or constructor to represent.  IsComplete can be used to identify these objects and take
        ///     additional steps to further describe their state.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return _isComplete;
            }
        }

        /// <summary>
        ///     The MemberInfo object that was passed into the constructor
        ///     of this InstanceDescriptor.
        /// </summary>
        public MemberInfo MemberInfo
        {
            get
            {
                return _member;
            }
        }

        /// <summary>
        ///     Invokes this instance descriptor, returning the object
        ///     the descriptor describes.
        /// </summary>
        public object Invoke()
        {
            object[] translatedArguments = new object[_arguments.Count];
            _arguments.CopyTo(translatedArguments, 0);

            // Instance descriptors can contain other instance
            // descriptors.  Translate them if necessary.
            //
            for (int i = 0; i < translatedArguments.Length; i++)
            {
                if (translatedArguments[i] is InstanceDescriptor)
                {
                    translatedArguments[i] = ((InstanceDescriptor)translatedArguments[i]).Invoke();
                }
            }

            if (_member is ConstructorInfo)
            {
                return ((ConstructorInfo)_member).Invoke(translatedArguments);
            }
            else if (_member is MethodInfo)
            {
                return ((MethodInfo)_member).Invoke(null, translatedArguments);
            }
            else if (_member is PropertyInfo)
            {
                return ((PropertyInfo)_member).GetValue(null, translatedArguments);
            }
            else if (_member is FieldInfo)
            {
                return ((FieldInfo)_member).GetValue(null);
            }
            else
            {
                Debug.Fail("Unrecognized reflection type in instance descriptor: " + _member.GetType().Name);
            }

            return null;
        }
    }
}

