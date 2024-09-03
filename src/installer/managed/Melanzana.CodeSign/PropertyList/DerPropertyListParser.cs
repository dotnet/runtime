using Claunia.PropertyList;
using System.Formats.Asn1;

namespace Melanzana.CodeSign.PropertyList
{
    /// <summary>
    /// Parses property lists in DER-encoded format.
    /// </summary>
    public class DerPropertyListParser
    {
        /// <summary>
        /// Parses a parses a property list in DER-encoded format.
        /// </summary>
        /// <param name="source">
        /// The DER-encoded property list.
        /// </param>
        /// <returns>
        /// A <see cref="NSObject"/> which represents the DER-encoded property list.
        /// </returns>
        public static NSObject Parse(ReadOnlyMemory<byte> source)
        {
            AsnReader reader = new AsnReader(source, AsnEncodingRules.DER);
            return Parse(reader);
        }

        private static NSArray ParseArray(AsnReader reader)
        {
            var sequenceReader = reader.ReadSequence();

            NSArray value = new NSArray();

            while (sequenceReader.HasData)
            {
                value.Add(Parse(sequenceReader));
            }

            return value;
        }

        private static NSDictionary ParseDictionary(AsnReader reader)
        {
            var setReader = reader.ReadSetOf();
            NSDictionary dictionary = new NSDictionary();

            // We're expecting the following structure:
            // SET <- dictionary
            //   SEQUENCE <- key/value
            while (setReader.HasData)
            {
                var sequenceReader = setReader.ReadSequence();
                var key = sequenceReader.ReadCharacterString(UniversalTagNumber.UTF8String);
                var value = Parse(sequenceReader);

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        private static NSObject Parse(AsnReader reader)
        {
            var tag = reader.PeekTag();

            if (tag.TagClass != TagClass.Universal)
            {
                throw new PropertyListFormatException($"Unexpected tag {tag}");
            }

            switch ((UniversalTagNumber)tag.TagValue)
            {
                case UniversalTagNumber.Boolean:
                    // The boolean value 'true' is encoded as 0xFF instead of 0x01, which violates the
                    // DER standards. Read it manually instead.
                    var encodedValue = reader.ReadEncodedValue();
                    var boolValue = AsnDecoder.ReadBoolean(encodedValue.Span, AsnEncodingRules.BER, out int _);
                    return new NSNumber(boolValue);

                case UniversalTagNumber.UTF8String:
                    return new NSString(reader.ReadCharacterString(UniversalTagNumber.UTF8String));

                case UniversalTagNumber.Set:
                    return ParseDictionary(reader);

                case UniversalTagNumber.Sequence:
                    return ParseArray(reader);

                default:
                    throw new PropertyListFormatException($"Unexpected tag {tag}");
            }
        }
    }
}
