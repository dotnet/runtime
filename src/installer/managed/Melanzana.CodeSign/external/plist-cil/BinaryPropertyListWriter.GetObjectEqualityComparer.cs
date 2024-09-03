using System.Collections.Generic;

namespace Claunia.PropertyList
{
    public partial class BinaryPropertyListWriter
    {
        /// <summary>
        ///     The equality comparer which is used when retrieving objects in the
        ///     <see cref="BinaryPropertyListWriter.idMap" />. The logic is slightly different from
        ///     <see cref="AddObjectEqualityComparer" />, results in two equivalent objects (UIDs mainly) being added to the
        ///     <see cref="BinaryPropertyListWriter.idMap" />. Whenever the ID for one of those equivalent objects is requested,
        ///     the first ID is always returned. This means that there are "orphan" objects in binary property lists - duplicate
        ///     objects which are never referenced -; this logic exists purely to maintain binary compatibility with Apple's
        ///     format.
        /// </summary>
        class GetObjectEqualityComparer : EqualityComparer<NSObject>
        {
            public override bool Equals(NSObject x, NSObject y) => x switch
            {
                // By default, use reference equality. Even if there are two objects - say a NSString - with the same
                // value, do not consider them equal unless they are the same instance of NSString.
                // The exceptions are UIDs, where we always compare by value, and "primitive" strings (a list of well-known
                // strings), which are treaded specially and "recycled".
                UID                                                       => x.Equals(y),
                NSNumber number when IsSerializationPrimitive(number)     => number.Equals(y),
                NSString nsString when IsSerializationPrimitive(nsString) => nsString.Equals(y),
                _                                                         => ReferenceEquals(x, y)
            };

            public override int GetHashCode(NSObject obj) => obj switch
            {
                UID u                                       => u.GetHashCode(),
                NSNumber n when IsSerializationPrimitive(n) => n.ToObject().GetHashCode(),
                NSString s when IsSerializationPrimitive(s) => s.Content.GetHashCode(),
                _                                           => obj.GetHashCode()
            };
        }
    }
}