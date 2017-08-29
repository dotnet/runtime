// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public enum ConfigurationElementCollectionType
    {
        /**********************************************************************

        This enum type specifies the behavior of the ConfigurationElementCollection.
        Some of the behavior is changeable via other properties (i.e. throwing
        on duplicate entries).

        - BasicMap and BasicMapAlternate

          This collection doesn't do any "clear" or "remove".  Whatever the set
          of entries is in the config file is given back to the user.  An example
          would be like Authentication Users, where each entry specifies a user.

          The Alternate version of this collection simply changes the index location
          of the items from the parent collection.  For example, suppose you had
          entries in machine and app level specified like this:

               machine.config  => A, B, C
               web.config      => D, E, F

          For BasicMap, the collection at the app level would be:

                       A, B, C, D, E, F

          With BasicMapAlternate, it'd be:

                       D, E, F, A, B, C

          That means that the Alternate allows the "nearest" config file entries to
          take precedence over the "parent" config file entries.  

        - AddRemoveClearMap and AddRemoveClearMapAlternate

          This collection honors the "add, remove, clear" commands.  Internally it
          keeps track of each of them so that it knows whether it has to write out
          an add/remove/clear at the appropriate levels, so it uses a concept of
          "virtual index" and "real index" to keep track of stuff.  The "virtual index"
          is what the end user would see and use.  The "real index" is just for us.
          Any access via indexes have to go through some transformation step.

          The Alternate version changes the inheritance stuff like the BasicMapAlternate,
          where the "nearest" config file entries take precedence over the "parent"
          config file entries (see example above).
        **********************************************************************/

        BasicMap,
        AddRemoveClearMap,
        BasicMapAlternate,
        AddRemoveClearMapAlternate,
    }
}
