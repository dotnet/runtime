// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern System.Runtime.InteropServices { }
.assembly extern TypeMapLib1 { }

.assembly TypeMapBlobOnlyLib
{
    // Keys and mapped types are encoded directly in the custom attribute blob as SerString values.
    // This intentionally avoids any IL usage of C1/S1 so there is no requirement for TypeRef rows
    // for the mapped types outside of custom attribute blobs.
    .custom instance void class [System.Runtime.InteropServices]System.Runtime.InteropServices.TypeMapAttribute`1<class [TypeMapLib1]BlobOnlyAttributeTypeNames>::.ctor(string, class [System.Runtime]System.Type)
           = {string('blob_only_c1')
              type(class 'C1, TypeMapLib1')}

    .custom instance void class [System.Runtime.InteropServices]System.Runtime.InteropServices.TypeMapAttribute`1<class [TypeMapLib1]BlobOnlyAttributeTypeNames>::.ctor(string, class [System.Runtime]System.Type)
           = {string('blob_only_s1')
              type(class 'S1, TypeMapLib1')}

    .custom instance void class [System.Runtime.InteropServices]System.Runtime.InteropServices.TypeMapAssociationAttribute`1<class [TypeMapLib1]BlobOnlyAttributeTypeNames>::.ctor(class [System.Runtime]System.Type, class [System.Runtime]System.Type)
           = {type(class 'C1, TypeMapLib1')
              type(class 'S1, TypeMapLib1')}

    .custom instance void class [System.Runtime.InteropServices]System.Runtime.InteropServices.TypeMapAssociationAttribute`1<class [TypeMapLib1]BlobOnlyAttributeTypeNames>::.ctor(class [System.Runtime]System.Type, class [System.Runtime]System.Type)
           = {type(class 'S1, TypeMapLib1')
              type(class 'C1, TypeMapLib1')}

    .custom instance void class [System.Runtime.InteropServices]System.Runtime.InteropServices.TypeMapAttribute`1<class [TypeMapLib1]BlobOnlyAttributeTypeNames>::.ctor(string, class [System.Runtime]System.Type)
           = {string('lib5_type1')
              type(class 'Lib5Type1, TypeMapLib5')}

    .custom instance void class [System.Runtime.InteropServices]System.Runtime.InteropServices.TypeMapAssociationAttribute`1<class [TypeMapLib1]BlobOnlyAttributeTypeNames>::.ctor(class [System.Runtime]System.Type, class [System.Runtime]System.Type)
           = {type(class 'Lib5Type1, TypeMapLib5')
              type(class 'Lib5Proxy1, TypeMapLib5')}

    .ver 0:0:0:0
}
