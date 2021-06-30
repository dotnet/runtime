// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DataObjectMethodAttribute : Attribute
    {
        public DataObjectMethodAttribute(DataObjectMethodType methodType) : this(methodType, false)
        {
        }

        public DataObjectMethodAttribute(DataObjectMethodType methodType, bool isDefault)
        {
            MethodType = methodType;
            IsDefault = isDefault;
        }

        public bool IsDefault { get; }

        public DataObjectMethodType MethodType { get; }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return (obj is DataObjectMethodAttribute other) && (other.MethodType == MethodType) && (other.IsDefault == IsDefault);
        }

        public override int GetHashCode() => ((int)MethodType).GetHashCode() ^ IsDefault.GetHashCode();

        public override bool Match([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return (obj is DataObjectMethodAttribute other) && (other.MethodType == MethodType);
        }
    }
}
