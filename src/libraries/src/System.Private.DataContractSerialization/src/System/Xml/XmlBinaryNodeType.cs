// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    // Element           => StartElement Attribute* Content EndElement
    //                    | StartElement Attribute* Text // Text must be WithEndElement
    //                    | Array StartElement Attribute* EndElement type MB32(Count) byte[Count * sizeof(type)]

    // StartElement      => ShortElementNode Name(LocalName)
    //                    | ElementNode Name(Prefix) Name(LocalName)
    //                    | ShortDictionaryElementNode MB32(LocalName)
    //                    | PrefixDictionaryElement[A-Z]Node MB32(LocalName)
    //                    | DictionaryElementNode Name(Prefix) MB32(LocalName)
    //                    | PrefixElement[A-Z]Node Name(LocalName)

    // EndElement        => EndElementNode
    // Content           => (Element | ArrayElement | Text | Comment)*

    // Attribute         => ShortAttributeNode Name(LocalName) Text
    //                    | AttributeNode Name(Prefix) Name(LocalName) Text
    //                    | ShortDictionaryAttributeNode MB32(LocalName) Text
    //                    | DictionaryAttributeNode Name(Prefix) MB32(LocalName) Text
    //                    | ShortXmlnsAttributeNode Name(Namespace)
    //                    | XmlnsAttributeNode Name(Prefix) Name(Namespace)
    //                    | ShortDictionaryXmlnsAttributeNode MB32(Namespace)
    //                    | DictionaryXmlnsAttributeNode Name(Prefix) MB32(Namespace)
    //                    | PrefixAttribute[A-Z] Name(LocalName) Text
    //                    | PrefixDictionaryAttribute[A-Z] MB32(LocalName) Text

    // Text              => BinaryTextNode
    //                    | CharsTextNode
    //                    | EmptyTextNode
    //                    | DictionaryTextNode MB32(Id)
    //                    | ZeroTextNode
    //                    | OneTextNode
    //                    | TrueTextNode
    //                    | FalseTextNode
    //                    | Int8TextNode  Int8
    //                    | Int16TextNode Int16
    //                    | Int32TextNode Int32
    //                    | Int64TextNode Int64
    //                    | FloatTextNode Float
    //                    | DoubleTextNode Double
    //                    | DecimalTextNode Decimal
    //                    | DateTimeTextNode DateTime
    //                    | StartListNode Text* EndListNode // Restriction: Cannot nest ListNode
    //                    | UniqueIdTextNode byte[16]       // byte[16] is a Guid (from Guid.ToBinary()) (urn:uuid:xxxx-xxxx-xxx)
    //                    | GuidTextNode byte[16]           // byte[16] is a Guid (from Guid.ToBinary()) (xxxx-xxxx-xxx)
    //                    | TimeSpanNode Int64
    //                    | UInt64TextNode UInt64
    //                    | BoolTextNode Int8
    // BinaryText        => Bytes8TextNode  UInt8  byte*
    //                    | Bytes16TextNode UInt16 byte*
    //                    | Bytes32TextNode UInt31  byte*
    // CharsText         => Chars8TextNode  UInt8  byte* // UTF8Chars
    //                    | Chars16TextNode UInt16 byte*
    //                    | Chars32TextNode Unt31  byte*
    //                    | UnicodeChars8TextNode UInt8 char*
    //                    | UnicodeChars16TextNode UInt16 char*
    //                    | UnicodeChars32TextNode UInt31 char*
    //                    | QNameDictionaryTextNode UInt8 MB32(LocalName) // UInt8 0-25 => 'a'-'z'
    // Comment           => CommentNode Name(Text)
    // Name              => MB32 byte* // Length, UTF8Chars
    // MB32(x:x>=0x80)   => byte(0x80 | (x & 0x7F)) MB32(x >> 7)
    // MB32(x:x<0x80)    => byte(x)


    // In order to help differentiate text from binary (where someone mixes up format and implementation) we overlay binary
    // nodes that are illegal to start a document with text characters that are legal to start a document.  Specifically these values are:
    // ' ' = 32
    // '\t' = 9
    // '\n' = 10
    // '\r' = 13
    // '<' = 60
    // The attribute nodes (MinAttribute to MaxAttribute) overlay all of these values and are invalid as the first byte of the document

    internal enum XmlBinaryNodeType
    {
        // ProcessingInstruction = 0, // Reserved (Not supported)
        EndElement = 1,
        Comment = 2,
        Array = 3,

        MinAttribute = Array + 1,
        ShortAttribute = MinAttribute + 0,
        Attribute = MinAttribute + 1,
        ShortDictionaryAttribute = MinAttribute + 2,
        DictionaryAttribute = MinAttribute + 3,
        ShortXmlnsAttribute = MinAttribute + 4,
        XmlnsAttribute = MinAttribute + 5,
        ShortDictionaryXmlnsAttribute = MinAttribute + 6,
        DictionaryXmlnsAttribute = MinAttribute + 7,
        PrefixDictionaryAttributeA = MinAttribute + 8,
        PrefixDictionaryAttributeB = PrefixDictionaryAttributeA + 1,
        PrefixDictionaryAttributeC = PrefixDictionaryAttributeB + 1,
        PrefixDictionaryAttributeD = PrefixDictionaryAttributeC + 1,
        PrefixDictionaryAttributeE = PrefixDictionaryAttributeD + 1,
        PrefixDictionaryAttributeF = PrefixDictionaryAttributeE + 1,
        PrefixDictionaryAttributeG = PrefixDictionaryAttributeF + 1,
        PrefixDictionaryAttributeH = PrefixDictionaryAttributeG + 1,
        PrefixDictionaryAttributeI = PrefixDictionaryAttributeH + 1,
        PrefixDictionaryAttributeJ = PrefixDictionaryAttributeI + 1,
        PrefixDictionaryAttributeK = PrefixDictionaryAttributeJ + 1,
        PrefixDictionaryAttributeL = PrefixDictionaryAttributeK + 1,
        PrefixDictionaryAttributeM = PrefixDictionaryAttributeL + 1,
        PrefixDictionaryAttributeN = PrefixDictionaryAttributeM + 1,
        PrefixDictionaryAttributeO = PrefixDictionaryAttributeN + 1,
        PrefixDictionaryAttributeP = PrefixDictionaryAttributeO + 1,
        PrefixDictionaryAttributeQ = PrefixDictionaryAttributeP + 1,
        PrefixDictionaryAttributeR = PrefixDictionaryAttributeQ + 1,
        PrefixDictionaryAttributeS = PrefixDictionaryAttributeR + 1,
        PrefixDictionaryAttributeT = PrefixDictionaryAttributeS + 1,
        PrefixDictionaryAttributeU = PrefixDictionaryAttributeT + 1,
        PrefixDictionaryAttributeV = PrefixDictionaryAttributeU + 1,
        PrefixDictionaryAttributeW = PrefixDictionaryAttributeV + 1,
        PrefixDictionaryAttributeX = PrefixDictionaryAttributeW + 1,
        PrefixDictionaryAttributeY = PrefixDictionaryAttributeX + 1,
        PrefixDictionaryAttributeZ = PrefixDictionaryAttributeY + 1,
        PrefixAttributeA = PrefixDictionaryAttributeZ + 1,
        PrefixAttributeB = PrefixAttributeA + 1,
        PrefixAttributeC = PrefixAttributeB + 1,
        PrefixAttributeD = PrefixAttributeC + 1,
        PrefixAttributeE = PrefixAttributeD + 1,
        PrefixAttributeF = PrefixAttributeE + 1,
        PrefixAttributeG = PrefixAttributeF + 1,
        PrefixAttributeH = PrefixAttributeG + 1,
        PrefixAttributeI = PrefixAttributeH + 1,
        PrefixAttributeJ = PrefixAttributeI + 1,
        PrefixAttributeK = PrefixAttributeJ + 1,
        PrefixAttributeL = PrefixAttributeK + 1,
        PrefixAttributeM = PrefixAttributeL + 1,
        PrefixAttributeN = PrefixAttributeM + 1,
        PrefixAttributeO = PrefixAttributeN + 1,
        PrefixAttributeP = PrefixAttributeO + 1,
        PrefixAttributeQ = PrefixAttributeP + 1,
        PrefixAttributeR = PrefixAttributeQ + 1,
        PrefixAttributeS = PrefixAttributeR + 1,
        PrefixAttributeT = PrefixAttributeS + 1,
        PrefixAttributeU = PrefixAttributeT + 1,
        PrefixAttributeV = PrefixAttributeU + 1,
        PrefixAttributeW = PrefixAttributeV + 1,
        PrefixAttributeX = PrefixAttributeW + 1,
        PrefixAttributeY = PrefixAttributeX + 1,
        PrefixAttributeZ = PrefixAttributeY + 1,
        MaxAttribute = PrefixAttributeZ,

        MinElement = MaxAttribute + 1,
        ShortElement = MinElement,
        Element = MinElement + 1,
        ShortDictionaryElement = MinElement + 2,
        DictionaryElement = MinElement + 3,
        PrefixDictionaryElementA = MinElement + 4,
        PrefixDictionaryElementB = PrefixDictionaryElementA + 1,
        PrefixDictionaryElementC = PrefixDictionaryElementB + 1,
        PrefixDictionaryElementD = PrefixDictionaryElementC + 1,
        PrefixDictionaryElementE = PrefixDictionaryElementD + 1,
        PrefixDictionaryElementF = PrefixDictionaryElementE + 1,
        PrefixDictionaryElementG = PrefixDictionaryElementF + 1,
        PrefixDictionaryElementH = PrefixDictionaryElementG + 1,
        PrefixDictionaryElementI = PrefixDictionaryElementH + 1,
        PrefixDictionaryElementJ = PrefixDictionaryElementI + 1,
        PrefixDictionaryElementK = PrefixDictionaryElementJ + 1,
        PrefixDictionaryElementL = PrefixDictionaryElementK + 1,
        PrefixDictionaryElementM = PrefixDictionaryElementL + 1,
        PrefixDictionaryElementN = PrefixDictionaryElementM + 1,
        PrefixDictionaryElementO = PrefixDictionaryElementN + 1,
        PrefixDictionaryElementP = PrefixDictionaryElementO + 1,
        PrefixDictionaryElementQ = PrefixDictionaryElementP + 1,
        PrefixDictionaryElementR = PrefixDictionaryElementQ + 1,
        PrefixDictionaryElementS = PrefixDictionaryElementR + 1,
        PrefixDictionaryElementT = PrefixDictionaryElementS + 1,
        PrefixDictionaryElementU = PrefixDictionaryElementT + 1,
        PrefixDictionaryElementV = PrefixDictionaryElementU + 1,
        PrefixDictionaryElementW = PrefixDictionaryElementV + 1,
        PrefixDictionaryElementX = PrefixDictionaryElementW + 1,
        PrefixDictionaryElementY = PrefixDictionaryElementX + 1,
        PrefixDictionaryElementZ = PrefixDictionaryElementY + 1,
        PrefixElementA = PrefixDictionaryElementZ + 1,
        PrefixElementB = PrefixElementA + 1,
        PrefixElementC = PrefixElementB + 1,
        PrefixElementD = PrefixElementC + 1,
        PrefixElementE = PrefixElementD + 1,
        PrefixElementF = PrefixElementE + 1,
        PrefixElementG = PrefixElementF + 1,
        PrefixElementH = PrefixElementG + 1,
        PrefixElementI = PrefixElementH + 1,
        PrefixElementJ = PrefixElementI + 1,
        PrefixElementK = PrefixElementJ + 1,
        PrefixElementL = PrefixElementK + 1,
        PrefixElementM = PrefixElementL + 1,
        PrefixElementN = PrefixElementM + 1,
        PrefixElementO = PrefixElementN + 1,
        PrefixElementP = PrefixElementO + 1,
        PrefixElementQ = PrefixElementP + 1,
        PrefixElementR = PrefixElementQ + 1,
        PrefixElementS = PrefixElementR + 1,
        PrefixElementT = PrefixElementS + 1,
        PrefixElementU = PrefixElementT + 1,
        PrefixElementV = PrefixElementU + 1,
        PrefixElementW = PrefixElementV + 1,
        PrefixElementX = PrefixElementW + 1,
        PrefixElementY = PrefixElementX + 1,
        PrefixElementZ = PrefixElementY + 1,
        MaxElement = PrefixElementZ,

        // MinorVersion = MaxElement + 1, // Reserved (Not supported)

        MinText = 0x80, // Must be even 
        ZeroText = MinText,
        OneText = MinText + 1 * 2,
        FalseText = MinText + 2 * 2,
        TrueText = MinText + 3 * 2,
        Int8Text = MinText + 4 * 2,
        Int16Text = MinText + 5 * 2,
        Int32Text = MinText + 6 * 2,
        Int64Text = MinText + 7 * 2,
        FloatText = MinText + 8 * 2,
        DoubleText = MinText + 9 * 2,
        DecimalText = MinText + 10 * 2,
        DateTimeText = MinText + 11 * 2,
        Chars8Text = MinText + 12 * 2,
        Chars16Text = MinText + 13 * 2,
        Chars32Text = MinText + 14 * 2,
        Bytes8Text = MinText + 15 * 2,
        Bytes16Text = MinText + 16 * 2,
        Bytes32Text = MinText + 17 * 2,
        StartListText = MinText + 18 * 2,
        EndListText = MinText + 19 * 2,
        EmptyText = MinText + 20 * 2,
        DictionaryText = MinText + 21 * 2,
        UniqueIdText = MinText + 22 * 2,
        TimeSpanText = MinText + 23 * 2,
        GuidText = MinText + 24 * 2,
        UInt64Text = MinText + 25 * 2,
        BoolText = MinText + 26 * 2,
        UnicodeChars8Text = MinText + 27 * 2,
        UnicodeChars16Text = MinText + 28 * 2,
        UnicodeChars32Text = MinText + 29 * 2,
        QNameDictionaryText = MinText + 30 * 2,

        ZeroTextWithEndElement = ZeroText + 1,
        OneTextWithEndElement = OneText + 1,
        FalseTextWithEndElement = FalseText + 1,
        TrueTextWithEndElement = TrueText + 1,
        Int8TextWithEndElement = Int8Text + 1,
        Int16TextWithEndElement = Int16Text + 1,
        Int32TextWithEndElement = Int32Text + 1,
        Int64TextWithEndElement = Int64Text + 1,
        FloatTextWithEndElement = FloatText + 1,
        DoubleTextWithEndElement = DoubleText + 1,
        DecimalTextWithEndElement = DecimalText + 1,
        DateTimeTextWithEndElement = DateTimeText + 1,
        Chars8TextWithEndElement = Chars8Text + 1,
        Chars16TextWithEndElement = Chars16Text + 1,
        Chars32TextWithEndElement = Chars32Text + 1,
        Bytes8TextWithEndElement = Bytes8Text + 1,
        Bytes16TextWithEndElement = Bytes16Text + 1,
        Bytes32TextWithEndElement = Bytes32Text + 1,
        StartListTextWithEndElement = StartListText + 1,
        EndListTextWithEndElement = EndListText + 1,
        EmptyTextWithEndElement = EmptyText + 1,
        DictionaryTextWithEndElement = DictionaryText + 1,
        UniqueIdTextWithEndElement = UniqueIdText + 1,
        TimeSpanTextWithEndElement = TimeSpanText + 1,
        GuidTextWithEndElement = GuidText + 1,
        UInt64TextWithEndElement = UInt64Text + 1,
        BoolTextWithEndElement = BoolText + 1,
        UnicodeChars8TextWithEndElement = UnicodeChars8Text + 1,
        UnicodeChars16TextWithEndElement = UnicodeChars16Text + 1,
        UnicodeChars32TextWithEndElement = UnicodeChars32Text + 1,
        QNameDictionaryTextWithEndElement = QNameDictionaryText + 1,
        MaxText = QNameDictionaryTextWithEndElement
    }
}
