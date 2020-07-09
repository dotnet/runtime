
#include "eventpipemetadatareader.h"

using std::wstring;
using std::vector;
using std::shared_ptr;

// TODO: this is kind of ugly and nonintuitive, it's reading a 16 byte, null terminated
// string. It's a weird overload though.
template<>
WCHAR *ReadFromBuffer(LPCBYTE eventData, ULONG cbEventData, ULONG *offset)
{
    WCHAR *start = (WCHAR *)(eventData + *offset);
    size_t length = wcslen(start);

    // Account for the null character
    *offset += (ULONG)((length + 1) * sizeof(WCHAR));

    assert(*offset <= cbEventData);
    return start;
}

EventPipeMetadataReader::EventPipeMetadataReader()
{

}

EventPipeDataDescriptor EventPipeMetadataReader::ParseType(LPCBYTE pMetadata, ULONG cbMetadata, ULONG *offset, bool v2)
{
    EventPipeDataDescriptor typeDescriptor;
    typeDescriptor.type = (EventPipeTypeCode)ReadFromBuffer<UINT32>(pMetadata, cbMetadata, offset);

    if (typeDescriptor.type == EventPipeTypeCode::ArrayType)
    {
        assert(v2 && "Array types are only supported in v2 metadata.");
        EventPipeDataDescriptor elementTypeDescriptor = ParseType(pMetadata, cbMetadata, offset, v2);
        typeDescriptor.elementType = shared_ptr<EventPipeDataDescriptor>(new EventPipeDataDescriptor(elementTypeDescriptor));
    }

    if (typeDescriptor.type == EventPipeTypeCode::Object)
    {
        // We have nested fields to read
        UINT32 fieldCount = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, offset);
        for (UINT32 f = 0; f < fieldCount; ++f)
        {
            EventPipeDataDescriptor fieldDescriptor = ParseField(pMetadata, cbMetadata, offset, v2);
            typeDescriptor.fields.push_back(fieldDescriptor);
        }
    }

    return typeDescriptor;
}

EventPipeDataDescriptor EventPipeMetadataReader::ParseField(LPCBYTE pMetadata, ULONG cbMetadata, ULONG *offset, bool v2)
{
    WCHAR *name = NULL;
    if (v2)
    {
        UINT32 paramMetadataLength = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, offset);
        UINT32 paramEndLabel = paramMetadataLength + *offset - 4;// TODO: unused, should check it
        name = ReadFromBuffer<WCHAR *>(pMetadata, cbMetadata, offset);
    }

    EventPipeDataDescriptor descriptor = ParseType(pMetadata, cbMetadata, offset, v2);

    if (!v2)
    {
        name = ReadFromBuffer<WCHAR *>(pMetadata, cbMetadata, offset);
    }

    descriptor.name = name;

    return descriptor;
}

EventPipeMetadataInstance EventPipeMetadataReader::Parse(LPCBYTE pMetadata, ULONG cbMetadata)
{
    EventPipeMetadataInstance metadata;
    if (pMetadata == NULL || cbMetadata == 0)
    {
        return metadata;
    }

    ULONG offset = 0;
    metadata.id = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, &offset);
    metadata.name = ReadFromBuffer<WCHAR *>(pMetadata, cbMetadata, &offset);
    metadata.keywords = ReadFromBuffer<INT64>(pMetadata, cbMetadata, &offset);
    metadata.version = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, &offset);
    metadata.level = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, &offset);
    metadata.opcode = 0;

    UINT32 paramCount = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, &offset);
    for (UINT32 i = 0; i < paramCount; ++i)
    {
        EventPipeDataDescriptor paramDescriptor = ParseField(pMetadata, cbMetadata, &offset, false);
        metadata.parameters.push_back(paramDescriptor);
    }

    while (offset < cbMetadata)
    {
        // We have V2 stuff to parse...
        UINT32 tagLength = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, &offset);
        BYTE tag = ReadFromBuffer<BYTE>(pMetadata, cbMetadata, &offset);

        if ((EventPipeV2Tag)tag == EventPipeV2Tag::Opcode)
        {
            metadata.opcode = ReadFromBuffer<BYTE>(pMetadata, cbMetadata, &offset);
        }
        else if ((EventPipeV2Tag)tag == EventPipeV2Tag::ParamsV2)
        {
            assert(paramCount == 0);
            UINT32 v2ParamCount = ReadFromBuffer<UINT32>(pMetadata, cbMetadata, &offset);
            for (UINT32 i = 0; i < v2ParamCount; ++i)
            {
                EventPipeDataDescriptor paramDescriptor = ParseField(pMetadata, cbMetadata, &offset, true);
                metadata.parameters.push_back(paramDescriptor);
            }
        }
        else
        {
            offset += tagLength;
            assert(offset <= cbMetadata);
        }
    }

    assert(offset == cbMetadata);

    return metadata;
}
