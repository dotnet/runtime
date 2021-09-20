
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    sealed class GeneratedMarshallingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class BlittableTypeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class NativeMarshallingAttribute : Attribute
    {
        public NativeMarshallingAttribute(Type nativeType)
        {
            NativeType = nativeType;
        }

        public Type NativeType { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class MarshalUsingAttribute : Attribute
    {
        public MarshalUsingAttribute()
        {
            CountElementName = string.Empty;
        }

        public MarshalUsingAttribute(Type nativeType)
            :this()
        {
            NativeType = nativeType;
        }

        public Type? NativeType { get; }

        public string CountElementName { get; set; }

        public int ConstantElementCount { get; set; }

        public int ElementIndirectionLevel { get; set; }

        public const string ReturnsCountValue = "return-value";
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GenericContiguousCollectionMarshallerAttribute : Attribute
    {
        public GenericContiguousCollectionMarshallerAttribute()
        {
        }
    }
}