// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import CryptoKit
import Foundation

@available(iOS 13, tvOS 13, *)
final class HashBox {
    var value: any HashFunction
    var algorithm : PAL_HashAlgorithm
    init(_ value: any HashFunction, algorithm: PAL_HashAlgorithm) {
        self.value = value
        self.algorithm = algorithm
    }
}

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
    prkPtr: UnsafeMutableRawPointer,
    prkLength: Int32,
    infoPtr: UnsafeMutableRawPointer,
    infoLength: Int32,
    destinationPtr: UnsafeMutablePointer<UInt8>,
    destinationLength: Int32) -> Int32 {

    let prk = Data(bytesNoCopy: prkPtr, count: Int(prkLength), deallocator: Data.Deallocator.none)
    let info = Data(bytesNoCopy: infoPtr, count: Int(infoLength), deallocator: Data.Deallocator.none)
    let destinationLengthInt = Int(destinationLength)

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

@_silgen_name("AppleCryptoNative_DigestOneShot")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestOneShot(
    algorithm: Int32,
    pbData: UnsafeMutableRawPointer?,
    cbData: Int32,
    pbOutput: UnsafeMutablePointer<UInt8>?,
    cbOutput: Int32,
    cbDigest: UnsafeMutablePointer<Int32>?) -> Int32 {

    guard let cbDigest, let pbOutput, let hashAlgorithm = PAL_HashAlgorithm(rawValue: algorithm) else {
        return -1
    }

    let data: Data

    if let ptr = pbData, cbData > 0 {
        data = Data(bytesNoCopy: ptr, count: Int(cbData), deallocator: .none)
    } else {
        data = Data()
    }

    let destination = UnsafeMutableRawBufferPointer(start: pbOutput, count: Int(cbOutput))

    switch hashAlgorithm {
        case .md5:
            _ = Insecure.MD5.hash(data: data).withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            cbDigest.pointee = Int32(Insecure.MD5.byteCount)
            return 1
        case .sha1:
            _ = Insecure.SHA1.hash(data: data).withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            cbDigest.pointee = Int32(Insecure.SHA1.byteCount)
            return 1
        case .sha256:
            _ = SHA256.hash(data: data).withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            cbDigest.pointee = Int32(SHA256.byteCount)
            return 1
        case .sha384:
            _ = SHA384.hash(data: data).withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            cbDigest.pointee = Int32(SHA384.byteCount)
            return 1
        case .sha512:
            _ = SHA512.hash(data: data).withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            cbDigest.pointee = Int32(SHA512.byteCount)
            return 1
        default:
            cbDigest.pointee = 0
            return -1
    }
}

@_silgen_name("AppleCryptoNative_DigestCreate")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestCreate(algorithm: Int32, pcbDigest: UnsafeMutablePointer<Int32>?) -> UnsafeMutableRawPointer? {
    guard let pcbDigest, let hashAlgorithm = PAL_HashAlgorithm(rawValue: algorithm) else {
        return nil
    }

    switch hashAlgorithm {
        case .md5:
            pcbDigest.pointee = Int32(Insecure.MD5.byteCount)
            let box = HashBox(Insecure.MD5(), algorithm: .md5)
            return Unmanaged.passRetained(box).toOpaque()
        case .sha1:
            pcbDigest.pointee = Int32(Insecure.SHA1.byteCount)
            let box = HashBox(Insecure.SHA1(), algorithm: .sha1)
            return Unmanaged.passRetained(box).toOpaque()
        case .sha256:
            pcbDigest.pointee = Int32(SHA256.byteCount)
            let box = HashBox(SHA256(), algorithm: .sha256)
            return Unmanaged.passRetained(box).toOpaque()
        case .sha384:
            pcbDigest.pointee = Int32(SHA384.byteCount)
            let box = HashBox(SHA384(), algorithm: .sha384)
            return Unmanaged.passRetained(box).toOpaque()
        case .sha512:
            pcbDigest.pointee = Int32(SHA512.byteCount)
            let box = HashBox(SHA512(), algorithm: .sha512)
            return Unmanaged.passRetained(box).toOpaque()
        default:
            pcbDigest.pointee = 0
            return nil
    }
}

@_silgen_name("AppleCryptoNative_DigestUpdate")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestUpdate(ctx: UnsafeMutableRawPointer?, pBuf: UnsafeMutableRawPointer?, cBuf: Int32) -> Int32 {
    if cBuf == 0 {
        return 1
    }

    guard let ctx, let pBuf, cBuf >= 0 else {
        return -1
    }

    let box = Unmanaged<HashBox>.fromOpaque(ctx).takeUnretainedValue()
    let source = Data(bytesNoCopy: pBuf, count: Int(cBuf), deallocator: Data.Deallocator.none)

    switch box.algorithm {
        case .md5:
            var hash = box.value as! Insecure.MD5
            hash.update(data: source)
            box.value = hash
            return 1
        case .sha1:
            var hash = box.value as! Insecure.SHA1
            hash.update(data: source)
            box.value = hash
            return 1
        case .sha256:
            var hash = box.value as! SHA256
            hash.update(data: source)
            box.value = hash
            return 1
        case .sha384:
            var hash = box.value as! SHA384
            hash.update(data: source)
            box.value = hash
            return 1
        case .sha512:
            var hash = box.value as! SHA512
            hash.update(data: source)
            box.value = hash
            return 1
        default:
            return -1
    }
}

@_silgen_name("AppleCryptoNative_DigestReset")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestReset(ctx: UnsafeMutableRawPointer?) -> Int32 {
    guard let ctx else {
        return -1
    }

    let box = Unmanaged<HashBox>.fromOpaque(ctx).takeUnretainedValue()

    switch box.algorithm {
        case .md5:
            box.value = Insecure.MD5()
            return 1
        case .sha1:
            box.value = Insecure.SHA1()
            return 1
        case .sha256:
            box.value = SHA256()
            return 1
        case .sha384:
            box.value = SHA384()
            return 1
        case .sha512:
            box.value = SHA512()
            return 1
        default:
            return -2
    }
}

@_silgen_name("AppleCryptoNative_DigestFinal")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestFinal(ctx: UnsafeMutableRawPointer?, pOutput: UnsafeMutablePointer<UInt8>?, cbOutput: Int32) -> Int32 {
    guard let ctx, let pOutput else {
        return -1
    }

    let box = Unmanaged<HashBox>.fromOpaque(ctx).takeUnretainedValue()
    let destination = UnsafeMutableRawBufferPointer(start: pOutput, count: Int(cbOutput))

    switch box.algorithm {
        case .md5:
            let hash = (box.value as! Insecure.MD5).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            break
        case .sha1:
            let hash = (box.value as! Insecure.SHA1).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            break
        case .sha256:
            let hash = (box.value as! SHA256).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            break
        case .sha384:
            let hash = (box.value as! SHA384).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            break
        case .sha512:
            let hash = (box.value as! SHA512).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            break
        default:
            return -1
    }

    return AppleCryptoNative_DigestReset(ctx: ctx)
}

@_silgen_name("AppleCryptoNative_DigestFree")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestFree(ptr: UnsafeMutableRawPointer?) {
    if let ptr {
        Unmanaged<HashBox>.fromOpaque(ptr).release()
    }
}

@_silgen_name("AppleCryptoNative_DigestClone")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestClone(ctx: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
    guard let ctx else {
        return nil
    }

    let box = Unmanaged<HashBox>.fromOpaque(ctx).takeUnretainedValue()

    switch box.algorithm {
        case .md5:
            let hash = box.value as! Insecure.MD5
            let copy = hash
            let cloneBox = HashBox(copy, algorithm: .md5)
            return Unmanaged.passRetained(cloneBox).toOpaque()
        case .sha1:
            let hash = box.value as! Insecure.SHA1
            let copy = hash
            let cloneBox = HashBox(copy, algorithm: .sha1)
            return Unmanaged.passRetained(cloneBox).toOpaque()
        case .sha256:
            let hash = box.value as! SHA256
            let copy = hash
            let cloneBox = HashBox(copy, algorithm: .sha256)
            return Unmanaged.passRetained(cloneBox).toOpaque()
        case .sha384:
            let hash = box.value as! SHA384
            let copy = hash
            let cloneBox = HashBox(copy, algorithm: .sha384)
            return Unmanaged.passRetained(cloneBox).toOpaque()
        case .sha512:
            let hash = box.value as! SHA512
            let copy = hash
            let cloneBox = HashBox(copy, algorithm: .sha512)
            return Unmanaged.passRetained(cloneBox).toOpaque()
        default:
            return nil
    }
}

@_silgen_name("AppleCryptoNative_DigestCurrent")
@available(iOS 13, tvOS 13, *)
public func AppleCryptoNative_DigestCurrent(ctx: UnsafeMutableRawPointer?, pOutput: UnsafeMutablePointer<UInt8>?, cbOutput: Int32) -> Int32 {
    guard let ctx, let pOutput else {
        return -1
    }

    let box = Unmanaged<HashBox>.fromOpaque(ctx).takeUnretainedValue()
    let destination = UnsafeMutableRawBufferPointer(start: pOutput, count: Int(cbOutput))

    switch box.algorithm {
        case .md5:
            let hash = (box.value as! Insecure.MD5).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            return 1
        case .sha1:
            let hash = (box.value as! Insecure.SHA1).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            return 1
        case .sha256:
            let hash = (box.value as! SHA256).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            return 1
        case .sha384:
            let hash = (box.value as! SHA384).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            return 1
        case .sha512:
            let hash = (box.value as! SHA512).finalize()
            _ = hash.withUnsafeBytes { digest in digest.copyBytes(to: destination) }
            return 1
        default:
            return -1
    }
}
