// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FASTSERIALIZABLE_OBJECT_H__
#define __FASTSERIALIZABLE_OBJECT_H__

#ifdef FEATURE_PERFTRACING

class FastSerializer;

class FastSerializableObject
{

public:

    // Virtual destructor to ensure that derived class destructors get called.
    virtual ~FastSerializableObject()
    {
        LIMITED_METHOD_CONTRACT;
    }

    // Serialize the object using the specified serializer.
    virtual void FastSerialize(FastSerializer *pSerializer) = 0;

    // Get the type name for the current object.
    virtual const char* GetTypeName() = 0;
};

#endif // FEATURE_PERFTRACING

#endif // _FASTSERIALIZABLE_OBJECT_H__
