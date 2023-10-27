public class MathLibrary {
    public var a: Double
    public var b: Double

    public static let shared = MathLibrary(a: 40.0, b: 2.0)

    private init(a: Double, b: Double) {
        self.a = a
        self.b = b
    }

    public func getMagicNumber(dummy: UnsafeRawPointer) {
        print(a + b)
    }


    public static func getInstance() -> UnsafeMutableRawPointer {
        let unmanagedInstance = Unmanaged.passUnretained(shared)
        let pointer = unmanagedInstance.toOpaque()
        return pointer
    }
}