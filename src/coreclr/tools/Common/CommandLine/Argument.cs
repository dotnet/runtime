// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Internal.CommandLine
{
    public abstract class Argument
    {
        internal Argument(ArgumentCommand command, IEnumerable<string> names, bool isOption, bool isRequired)
        {
            var nameArray = names.ToArray();
            Command = command;
            Name = nameArray.First();
            Names = new ReadOnlyCollection<string>(nameArray);
            IsOption = isOption;
            IsRequired = isRequired;
        }

        public ArgumentCommand Command { get; private set; }

        public string Name { get; private set; }

        public ReadOnlyCollection<string> Names { get; private set; }

        public string Help { get; set; }

        public bool IsOption { get; private set; }

        public bool IsParameter
        {
            get { return !IsOption; }
        }

        public bool IsSpecified { get; private set; }

        public bool IsHidden { get; set; }

        public bool IsRequired { get; private set; }

        public virtual bool IsList
        {
            get { return false; }
        }

        public object Value
        {
            get { return GetValue(); }
        }

        public object DefaultValue
        {
            get { return GetDefaultValue(); }
        }

        public bool IsActive
        {
            get { return Command == null || Command.IsActive; }
        }

        public abstract bool IsFlag { get; }

        internal abstract object GetValue();

        internal abstract object GetDefaultValue();

        internal void MarkSpecified()
        {
            IsSpecified = true;
        }

        public string GetDisplayName()
        {
            return GetDisplayName(Name);
        }

        public IEnumerable<string> GetDisplayNames()
        {
            return Names.Select(GetDisplayName);
        }

        private string GetDisplayName(string name)
        {
            return IsOption ? GetOptionDisplayName(name) : GetParameterDisplayName(name);
        }

        private static string GetOptionDisplayName(string name)
        {
            var modifier = name.Length == 1 ? @"-" : @"--";
            return modifier + name;
        }

        private static string GetParameterDisplayName(string name)
        {
            return @"<" + name + @">";
        }

        public virtual string GetDisplayValue()
        {
            return Value == null ? string.Empty : Value.ToString();
        }

        public override string ToString()
        {
            return GetDisplayName();
        }
    }
}
