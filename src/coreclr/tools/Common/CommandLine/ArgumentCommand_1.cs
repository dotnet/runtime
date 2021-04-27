// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.CommandLine
{
    public sealed class ArgumentCommand<T> : ArgumentCommand
    {
        internal ArgumentCommand(string name, T value)
            : base(name)
        {
            Value = value;
        }

        public new T Value { get; private set; }

        internal override object GetValue()
        {
            return Value;
        }
    }
}
