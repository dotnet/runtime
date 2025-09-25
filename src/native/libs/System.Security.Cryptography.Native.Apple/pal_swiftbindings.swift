// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import CryptoKit
import Foundation

protocol NonceProtocol {
    init<D>(data: D) throws where D : DataProtocol
}

protocol SealedBoxProtocol {
    associatedtype Nonce : NonceProtocol

    var ciphertext: Data { get }
    var tag: Data { get }

    init<C, T>(
        nonce: Nonce,
        ciphertext: C,
        tag: T
    ) throws where C : DataProtocol, T : DataProtocol
}

@available(iOS 13, tvOS 13, *)
protocol AEADSymmetricAlgorithm {
    associatedtype SealedBox : SealedBoxProtocol

    static func seal<Plaintext>(_ plaintext: Plaintext, using key: SymmetricKey, nonce: SealedBox.Nonce?) throws -> SealedBox where Plaintext: DataProtocol
    static func seal<Plaintext, AuthenticatedData>(_ plaintext: Plaintext, using key: SymmetricKey, nonce: SealedBox.Nonce?, authenticating additionalData: AuthenticatedData) throws -> SealedBox where Plaintext: DataProtocol, AuthenticatedData: DataProtocol
    static func open<AuthenticatedData>(_ sealedBox: SealedBox, using key: SymmetricKey, authenticating additionalData: AuthenticatedData) throws -> Data where AuthenticatedData: DataProtocol
    static func open(_ sealedBox: SealedBox, using key: SymmetricKey) throws -> Data
}

@available(iOS 13, tvOS 13, *)
extension AES.GCM.Nonce: NonceProtocol {}

@available(iOS 13, tvOS 13, *)
extension AES.GCM.SealedBox: SealedBoxProtocol {
    typealias Nonce = AES.GCM.Nonce
}

@available(iOS 13, tvOS 13, *)
extension AES.GCM: AEADSymmetricAlgorithm {}

@available(iOS 13, tvOS 13, *)
extension ChaChaPoly.Nonce: NonceProtocol {}

@available(iOS 13, tvOS 13, *)
extension ChaChaPoly.SealedBox: SealedBoxProtocol {
    typealias Nonce = ChaChaPoly.Nonce
}

@available(iOS 13, tvOS 13, *)
extension ChaChaPoly: AEADSymmetricAlgorithm {}

@available(iOS 13, tvOS 13, *)
func encrypt<Algorithm>(
    _ algorithm: Algorithm.Type,
    key: UnsafeBufferPointer<UInt8>,
    nonceData: UnsafeBufferPointer<UInt8>,
    plaintext: UnsafeBufferPointer<UInt8>,
    cipherText: UnsafeMutableBufferPointer<UInt8>,
    tag: UnsafeMutableBufferPointer<UInt8>,
    aad: UnsafeBufferPointer<UInt8>) throws where Algorithm: AEADSymmetricAlgorithm {

    let symmetricKey = SymmetricKey(data: key)

    let nonce = try Algorithm.SealedBox.Nonce(data: nonceData)

    let result = try Algorithm.seal(plaintext, using: symmetricKey, nonce: nonce, authenticating: aad)

    // Copy results out of the SealedBox as the Data objects returned here are sometimes slices,
    // which don't have a correct implementation of copyBytes.
    // See https://github.com/apple/swift-foundation/issues/638 for more information.
    let resultCiphertext = Data(result.ciphertext)
    let resultTag = Data(result.tag)

    _ = resultCiphertext.copyBytes(to: cipherText)
    _ = resultTag.copyBytes(to: tag)
}

@available(iOS 13, tvOS 13, *)
func decrypt<Algorithm>(
    _ algorithm: Algorithm.Type,
    key: UnsafeBufferPointer<UInt8>,
    nonceData: UnsafeBufferPointer<UInt8>,
    cipherText: UnsafeBufferPointer<UInt8>,
    tag: UnsafeBufferPointer<UInt8>,
    plaintext: UnsafeMutableBufferPointer<UInt8>,
    aad: UnsafeBufferPointer<UInt8>) throws where Algorithm: AEADSymmetricAlgorithm {

    let symmetricKey = SymmetricKey(data: key)

    let nonce = try Algorithm.SealedBox.Nonce(data: nonceData)

    let sealedBox = try Algorithm.SealedBox(nonce: nonce, ciphertext: cipherText, tag: tag)

    let result = try Algorithm.open(sealedBox, using: symmetricKey, authenticating: aad)

    _ = result.copyBytes(to: plaintext)
}

@_silgen_name("AppleCryptoNative_ChaCha20Poly1305Encrypt")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_ChaCha20Poly1305Encrypt(
    key: UnsafeBufferPointer<UInt8>,
    nonceData: UnsafeBufferPointer<UInt8>,
    plaintext: UnsafeBufferPointer<UInt8>,
    cipherText: UnsafeMutableBufferPointer<UInt8>,
    tag: UnsafeMutableBufferPointer<UInt8>,
    aad: UnsafeBufferPointer<UInt8>
) throws {
    return try encrypt(
        ChaChaPoly.self,
        key: key,
        nonceData: nonceData,
        plaintext: plaintext,
        cipherText: cipherText,
        tag: tag,
        aad: aad)
}

@_silgen_name("AppleCryptoNative_ChaCha20Poly1305Decrypt")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_ChaCha20Poly1305Decrypt(
    key: UnsafeBufferPointer<UInt8>,
    nonceData: UnsafeBufferPointer<UInt8>,
    cipherText: UnsafeBufferPointer<UInt8>,
    tag: UnsafeBufferPointer<UInt8>,
    plaintext: UnsafeMutableBufferPointer<UInt8>,
    aad: UnsafeBufferPointer<UInt8>
) throws {
    return try decrypt(
        ChaChaPoly.self,
        key: key,
        nonceData: nonceData,
        cipherText: cipherText,
        tag: tag,
        plaintext: plaintext,
        aad: aad);
}

@_silgen_name("AppleCryptoNative_AesGcmEncrypt")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_AesGcmEncrypt(
    key: UnsafeBufferPointer<UInt8>,
    nonceData: UnsafeBufferPointer<UInt8>,
    plaintext: UnsafeBufferPointer<UInt8>,
    cipherText: UnsafeMutableBufferPointer<UInt8>,
    tag: UnsafeMutableBufferPointer<UInt8>,
    aad: UnsafeBufferPointer<UInt8>
) throws {
    return try encrypt(
        AES.GCM.self,
        key: key,
        nonceData: nonceData,
        plaintext: plaintext,
        cipherText: cipherText,
        tag: tag,
        aad: aad)
}

@_silgen_name("AppleCryptoNative_AesGcmDecrypt")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_AesGcmDecrypt(
    key: UnsafeBufferPointer<UInt8>,
    nonceData: UnsafeBufferPointer<UInt8>,
    cipherText: UnsafeBufferPointer<UInt8>,
    tag: UnsafeBufferPointer<UInt8>,
    plaintext: UnsafeMutableBufferPointer<UInt8>,
    aad: UnsafeBufferPointer<UInt8>
) throws {
    return try decrypt(
        AES.GCM.self,
        key: key,
        nonceData: nonceData,
        cipherText: cipherText,
        tag: tag,
        plaintext: plaintext,
        aad: aad);
}

@_silgen_name("AppleCryptoNative_IsAuthenticationFailure")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_IsAuthenticationFailure(error: Error) -> Bool {
    if let error = error as? CryptoKitError {
        switch error {
        case .authenticationFailure:
            return true
        default:
            return false
        }
    }
    return false
}

// Must remain in sync with PAL_HashAlgorithm from managed side.
enum PAL_HashAlgorithm: Int32 {
    case unknown = 0
    case md5 = 1
    case sha1 = 2
    case sha256 = 3
    case sha384 = 4
    case sha512 = 5
}

enum HKDFError: Error {
    case unknownHashAlgorithm
}

struct Logger {
    private static let stdoutLock = NSLock()

    static func log(_ parts: Any..., separator: String = " ", terminator: String = "\n") {
        let message = parts.map { String(describing: $0) }.joined(separator: separator) + terminator

        stdoutLock.lock()
        defer { stdoutLock.unlock() }

        if let data = message.data(using: .utf8) {
            FileHandle.standardOutput.write(data)
        }
    }

    static func hexEncode<C: Collection>(_ bytes: C) -> String where C.Element == UInt8 {
        var s = ""
        s.reserveCapacity(64)
        s.append("<\(bytes.count)> ")
        for b in bytes {
            s.append(String(UnicodeScalar(_hex[Int(b >> 4)])))
            s.append(String(UnicodeScalar(_hex[Int(b & 0x0F)])))
        }
        return s
    }

    private static let _hex: [UInt8] = Array("0123456789abcdef".utf8)
}

@_silgen_name("AppleCryptoNative_HKDFExpand")
@available(iOS 14, tvOS 14, *)
public func AppleCryptoNative_HKDFExpand(
    hashAlgorithm: Int32,
    prkPtr: UnsafeMutableRawPointer,
    prkLength: Int32,
    infoPtr: UnsafeMutableRawPointer,
    infoLength: Int32,
    destinationPtr: UnsafeMutablePointer<UInt8>,
    destinationLength: Int32) -> Int32 {

    let prk = Data(bytesNoCopy: prkPtr, count: Int(prkLength), deallocator: Data.Deallocator.none)
    let info = Data(bytesNoCopy: infoPtr, count: Int(infoLength), deallocator: Data.Deallocator.none)
    let destinationLengthInt = Int(destinationLength)

    Logger.log("alg:", hashAlgorithm, "prk:", Logger.hexEncode(prk), "info:", Logger.hexEncode(info))

    guard let algorithm = PAL_HashAlgorithm(rawValue: hashAlgorithm) else {
        return -2
    }

    let keyFactory : () throws -> ContiguousBytes = {
        switch algorithm {
            case .unknown:
                throw HKDFError.unknownHashAlgorithm
            case .md5:
                return HKDF<Insecure.MD5>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destinationLengthInt)
            case .sha1:
                return HKDF<Insecure.SHA1>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destinationLengthInt)
            case .sha256:
                return HKDF<SHA256>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destinationLengthInt)
            case .sha384:
                return HKDF<SHA384>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destinationLengthInt)
            case .sha512:
                return HKDF<SHA512>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destinationLengthInt)
        }
    }

    guard let key = try? keyFactory() else {
        return -1
    }

    return key.withUnsafeBytes { keyBytes in
        let destination = UnsafeMutableRawBufferPointer(start: destinationPtr, count: destinationLengthInt)
        return Int32(keyBytes.copyBytes(to: destination))
    }
}

@_silgen_name("AppleCryptoNative_HKDFExtract")
@available(iOS 14, tvOS 14, *)
public func AppleCryptoNative_HKDFExtract(
    hashAlgorithm: Int32,
    ikmPtr: UnsafeMutableRawPointer,
    ikmLength: Int32,
    saltPtr: UnsafeMutableRawPointer,
    saltLength: Int32,
    destinationPtr: UnsafeMutablePointer<UInt8>,
    destinationLength: Int32) -> Int32 {

    let ikm = Data(bytesNoCopy: ikmPtr, count: Int(ikmLength), deallocator: Data.Deallocator.none)
    let salt = Data(bytesNoCopy: saltPtr, count: Int(saltLength), deallocator: Data.Deallocator.none)
    let destinationLengthInt = Int(destinationLength)

    Logger.log("alg:", hashAlgorithm, "ikm:", Logger.hexEncode(ikm), "salt:", Logger.hexEncode(salt))
    let key = SymmetricKey(data: ikm)

    guard let algorithm = PAL_HashAlgorithm(rawValue: hashAlgorithm) else {
        return -2
    }

    let prkFactory : () throws -> ContiguousBytes  = {
        switch algorithm {
            case .unknown:
                throw HKDFError.unknownHashAlgorithm
            case .md5:
                return HKDF<Insecure.MD5>.extract(inputKeyMaterial: key, salt: salt)
            case .sha1:
                return HKDF<Insecure.SHA1>.extract(inputKeyMaterial: key, salt: salt)
            case .sha256:
                return HKDF<SHA256>.extract(inputKeyMaterial: key, salt: salt)
            case .sha384:
                return HKDF<SHA384>.extract(inputKeyMaterial: key, salt: salt)
            case .sha512:
                return HKDF<SHA512>.extract(inputKeyMaterial: key, salt: salt)
        }
    }

    guard let prk = try? prkFactory() else {
        return -1
    }

    return prk.withUnsafeBytes { prkBytes in
        let destination = UnsafeMutableRawBufferPointer(start: destinationPtr, count: destinationLengthInt)
        return Int32(prkBytes.copyBytes(to: destination))
    }
}

@_silgen_name("AppleCryptoNative_HKDFDeriveKey")
@available(iOS 14, tvOS 14, *)
public func AppleCryptoNative_HKDFDeriveKey(
    hashAlgorithm: Int32,
    ikmPtr: UnsafeMutableRawPointer,
    ikmLength: Int32,
    saltPtr: UnsafeMutableRawPointer,
    saltLength: Int32,
    infoPtr: UnsafeMutableRawPointer,
    infoLength: Int32,
    destinationPtr: UnsafeMutablePointer<UInt8>,
    destinationLength: Int32) -> Int32 {

    let ikm = Data(bytesNoCopy: ikmPtr, count: Int(ikmLength), deallocator: Data.Deallocator.none)
    let salt = Data(bytesNoCopy: saltPtr, count: Int(saltLength), deallocator: Data.Deallocator.none)
    let info = Data(bytesNoCopy: infoPtr, count: Int(infoLength), deallocator: Data.Deallocator.none)
    let destinationLengthInt = Int(destinationLength)

    Logger.log("alg:", hashAlgorithm, "ikm:", Logger.hexEncode(ikm), "salt:", Logger.hexEncode(info), "salt:", Logger.hexEncode(info))
    let key = SymmetricKey(data: ikm)

    guard let algorithm = PAL_HashAlgorithm(rawValue: hashAlgorithm) else {
        return -2
    }

    let derivedKeyFactory : () throws -> ContiguousBytes = {
        switch algorithm {
            case .unknown:
                throw HKDFError.unknownHashAlgorithm
            case .md5:
                return HKDF<Insecure.MD5>.deriveKey(inputKeyMaterial: key, salt: salt, info: info, outputByteCount: destinationLengthInt)
            case .sha1:
                return HKDF<Insecure.SHA1>.deriveKey(inputKeyMaterial: key, salt: salt, info: info, outputByteCount: destinationLengthInt)
            case .sha256:
                return HKDF<SHA256>.deriveKey(inputKeyMaterial: key, salt: salt, info: info, outputByteCount: destinationLengthInt)
            case .sha384:
                return HKDF<SHA384>.deriveKey(inputKeyMaterial: key, salt: salt, info: info, outputByteCount: destinationLengthInt)
            case .sha512:
                return HKDF<SHA512>.deriveKey(inputKeyMaterial: key, salt: salt, info: info, outputByteCount: destinationLengthInt)
        }
    }

    guard let derivedKey = try? derivedKeyFactory() else {
        return -1
    }

    return derivedKey.withUnsafeBytes { keyBytes in
        let destination = UnsafeMutableRawBufferPointer(start: destinationPtr, count: destinationLengthInt)
        return Int32(keyBytes.copyBytes(to: destination))
    }
}
