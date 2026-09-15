using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes.StructLayout
{
    [StructLayout(LayoutKind.Explicit)]
    [KeptMember(".ctor()")]
    class ExplicitClassData
    {
        [FieldOffset(0)]
        [Kept] // ILLink could remove this
        public int never_used;
        [FieldOffset(4)]
        [Kept]
        public int used;
        [FieldOffset(8)]
        [Kept]
        public int never_ever_used;
    }

    [StructLayout(LayoutKind.Explicit)]
    [Kept]
    class UnallocatedExplicitClassData
    {
        [FieldOffset(0)]
        public int never_used;
    }

    [StructLayout(LayoutKind.Explicit)]
    [Kept]
    class UnallocatedButReferencedWithReflectionExplicitClassData
    {
        [Kept]
        [FieldOffset(0)]
        public int never_used;
    }

    [Kept]
    [StructLayout(LayoutKind.Sequential)]
    class ExplicitLayoutGrandBase
    {
        [Kept]
        public int never_used_grandbase;
    }

    [Kept]
    [KeptBaseType(typeof(ExplicitLayoutGrandBase))]
    class ExplicitAutoLayoutIntermediate : ExplicitLayoutGrandBase
    {
        public int removed_auto_field;
    }

    [Kept]
    [StructLayout(LayoutKind.Explicit)]
    [KeptBaseType(typeof(ExplicitAutoLayoutIntermediate))]
    class ExplicitLayoutThroughAutoIntermediate : ExplicitAutoLayoutIntermediate
    {
        [FieldOffset(0)]
        [Kept]
        public int used;
    }

    [RemovedMemberInAssembly("test.exe", typeof(ExplicitAutoLayoutIntermediate), "removed_auto_field")]
    public class ExplicitClass
    {
        [Kept]
        static UnallocatedExplicitClassData _myField;

        [Kept]
        static ExplicitLayoutThroughAutoIntermediate _layoutThroughAutoIntermediate;

        public static void Main()
        {
            var c = new ExplicitClassData();
            c.used = 1;

            _myField = null;

            typeof(UnallocatedButReferencedWithReflectionExplicitClassData).ToString();

            if (string.Empty.Length > 0)
            {
                _layoutThroughAutoIntermediate.used = 123;
            }
        }
    }
}
