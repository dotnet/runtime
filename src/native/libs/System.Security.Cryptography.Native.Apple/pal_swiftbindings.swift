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

@_silgen_name("AppleCryptoNative_HKDFExpand")
@available(iOS 14, tvOS 14, *)
public func AppleCryptoNative_HKDFExpand(
    hashAlgorithm: Int32,
    prk: UnsafeBufferPointer<UInt8>,
    info: UnsafeBufferPointer<UInt8>,
    destination: UnsafeMutableBufferPointer<UInt8>) throws {

    if let algorithm = PAL_HashAlgorithm(rawValue: hashAlgorithm) {
        let key = try {
            switch algorithm {
                case .unknown:
                    throw HKDFError.unknownHashAlgorithm
                case .md5:
                    HKDF<Insecure.MD5>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destination.count)
                case .sha1:
                    HKDF<Insecure.SHA1>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destination.count)
                case .sha256:
                    HKDF<SHA256>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destination.count)
                case .sha384:
                    HKDF<SHA384>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destination.count)
                case .sha512:
                    HKDF<SHA512>.expand(pseudoRandomKey: prk, info: info, outputByteCount: destination.count)
            }
        }()

        _ = key.withUnsafeBytes { keyBytes in
            keyBytes.copyBytes(to: destination)
        }
    }
    else {
        throw HKDFError.unknownHashAlgorithm
    }
}

@_silgen_name("AppleCryptoNative_HKDFExtract")
@available(iOS 14, tvOS 14, *)
public func AppleCryptoNative_HKDFExtract(
    hashAlgorithm: Int32,
    ikm: UnsafeBufferPointer<UInt8>,
    salt: UnsafeBufferPointer<UInt8>,
    destination: UnsafeMutableBufferPointer<UInt8>) throws {

    let key = SymmetricKey(data: ikm)

    if let algorithm = PAL_HashAlgorithm(rawValue: hashAlgorithm) {
        let prk : ContiguousBytes = try {
            switch algorithm {
                case .unknown:
                    throw HKDFError.unknownHashAlgorithm
                case .md5:
                    HKDF<Insecure.MD5>.extract(inputKeyMaterial: key, salt: salt)
                case .sha1:
                    HKDF<Insecure.SHA1>.extract(inputKeyMaterial: key, salt: salt)
                case .sha256:
                    HKDF<SHA256>.extract(inputKeyMaterial: key, salt: salt)
                case .sha384:
                    HKDF<SHA384>.extract(inputKeyMaterial: key, salt: salt)
                case .sha512:
                    HKDF<SHA512>.extract(inputKeyMaterial: key, salt: salt)
            }
        }()

        _ = prk.withUnsafeBytes { keyBytes in
            keyBytes.copyBytes(to: destination)
        }
    }
    else {
        throw HKDFError.unknownHashAlgorithm
    }
}

@_silgen_name("AppleCryptoNative_HKDFDeriveKey")
@available(iOS 14, tvOS 14, *)
public func AppleCryptoNative_HKDFDeriveKey(
    hashAlgorithm: Int32,
    ikm: UnsafeBufferPointer<UInt8>,
    salt: UnsafeBufferPointer<UInt8>,
    destination: UnsafeMutableBufferPointer<UInt8>) throws {

    let key = SymmetricKey(data: ikm)

    if let algorithm = PAL_HashAlgorithm(rawValue: hashAlgorithm) {
        let prk : ContiguousBytes = try {
            switch algorithm {
                case .unknown:
                    throw HKDFError.unknownHashAlgorithm
                case .md5:
                    HKDF<Insecure.MD5>.extract(inputKeyMaterial: key, salt: salt)
                case .sha1:
                    HKDF<Insecure.SHA1>.extract(inputKeyMaterial: key, salt: salt)
                case .sha256:
                    HKDF<SHA256>.extract(inputKeyMaterial: key, salt: salt)
                case .sha384:
                    HKDF<SHA384>.extract(inputKeyMaterial: key, salt: salt)
                case .sha512:
                    HKDF<SHA512>.extract(inputKeyMaterial: key, salt: salt)
            }
        }()

        _ = prk.withUnsafeBytes { keyBytes in
            keyBytes.copyBytes(to: destination)
        }
    }
    else {
        throw HKDFError.unknownHashAlgorithm
    }
}
