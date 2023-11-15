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
