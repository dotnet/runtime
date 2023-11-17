// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

public enum MyError: Error {
    case runtimeError(message: String)
}

var errorMessage: String = ""

public func setMyErrorMessage(bytes: UnsafePointer<UInt8>, length: Int) {
    let data = Data(bytes: bytes, count: length)
    errorMessage = String(data: data, encoding: .utf8)!
}

public func conditionallyThrowError(willThrow: Bool) throws -> Int {
    if willThrow {
        throw MyError.runtimeError(message: errorMessage)
    } else {
        return 42
    }
}

public func getMyErrorMessage(from pointer: UnsafePointer<MyError>) -> UnsafePointer<UInt8>? {
    let pointerValue = UInt(bitPattern: pointer)
    var offsetValue: UInt
#if arch(arm64)
    offsetValue = 0x48
#elseif arch(x86_64)
    offsetValue = 0x20
#else
    fatalError("Unsupported architecture")
#endif
    let offsetPointerValue = pointerValue + offsetValue
    let offsetPointer = UnsafeRawPointer(bitPattern: offsetPointerValue)
    
    if let offsetErrorPointer = offsetPointer?.assumingMemoryBound(to: MyError.self) {
        let errorInstance = offsetErrorPointer.pointee
        switch errorInstance {
        case .runtimeError(let message):
            let messageBytes: [UInt8] = Array(message.utf8)
            let buffer = UnsafeMutableBufferPointer<UInt8>.allocate(capacity: messageBytes.count)
            _ = buffer.initialize(from: messageBytes)
            return UnsafePointer(buffer.baseAddress!)
        }
    } else {
        return nil
    }
}
