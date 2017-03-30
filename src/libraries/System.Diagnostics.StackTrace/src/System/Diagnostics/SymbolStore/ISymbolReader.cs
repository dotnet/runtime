// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolReader
    {
        // Find a document. Language, vendor, and document type are
        // optional.
        ISymbolDocument GetDocument(string url,
                                    Guid language,
                                    Guid languageVendor,
                                    Guid documentType);
    
        // Return an array of all of the documents defined in the symbol
        // store.
        ISymbolDocument[] GetDocuments();
    
        // Return the method that was specified as the user entry point
        // for the module, if any. This would be, perhaps, the user's main
        // method rather than compiler generated stubs before main.
        SymbolToken UserEntryPoint { get; }
    
        // Get a symbol reader method given the id of a method.
        ISymbolMethod GetMethod(SymbolToken method);
    
        // Get a symbol reader method given the id of a method and an E&C
        // version number. Version numbers start a 1 and are incremented
        // each time the method is changed due to an E&C operation.
        ISymbolMethod GetMethod(SymbolToken method, int version);
    
        // Return a non-local variable given its parent and name.
        ISymbolVariable[] GetVariables(SymbolToken parent);
    
        // Return a non-local variable given its parent and name.
        ISymbolVariable[] GetGlobalVariables();
    
        // Given a position in a document, return the ISymbolMethod that
        // contains that position.
        ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document,
                                                    int line,
                                                    int column);
        
        // Gets a custom attribute based upon its name. Not to be
        // confused with Metadata custom attributes, these attributes are
        // held in the symbol store.
        byte[] GetSymAttribute(SymbolToken parent, string name);
    
        // Get the namespaces defined at global scope within this symbol store.
        ISymbolNamespace[] GetNamespaces();
    }
}
