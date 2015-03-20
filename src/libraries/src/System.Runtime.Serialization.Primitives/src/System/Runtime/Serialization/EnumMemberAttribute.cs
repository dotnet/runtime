// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            get { return _value; }
            set { _value = value; _isValueSetExplicitly = true; }
        }

        public bool IsValueSetExplicitly
        {
            get { return _isValueSetExplicitly; }
        }
    }
}
