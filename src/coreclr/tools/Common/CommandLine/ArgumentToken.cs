// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.CommandLine
{
    internal sealed class ArgumentToken
    {
        internal ArgumentToken(string modifier, string name, string value)
        {
            Modifier = modifier;
            Name = name;
            Value = value;
        }

        public string Modifier { get; private set; }

        public string Name { get; private set; }

        public string Value { get; private set; }

        public bool IsOption
        {
            get { return !string.IsNullOrEmpty(Modifier); }
        }

        public bool IsSeparator
        {
            get { return Name == @":" || Name == @"="; }
        }

        public bool HasValue
        {
            get { return !string.IsNullOrEmpty(Value); }
        }

        public bool IsMatched { get; private set; }

        public void MarkMatched()
        {
            IsMatched = true;
        }

        private bool Equals(ArgumentToken other)
        {
            return string.Equals(Modifier, other.Modifier) &&
                   string.Equals(Name, other.Name) &&
                   string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (ReferenceEquals(obj, this))
                return true;

            var other = obj as ArgumentToken;
            return !ReferenceEquals(other, null) && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Modifier != null ? Modifier.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return HasValue
                ? string.Format(@"{0}{1}:{2}", Modifier, Name, Value)
                : string.Format(@"{0}{1}", Modifier, Name);
        }
    }
}
