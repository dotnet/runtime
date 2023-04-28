file sealed unsafe class ComClassInformation : System.Runtime.InteropServices.Marshalling.IComExposedClass
{
    private static volatile System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry* s_vtables;
    public static System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count)
    {
        count = 1;
        if (s_vtables == null)
        {
            System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry* vtables = (System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry*)System.Runtime.CompilerServices.RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComClassInformation), sizeof(System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry) * 1);
            System.Runtime.InteropServices.Marshalling.IIUnknownDerivedDetails details;
            details = System.Runtime.InteropServices.Marshalling.StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(SharedTypes.ComInterfaces.IGetAndSetInt).TypeHandle);
            vtables[0] = new()
            {
                IID = details.Iid,
                Vtable = (nint)details.ManagedVirtualMethodTable
            };
            s_vtables = vtables;
        }

        return s_vtables;
    }
}

namespace ComInterfaceGenerator.Tests
{
    [System.Runtime.InteropServices.Marshalling.ComExposedClassAttribute<ComClassInformation>]
    partial class ManagedObjectExposedToCom
    {
    }
}
