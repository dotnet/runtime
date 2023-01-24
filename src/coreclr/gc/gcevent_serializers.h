// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCEVENT_SERIALIZERS_H__
#define __GCEVENT_SERIALIZERS_H__

/*
 * gcevent_serializers.h - Serialization traits and plumbing for
 * serializing dynamic events.
 *
 * Dynamic events are events that can be fired by the GC without prior
 * knowledge of the EE. In order to accomplish this, the GC sends raw
 * bytes to the EE using the `IGCToCLR::FireDynamicEvent` callback, which
 * the EE will then fire as its own event.
 *
 * In order to keep the friction of adding new dynamic events low, this
 * file defines a simple ETW-style binary serialization format that
 * is efficient and easy to both serialize and deserialize.
 *
 * ## Serializing Types
 *
 * This file makes use of `EventSerializationTraits` to serialize
 * types. A type can opt-in to serialization using the mechanisms
 * in this file by specializing the `EventSerializationTraits` template,
 * providing implementations of `Serialize` and `SerializedSize`.
 *
 * If you attempt to serialize a type that does not implement this trait,
 * you will receive an error message like this:
 *
 * bool gc_event::EventSerializationTraits<Head>::Serialize(const T&,uint8_t **)': attempting to reference a deleted function
 * with
 *  [
 *      Head=<your type you tried to serialize>,
 *      T=<your type you tried to serialize>
 *  ]
 *
 * If you get this message, you will need to specialize `EventSerializationTraits`
 * for the type you want to serialize.
 */

#ifdef _MSC_VER
#define ByteSwap32 _byteswap_ulong
#define ByteSwap64 _byteswap_uint64
#else
#define ByteSwap32 __bulitin_bswap32
#define ByteSwap64 __builtin_bswap64
#endif // MSC_VER

namespace gc_event
{

/*
 * `EventSerializatonTraits` is a trait implemented by types that
 * can be serialized to the payload of a dynamic event.
 */
template<class T>
struct EventSerializationTraits
{
    /*
     * Serializes the value `value` to the buffer `buffer`, incrementing
     * the buffer double-pointer to point to the next byte to be written.
     *
     * It is the responsibility of the caller to ensure that the buffer is
     * large enough to accommodate the serialized form of T.
     */
    static void Serialize(const T& value, uint8_t** buffer) = delete;

    /*
     * Returns the size of the value `value` if it were to be serialized.
     */
    static size_t SerializedSize(const T& value) = delete;
};

/*
 * EventSerializationTraits implementation for uint32_t. Other integral types
 * can follow this pattern.
 *
 * The convention here is that integral types are always serialized as
 * little-endian.
 */
template<>
struct EventSerializationTraits<uint32_t>
{
    static void Serialize(const uint32_t& value, uint8_t** buffer)
    {
#if defined(BIGENDIAN)
        **((uint32_t**)buffer) = ByteSwap32(value);
#else
        **((uint32_t**)buffer) = value;
#endif // BIGENDIAN
        *buffer += sizeof(uint32_t);
    }

    static size_t SerializedSize(const uint32_t& value)
    {
        return sizeof(uint32_t);
    }
};

/*
 * Helper routines for serializing lists of arguments.
 */

/*
 * Given a list of arguments , returns the total size of
 * the buffer required to fully serialize the list of arguments.
 */
template<class Head>
size_t SerializedSize(Head head)
{
    return EventSerializationTraits<Head>::SerializedSize(head);
}

template<class Head, class... Tail>
size_t SerializedSize(Head head, Tail... tail)
{
    return EventSerializationTraits<Head>::SerializedSize(head) + SerializedSize(tail...);
}

/*
 * Given a list of arguments and a list of actual parameters, serialize
 * the arguments into the buffer that's given to us.
 */
template<class Head>
void Serialize(uint8_t** buf, Head head)
{
    EventSerializationTraits<Head>::Serialize(head, buf);
}

template<class Head, class... Tail>
void Serialize(uint8_t** buf, Head head, Tail... tail)
{
    EventSerializationTraits<Head>::Serialize(head, buf);
    Serialize(buf, tail...);
}

} // namespace gc_event

#endif // __GCEVENT_SERIALIZERS_H__

