// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class SelfLibrary {
    public var number: Int
    public static let shared = SelfLibrary(number: 42)

    private init(number: Int) {
        self.number = number
    }

    public func getMagicNumber() -> Int {
        return self.number
    }

    public static func getInstance() -> UnsafeMutableRawPointer {
        let unmanagedInstance = Unmanaged.passUnretained(shared)
        let pointer = unmanagedInstance.toOpaque()
        return pointer
    }
}

@frozen
public struct FrozenEnregisteredStruct
{
    let a : Int64;
    let b : Int64;

    public func sum() -> Int64 {
        return a + b
    }

    public func sumWithExtraArgs(c: Float, d: Float) -> Float {
        return Float(a + b) + c + d
    }
}

@frozen
public struct FrozenNonEnregisteredStruct {
    let a : Int64;
    let b : Int64;
    let c : Int64;
    let d : Int64;
    let e : Int64;

    public func sum() -> Int64 {
        return a + b + c + d + e
    }

    public func sumWithExtraArgs(f: Float, g: Float) -> Float {
        return Float(a + b + c + d + e) + f + g
    }
}