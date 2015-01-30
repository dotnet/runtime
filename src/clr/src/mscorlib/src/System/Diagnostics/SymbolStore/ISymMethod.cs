// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Represents a method within a symbol reader. This provides access to
** only the symbol-related attributes of a method, such as sequence
** points, lexical scopes, and parameter information. Use it in
** conjucntion with other means to read the type-related attrbiutes of
** a method, such as Reflections.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    using System.Runtime.InteropServices;
    using System;
    
    // Interface does not need to be marked with the serializable attribute
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolMethod
    {
        // Get the token for this method.
        SymbolToken Token { get; }
    
        // Get the count of sequence points.
        int SequencePointCount { get; }
        
        // Get the sequence points for this method. The sequence points
        // are sorted by offset and are for all documents in the
        // method. Use GetSequencePointCount to retrieve the count of all
        // sequence points and create arrays of the proper size.
        // GetSequencePoints will verify the size of each array and place
        // the sequence point information into each. If any array is NULL,
        // then the data for that array is simply not returned.
        void GetSequencePoints(int[] offsets,
                               ISymbolDocument[] documents,
                               int[] lines,
                               int[] columns,
                               int[] endLines,
                               int[] endColumns);
    
        // Get the root lexical scope for this method. This scope encloses
        // the entire method.
        ISymbolScope RootScope { get; } 
    
        // Given an offset within the method, returns the most enclosing
        // lexical scope. This can be used to start local variable
        // searches.
        ISymbolScope GetScope(int offset);
    
        // Given a position in a document, return the offset within the
        // method that corresponds to the position.
        int GetOffset(ISymbolDocument document,
                             int line,
                             int column);
    
        // Given a position in a document, return an array of start/end
        // offset paris that correspond to the ranges of IL that the
        // position covers within this method. The array is an array of
        // integers and is [start,end,start,end]. The number of range
        // pairs is the length of the array / 2.
        int[] GetRanges(ISymbolDocument document,
                               int line,
                               int column);
    
        // Get the parameters for this method. The paraemeters are
        // returned in the order they are defined within the method's
        // signature.
        ISymbolVariable[] GetParameters();
    
        // Get the namespace that this method is defined within.
        ISymbolNamespace GetNamespace();
    
        // Get the start/end document positions for the source of this
        // method. The first array position is the start while the second
        // is the end. Returns true if positions were defined, false
        // otherwise.
        bool GetSourceStartEnd(ISymbolDocument[] docs,
                                         int[] lines,
                                         int[] columns);
    }

}
