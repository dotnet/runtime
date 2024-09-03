using System;
using System.Collections.Generic;

namespace Claunia.PropertyList
{
    public partial class BinaryPropertyListWriter
    {
        /// <summary>
        ///     The equality comparer which is used when adding an object to the <see cref="BinaryPropertyListWriter.idMap" />
        ///     . In most cases, objects are always added. The only exception are very specific strings, which are only added once.
        /// </summary>
        class AddObjectEqualityComparer : EqualityComparer<NSObject>
        {
            public override bool Equals(NSObject x, NSObject y)
            {
                if(x is not NSString a ||
                   y is not NSString b)
                    return ReferenceEquals(x, y);

                if(!IsSerializationPrimitive(a) ||
                   !IsSerializationPrimitive(b))
                    return ReferenceEquals(x, y);

                return string.Equals(a.Content, b.Content, StringComparison.Ordinal);
            }

            public override int GetHashCode(NSObject obj)
            {
                if(obj is NSString s &&
                   IsSerializationPrimitive(s))
                    return s.Content.GetHashCode();

                return obj.GetHashCode();
            }
        }
    }
}