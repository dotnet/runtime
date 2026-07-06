// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    partial class Marshaller
    {
        protected static Marshaller CreateMarshaller(MarshallerKind kind)
        {
            // ReadyToRun only supports emitting IL for blittable types
            switch (kind)
            {
                case MarshallerKind.Enum:
                case MarshallerKind.BlittableValue:
                case MarshallerKind.BlittableStruct:
                case MarshallerKind.UnicodeChar:
                    return new BlittableValueMarshaller();
                case MarshallerKind.VoidReturn:
                    return new VoidReturnMarshaller();
                default:
                    // ensures we don't throw during create marshaller. We will throw NSE
                    // during EmitIL which will be handled.
                    return new NotSupportedMarshaller();
            }
        }

    }
}
