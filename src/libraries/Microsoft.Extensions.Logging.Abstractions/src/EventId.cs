// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Identifies a logging event. The primary identifier is the "Id" property, with the "Name" property providing a short description of this type of event.
    /// </summary>
    public readonly struct EventId
    {
        /// <summary>
        /// Implicitly creates an EventId from the given <see cref="int"/>.
        /// </summary>
        /// <param name="i">The <see cref="int"/> to convert to an EventId.</param>
        public static implicit operator EventId(int i)
        {
            return new EventId(i);
        }

        /// <summary>
        /// Checks if two specified <see cref="EventId"/> instances have the same value. They are equal if they have the same Id.
        /// </summary>
        /// <param name="left">The first <see cref="EventId"/>.</param>
        /// <param name="right">The second <see cref="EventId"/>.</param>
        /// <returns><c>true</c> if the objects are equal.</returns>
        public static bool operator ==(EventId left, EventId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks if two specified <see cref="EventId"/> instances have different values.
        /// </summary>
        /// <param name="left">The first <see cref="EventId"/>.</param>
        /// <param name="right">The second <see cref="EventId"/>.</param>
        /// <returns><c>true</c> if the objects are not equal.</returns>
        public static bool operator !=(EventId left, EventId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Initializes an instance of the <see cref="EventId"/> struct.
        /// </summary>
        /// <param name="id">The numeric identifier for this event.</param>
        /// <param name="name">The name of this event.</param>
        public EventId(int id, string name = null)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Gets the numeric identifier for this event.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the name of this event.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name ?? Id.ToString();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type. Two events are equal if they have the same id.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c>.</returns>
        public bool Equals(EventId other)
        {
            return Id == other.Id;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is EventId eventId && Equals(eventId);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id;
        }
    }
}
