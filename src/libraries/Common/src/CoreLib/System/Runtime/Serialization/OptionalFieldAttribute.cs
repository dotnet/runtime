// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class OptionalFieldAttribute : Attribute
    {
        private int _versionAdded = 1;

        public int VersionAdded
        {
            get { return _versionAdded; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(SR.Serialization_OptionalFieldVersionValue);
                }
                _versionAdded = value;
            }
        }
    }
}
