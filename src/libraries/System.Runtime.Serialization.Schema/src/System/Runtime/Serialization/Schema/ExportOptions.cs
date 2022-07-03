// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Runtime.Serialization.Schema
{
    // TODO smolloy - should we rename this to avoid confusion with Sys.RT.Ser.ExportOptions? Rename ImportOptions to match.
    // Do we even need this class? It is not a different signature than the in-box one. Should we just use that?
    // Actually, that's not quite right. This _would be_ the same signature if export supported surrogates in box.
    // But it doesn't, so the surrogate provider is not there. We could add it. Or just use this class. Depends on which
    // is the more likely "pit of success" I guess.
    public class ExportOptions
    {
        // TODO smolloy - this is named differently than the in-box version of the options.
        public ISerializationSurrogateProvider? SurrogateProvider { get; set; }

        // TODO smolloy - This was used in core already. Since the idea is to shift this export functionality out here, this
        // existing piece should come along with us. Which is to say, we probably still need this.
        private Collection<Type>? _knownTypes;
        public Collection<Type> KnownTypes => _knownTypes ??= new Collection<Type>();
    }
}
