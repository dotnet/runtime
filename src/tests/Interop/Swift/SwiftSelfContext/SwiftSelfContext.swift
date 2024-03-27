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
