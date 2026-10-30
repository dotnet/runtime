// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

public enum MyError: Error {
    case runtimeError(message: String)
}

var errorMessage: String = ""

public func setMyErrorMessage(message: UnsafePointer<unichar>, length: Int32) {
    errorMessage = NSString(characters: message, length: Int(length)) as String
}

public func conditionallyThrowError(willThrow: Int32) throws -> Int32 {
    if willThrow != 0 {
        throw MyError.runtimeError(message: errorMessage)
    } else {
        return 42
    }
}

@_silgen_name("conditionallyThrowErrorInFirstStackSlot")
public func conditionallyThrowErrorInFirstStackSlot(
    willThrow: Int32,
    dummy1: Int32,
    dummy2: Int32,
    dummy3: Int32,
    dummy4: Int32,
    dummy5: Int32,
    dummy6: Int32,
    dummy7: Int32
) throws -> Int32 {
    return try conditionallyThrowError(willThrow: willThrow)
}

@_silgen_name("conditionallyThrowErrorAfterSixArguments")
public func conditionallyThrowErrorAfterSixArguments(
    willThrow: Int32,
    dummy1: Int32,
    dummy2: Int32,
    dummy3: Int32,
    dummy4: Int32,
    dummy5: Int32
) throws -> Int32 {
    return try conditionallyThrowError(willThrow: willThrow)
}

private let fnvOffsetBasis: UInt = 14695981039346656037
private let fnvPrime: UInt = 1099511628211

private func mix(_ hash: UInt, _ value: UInt) -> UInt {
    return (hash ^ value) &* fnvPrime
}

private func hashBuffer(_ hash: UInt, _ buffer: UnsafeBufferPointer<UInt8>) -> UInt {
    let baseAddress = buffer.baseAddress.map { UInt(bitPattern: $0) } ?? 0
    return mix(mix(hash, baseAddress), UInt(buffer.count))
}

@_silgen_name("validateSwiftAbiOneWord")
public func validateSwiftAbiOneWord(
    key: UnsafeRawPointer,
    buffer1: UnsafeBufferPointer<UInt8>,
    buffer2: UnsafeBufferPointer<UInt8>,
    buffer3: UnsafeBufferPointer<UInt8>,
    buffer4: UnsafeBufferPointer<UInt8>,
    buffer5: UnsafeBufferPointer<UInt8>
) throws -> UInt {
    var hash = mix(fnvOffsetBasis, UInt(bitPattern: key))
    hash = hashBuffer(hash, buffer1)
    hash = hashBuffer(hash, buffer2)
    hash = hashBuffer(hash, buffer3)
    hash = hashBuffer(hash, buffer4)
    return hashBuffer(hash, buffer5)
}

public func getMyErrorMessage(from error: Error, messageLength: inout Int32) -> UnsafePointer<unichar>? {
    if let myError = error as? MyError {
        switch myError {
        case .runtimeError(let message):
            let nsMessage = message as NSString
            let buffer = UnsafeMutableBufferPointer<unichar>.allocate(capacity: nsMessage.length)
            nsMessage.getCharacters(buffer.baseAddress!, range: NSRange(location: 0, length: nsMessage.length))
            messageLength = Int32(nsMessage.length)
            return UnsafePointer(buffer.baseAddress!)
        }
    }
    messageLength = 0
    return nil
}

public func freeStringBuffer(buffer: UnsafeMutablePointer<unichar>) {
    buffer.deallocate()
}

public func nativeFunctionWithCallback(setError: Int32, _ callback: (Int32) -> Void) {
    callback(setError)
}

public func nativeFunctionWithCallback(value: Int32, setError: Int32, _ callback: (Int32, Int32) -> Int32) -> Int32 {
    return callback(value, setError)
}
