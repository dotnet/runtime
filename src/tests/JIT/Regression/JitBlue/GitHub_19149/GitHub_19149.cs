// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Bug report thanks to @mgravell
//
// JIT bug affecting how fixed buffers are handled
//
// Affects: netcoreapp2.1, debug and release
// Does not seem to affect: netcoreapp2.0, net47
//
// the idea behind CommandBytes is that it is a fixed-sized string-like thing
// used for matching commands; it is *implemented* as a fixed buffer
// of **longs**, but: the first byte of the first element is coerced into
// a byte and used to store the length; the actual text payload (ASCII)
// starts at the second byte of the first element
//
// as far as I can tell, it is all validly implemented, and it works fine
// in isolation, however: when used in a dictionary, it goes bad;
// - items not being found despite having GetHashCode and Equals match
// - items over 1 chunk size becoming corrupted (see: ToInnerString)
//
// however, if I replace the fixed buffer with the same number of
// regular fields (_c0,_c1,_c2) and use *all the same code*, it
// all works correctly!
//
// The "Main" method populates a dictionary in the expected way,
// then attempts to find things - either via TryGetValue or manually;
// it then compares the contents
//
// Yes, this code is evil; it is for a very specific optimized scenario.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

unsafe struct CommandBytes : IEquatable<CommandBytes>
{
    private const int ChunkLength = 3;
    public const int MaxLength = (ChunkLength * 8) - 1;

    fixed long _chunks[ChunkLength];

    public override int GetHashCode()
    {
        fixed (long* lPtr = _chunks)
        {
            var hashCode = -1923861349;
            long* x = lPtr;
            for (int i = 0; i < ChunkLength; i++)
            {
                hashCode = hashCode * -1521134295 + (*x++).GetHashCode();
            }
            return hashCode;
        }
    }

    public override string ToString()
    {
        fixed (long* lPtr = _chunks)
        {
            var bPtr = (byte*)lPtr;
            return Encoding.ASCII.GetString(bPtr + 1, bPtr[0]);
        }
    }
    public int Length
    {
        get
        {
            fixed (long* lPtr = _chunks)
            {
                var bPtr = (byte*)lPtr;
                return bPtr[0];
            }
        }
    }
    public byte this[int index]
    {
        get
        {
            fixed (long* lPtr = _chunks)
            {
                byte* bPtr = (byte*)lPtr;
                int len = bPtr[0];
                if (index < 0 || index >= len) throw new IndexOutOfRangeException();
                return bPtr[index + 1];
            }
        }
    }

    public CommandBytes(string value)
    {
        value = value.ToLowerInvariant();
        var len = Encoding.ASCII.GetByteCount(value);
        if (len > MaxLength) throw new ArgumentOutOfRangeException("Maximum command length exceeed");

        fixed (long* lPtr = _chunks)
        {
            Clear(lPtr);
            byte* bPtr = (byte*)lPtr;
            bPtr[0] = (byte)len;
            fixed (char* cPtr = value)
            {
                Encoding.ASCII.GetBytes(cPtr, value.Length, bPtr + 1, len);
            }
        }
    }
    public override bool Equals(object obj) => obj is CommandBytes cb && Equals(cb);

    public string ToInnerString()
    {
        fixed (long* lPtr = _chunks)
        {
            long* x = lPtr;
            var sb = new StringBuilder();
            for (int i = 0; i < ChunkLength; i++)
            {
                if (sb.Length != 0) sb.Append(',');
                sb.Append(*x++);
            }
            return sb.ToString();
        }
    }
    public bool Equals(CommandBytes value)
    {
        fixed (long* lPtr = _chunks)
        {
            long* x = lPtr;
            long* y = value._chunks;
            for (int i = 0; i < ChunkLength; i++)
            {
                if (*x++ != *y++) return false;
            }
            return true;
        }
    }
    private static void Clear(long* ptr)
    {
        for (int i = 0; i < ChunkLength; i++)
        {
            *ptr++ = 0L;
        }
    }
}

public static class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        var lookup = new Dictionary<CommandBytes, string>();
        void Add(string val)
        {
            var cb = new CommandBytes(val);
            // prove we didn't screw up
            if (cb.ToString() != val)
                throw new InvalidOperationException("oops!");
            lookup.Add(cb, val);
        }
        Add("client");
        Add("cluster");
        Add("command");
        Add("config");
        Add("dbsize");
        Add("decr");
        Add("del");
        Add("echo");
        Add("exists");
        Add("flushall");
        Add("flushdb");
        Add("get");
        Add("incr");
        Add("incrby");
        Add("info");
        Add("keys");
        Add("llen");
        Add("lpop");
        Add("lpush");
        Add("lrange");
        Add("memory");
        Add("mget");
        Add("mset");
        Add("ping");
        Add("quit");
        Add("role");
        Add("rpop");
        Add("rpush");
        Add("sadd");
        Add("scard");
        Add("select");
        Add("set");
        Add("shutdown");
        Add("sismember");
        Add("spop");
        Add("srem");
        Add("strlen");
        Add("subscribe");
        Add("time");
        Add("unlink");
        Add("unsubscribe");

        bool HuntFor(string lookFor)
        {
            Console.WriteLine($"Looking for: '{lookFor}'");
            var hunt = new CommandBytes(lookFor);
            bool result = lookup.TryGetValue(hunt, out var found);

            if (result)
            {
                Console.WriteLine($"Found via TryGetValue: '{found}'");
            }
            else
            {
                Console.WriteLine("**NOT FOUND** via TryGetValue");
            }

            Console.WriteLine("looking manually");
            foreach (var pair in lookup)
            {
                if (pair.Value == lookFor)
                {
                    Console.WriteLine($"Found manually: '{pair.Value}'");
                    var key = pair.Key;
                    void Compare<T>(string caption, Func<CommandBytes, T> func)
                    {
                        T x = func(hunt), y = func(key);
                        Console.WriteLine($"{caption}: {EqualityComparer<T>.Default.Equals(x, y)}, '{x}' vs '{y}'");
                    }
                    Compare("GetHashCode", _ => _.GetHashCode());
                    Compare("ToString", _ => _.ToString());
                    Compare("Length", _ => _.Length);
                    Compare("ToInnerString", _ => _.ToInnerString());
                    Console.WriteLine($"Equals: {key.Equals(hunt)}, {hunt.Equals(key)}");
                    var eq = EqualityComparer<CommandBytes>.Default;

                    Console.WriteLine($"EqualityComparer: {eq.Equals(key, hunt)}, {eq.Equals(hunt, key)}");
                    Compare("eq GetHashCode", _ => eq.GetHashCode(_));
                }
            }
            Console.WriteLine();

            return result;
        }

        bool result1 = HuntFor("ping");
        bool result2 = HuntFor("subscribe");

        return (result1 && result2) ? 100 : -1;
    }
}
