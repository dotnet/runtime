// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __DECODEMD_H__
#define __DECODEMD_H__

// --------------------------------------------------------
// This is used to decode a bitstream encoding

class Decoder
{
public:
    Decoder();
    Decoder(PTR_BYTE bytes);
    void Init(PTR_BYTE bytes);
    unsigned Next();
    signed NextSigned();
    PTR_BYTE End();

    // --------------------------------------------------------
    // This structures contains the state of the FSM

    struct Decode
    {
        const BYTE* decoded;    //the already decoded values
        unsigned  next;   //what to do when no more decoded values
    };

private:
    // --------------------------------------------------------
    // This is used to access nibbles from a byte stream.

    class Nibbles
    {
        friend class Decoder;
    public:
        void SetContents(PTR_BYTE bytes);
        BYTE Next();
        BYTE Read();
        unsigned Bits(unsigned number);
    private:
        PTR_BYTE data;
        BYTE nibbles[2];
        unsigned next;
    };

    Decode state;
    Nibbles data;
};

// --------------------------------------------------------
// This is used to encode a bitstream encoding
class Encoder
{
public:
    Encoder(BYTE *buffer);
    void ContainsNegatives(BOOL b);
    void EncodeSigned(signed value);
    void Encode(unsigned value);
    void Encode(signed value, BOOL isSigned);
    void Add(unsigned value, unsigned length);
    void Add64(unsigned __int64 value, unsigned length);
    void Done();
    unsigned Contents(BYTE** contents);
    unsigned Length();
private:
    BYTE* buffer;
    BYTE encoding;
    unsigned unusedBits;
    BOOL done;
    BOOL signedNumbers;
    unsigned index;
};
#endif // __DECODEMD_H__
