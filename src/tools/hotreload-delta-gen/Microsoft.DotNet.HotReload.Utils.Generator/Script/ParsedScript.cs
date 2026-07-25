using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Script;

/// A parsed script.
///
/// Capabilities: null if the script didn't have any capabilities, or they were all unknowns
/// Changes: the sequence of changes
/// UnknownCapabilities: any capabilities we couldn't decode to an enum value
public record ParsedScript (EnC.EditAndContinueCapabilities? Capabilities, IEnumerable<Plan.Change<string,string>> Changes, IEnumerable<string> UnknownCapabilities) {

    public static ParsedScript Empty => new (null, Array.Empty<Plan.Change<string,string>>(), Array.Empty<string>());

    public static ParsedScript Make(IEnumerable<Plan.Change<string,string>> changes, EnC.EditAndContinueCapabilities? capabilities, IEnumerable<string> unknownCapabilities) => new (capabilities, changes, unknownCapabilities);

}
