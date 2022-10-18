using System.Formats.Asn1;

namespace Claunia.PropertyList
{
    public static class DerPropertyListWriter
    {
        public static byte[] Write(NSObject value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var asnWriter = new AsnWriter(AsnEncodingRules.DER);
            Write(asnWriter, value);
            return asnWriter.Encode();
        }

        private static void Write(AsnWriter writer, NSObject value)
        {
            switch (value)
            {
                case NSDictionary dictionary:
                    Write(writer, dictionary);
                    break;
                case NSNumber number:
                    Write(writer, number);
                    break;
                case NSString @string:
                    Write(writer, @string);
                    break;
                case NSArray array:
                    Write(writer, array);
                    break;
            }
        }

        private static void Write(AsnWriter writer, NSDictionary dictionary)
        {
            using (writer.PushSetOf())
            {
                foreach (KeyValuePair<string, NSObject> entry in dictionary)
                {
                    using (writer.PushSequence())
                    {
                        writer.WriteCharacterString(UniversalTagNumber.UTF8String, entry.Key);
                        Write(writer, entry.Value);
                    }
                }
            }
        }

        private static void Write(AsnWriter writer, NSArray array)
        {
            using (writer.PushSequence())
            {
                foreach (NSObject item in array)
                {
                    Write(writer, item);
                }
            }
        }

        private static void Write(AsnWriter writer, NSString @string)
        {
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, @string.ToString());
        }

        private static void Write(AsnWriter writer, NSNumber number)
        {
            if (number.isBoolean())
            {
                writer.WriteBoolean(number.ToBool());
                return;
            }
            else if (number.isInteger())
            {
                writer.WriteInteger(number.ToInt());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
