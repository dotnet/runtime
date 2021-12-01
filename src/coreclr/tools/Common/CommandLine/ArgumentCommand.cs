// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.CommandLine
{
    public abstract class ArgumentCommand
    {
        internal ArgumentCommand(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public string Help { get; set; }

        public object Value
        {
            get { return GetValue(); }
        }

        public bool IsHidden { get; set; }

        public bool IsActive { get; private set; }

        internal abstract object GetValue();

        internal void MarkActive()
        {
            IsActive = true;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
