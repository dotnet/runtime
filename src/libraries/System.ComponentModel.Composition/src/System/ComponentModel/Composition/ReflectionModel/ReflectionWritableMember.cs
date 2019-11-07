// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal abstract class ReflectionWritableMember : ReflectionMember
    {
        public abstract bool CanWrite
        {
            get;
        }

        public abstract void SetValue(object instance, object value);
    }
}
