// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http.HPack;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace System.Net.Http.Tests.Performance
{ 
    public class HPackHuffmanBenchmark
    {
        private List<byte[]> _data;
        private byte[] _buffer;

        public HPackHuffmanBenchmark()
        {
            _data = new List<byte[]> {

                    // www.example.com
                    new byte[] { 0xf1, 0xe3, 0xc2, 0xe5, 0xf2, 0x3a, 0x6b, 0xa0, 0xab, 0x90, 0xf4, 0xff },
                    // no-cache
                    new byte[] { 0xa8, 0xeb, 0x10, 0x64, 0x9c, 0xbf },
                    // upgrade-insecure-requests
                    new byte[] { 0xb6, 0xb9, 0xac, 0x1c, 0x85, 0x58, 0xd5, 0x20, 0xa4, 0xb6, 0xc2, 0xad, 0x61, 0x7b, 0x5a, 0x54, 0x25, 0x1f },
                    // mEO7bfwFStBMwJWfW4pmg2XL25AswjrVlfcfYbxkcS2ssduZmiKoipMH9XwoTGkb+Qnq9bcjwWbwDQzsea/vMQ==
                    Convert.FromBase64String("pwanY5fGHcm7o8ZeUvJqumYX5nE3Cjx0s40Skl5x+epNwkIkt/aTZjmr0Y3/zwffi6x/72Vdn4ydPHKPxf2e0FGx30bIIA=="),
                };
            _buffer = new byte[10000];
        }

        [Params(0, 1, 2, 3)]
        public int DataIndex { get; set; }

        [Benchmark]
        public void Decode() => Huffman.Decode(_data[DataIndex], ref _buffer);
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
