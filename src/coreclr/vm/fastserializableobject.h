// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __FASTSERIALIZABLE_OBJECT_H__
#define __FASTSERIALIZABLE_OBJECT_H__

#ifdef FEATURE_PERFTRACING

class FastSerializer;

class FastSerializableObject
{
public:
    FastSerializableObject(int objectVersion, int minReaderVersion, bool isPrivate) :
        m_objectVersion(objectVersion), m_minReaderVersion(minReaderVersion), m_isPrivate(isPrivate)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual ~FastSerializableObject()
    {
        LIMITED_METHOD_CONTRACT;
    }

    // Serialize the object using the specified serializer.
    virtual void FastSerialize(FastSerializer *pSerializer) = 0;

    // Get the type name for the current object.
    virtual const char *GetTypeName() = 0;

    int GetObjectVersion() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_objectVersion;
    }

    int GetMinReaderVersion() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_minReaderVersion;
    }

    bool IsPrivate() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_isPrivate;
    }

private:
    const int m_objectVersion;
    const int m_minReaderVersion;
    const bool m_isPrivate;
};

#endif // FEATURE_PERFTRACING

#endif // _FASTSERIALIZABLE_OBJECT_H__
