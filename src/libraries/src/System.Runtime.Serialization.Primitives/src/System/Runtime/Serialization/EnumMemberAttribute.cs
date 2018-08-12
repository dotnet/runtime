// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class EnumMemberAttribute : Attribute
    {
        private string _value;
        private bool _isValueSetExplicitly;

        public EnumMemberAttribute()
        {
        }

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                _isValueSetExplicitly = true;
            }
        }

        public bool IsValueSetExplicitly => _isValueSetExplicitly;
    }
}
