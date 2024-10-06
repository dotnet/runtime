// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct HasherFNV1a {

    private var hash: UInt = 14_695_981_039_346_656_037
    private let prime: UInt = 1_099_511_628_211

    mutating func combine<T>(_ val: T) {
        for byte in withUnsafeBytes(of: val, Array.init) {
            hash ^= UInt(byte)
            hash = hash &* prime
        }
    }

    func finalize() -> Int {
        Int(truncatingIfNeeded: hash)
    }
}

@frozen public struct F0 {
    public var elements: (UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8)
}

public func swiftFunc0(a0: F0) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.elements.0);
    hasher.combine(a0.elements.1);
    hasher.combine(a0.elements.2);
    hasher.combine(a0.elements.3);
    hasher.combine(a0.elements.4);
    hasher.combine(a0.elements.5);
    hasher.combine(a0.elements.6);
    hasher.combine(a0.elements.7);
    hasher.combine(a0.elements.8);
    hasher.combine(a0.elements.9);
    hasher.combine(a0.elements.10);
    hasher.combine(a0.elements.11);
    hasher.combine(a0.elements.12);
    hasher.combine(a0.elements.13);
    hasher.combine(a0.elements.14);
    hasher.combine(a0.elements.15);
    hasher.combine(a0.elements.16);
    hasher.combine(a0.elements.17);
    hasher.combine(a0.elements.18);
    hasher.combine(a0.elements.19);
    hasher.combine(a0.elements.20);
    hasher.combine(a0.elements.21);
    hasher.combine(a0.elements.22);
    hasher.combine(a0.elements.23);
    hasher.combine(a0.elements.24);
    hasher.combine(a0.elements.25);
    hasher.combine(a0.elements.26);
    hasher.combine(a0.elements.27);
    hasher.combine(a0.elements.28);
    hasher.combine(a0.elements.29);
    hasher.combine(a0.elements.30);
    hasher.combine(a0.elements.31);
    return hasher.finalize()
}

@frozen public struct F1 {
    public var elements: (Int32, Int32, Int32, Int32, Int32, Int32, Int32, Int32)
}

public func swiftFunc1(a0: F1) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.elements.0);
    hasher.combine(a0.elements.1);
    hasher.combine(a0.elements.2);
    hasher.combine(a0.elements.3);
    hasher.combine(a0.elements.4);
    hasher.combine(a0.elements.5);
    hasher.combine(a0.elements.6);
    hasher.combine(a0.elements.7);
    return hasher.finalize()
}

@frozen public struct F2 {
    public var elements: (UInt64, UInt64, UInt64, UInt64, UInt64, UInt64)
}

public func swiftFunc2(a0: F2) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.elements.0);
    hasher.combine(a0.elements.1);
    hasher.combine(a0.elements.2);
    hasher.combine(a0.elements.3);
    hasher.combine(a0.elements.4);
    hasher.combine(a0.elements.5);
    return hasher.finalize()
}

@frozen public struct F3 {
    public var element:  UInt8
}

public func swiftFunc3(a0: F3) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.element);
    return hasher.finalize()
}
