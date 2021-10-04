// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// EventArgs for the ResolveNameEventHandler. This event is used
    /// by the serialization process to match a name to an object
    /// instance.
    /// </summary>
    public sealed class InstanceDescriptor
    {
        /// <summary>
        /// Creates a new InstanceDescriptor.
        /// </summary>
        public InstanceDescriptor(MemberInfo? member, ICollection? arguments) : this(member, arguments, true)
        {
        }

        /// <summary>
        /// Creates a new InstanceDescriptor.
        /// </summary>
        public InstanceDescriptor(MemberInfo? member, ICollection? arguments, bool isComplete)
        {
            MemberInfo = member;
            IsComplete = isComplete;

            if (arguments == null)
            {
                Arguments = Array.Empty<object>();
            }
            else
            {
                object[] args = new object[arguments.Count];
                arguments.CopyTo(args, 0);
                Arguments = args;
            }

            if (member is FieldInfo fi)
            {
                if (!fi.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeStatic);
                }
                if (Arguments.Count != 0)
                {
                    throw new ArgumentException(SR.InstanceDescriptorLengthMismatch);
                }
            }
            else if (member is ConstructorInfo ci)
            {
                if (ci.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorCannotBeStatic);
                }
                if (Arguments.Count != ci.GetParameters().Length)
                {
                    throw new ArgumentException(SR.InstanceDescriptorLengthMismatch);
                }
            }
            else if (member is MethodInfo mi)
            {
                if (!mi.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeStatic);
                }
                if (Arguments.Count != mi.GetParameters().Length)
                {
                    throw new ArgumentException(SR.InstanceDescriptorLengthMismatch);
                }
            }
            else if (member is PropertyInfo pi)
            {
                if (!pi.CanRead)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeReadable);
                }
                MethodInfo? getMethod = pi.GetGetMethod();
                if (getMethod != null && !getMethod.IsStatic)
                {
                    throw new ArgumentException(SR.InstanceDescriptorMustBeStatic);
                }
            }
        }

        /// <summary>
        /// The collection of arguments that should be passed to
        /// MemberInfo in order to create an instance.
        /// </summary>
        public ICollection Arguments { get; }

        /// <summary>
        /// Determines if the contents of this instance descriptor completely identify the instance.
        /// This will normally be the case, but some objects may be too complex for a single method
        /// or constructor to represent. IsComplete can be used to identify these objects and take
        /// additional steps to further describe their state.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// The MemberInfo object that was passed into the constructor
        /// of this InstanceDescriptor.
        /// </summary>
        public MemberInfo? MemberInfo { get; }

        /// <summary>
        /// Invokes this instance descriptor, returning the object
        /// the descriptor describes.
        /// </summary>
        public object? Invoke()
        {
            object?[] translatedArguments = new object[Arguments.Count];
            Arguments.CopyTo(translatedArguments, 0);

            // Instance descriptors can contain other instance
            // descriptors. Translate them if necessary.
            for (int i = 0; i < translatedArguments.Length; i++)
            {
                if (translatedArguments[i] is InstanceDescriptor instanceDescriptor)
                {
                    translatedArguments[i] = instanceDescriptor.Invoke();
                }
            }

            if (MemberInfo is ConstructorInfo)
            {
                return ((ConstructorInfo)MemberInfo).Invoke(translatedArguments);
            }
            else if (MemberInfo is MethodInfo)
            {
                return ((MethodInfo)MemberInfo).Invoke(null, translatedArguments);
            }
            else if (MemberInfo is PropertyInfo)
            {
                return ((PropertyInfo)MemberInfo).GetValue(null, translatedArguments);
            }
            else if (MemberInfo is FieldInfo)
            {
                return ((FieldInfo)MemberInfo).GetValue(null);
            }

            return null;
        }
    }
}
