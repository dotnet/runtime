// Finding: FrozenSet<T> routes small sets (up to 10 items) of any enum type to SmallValueTypeComparableFrozenSet<T>, because
// Constants.IsKnownComparable<T>() includes typeof(T).IsEnum. That class's constructor asserts Debug.Assert(default(T) is
// IComparable<T>), which is false for enums: they implement the non-generic IComparable only. The lookups themselves use
// Comparer<T>.Default and work, so Release builds behave correctly, but Debug/Checked builds of System.Collections.Immutable abort
// the process for something as ordinary as new[] { DayOfWeek.Monday }.ToFrozenSet(). The dictionary counterpart doesn't have this
// assert. The existing tests only freeze enum-keyed dictionaries.
// Run: ./run-on-local-runtime.sh repros/34-FrozenSet-EnumAssert.cs   (the assert needs a Debug/Checked build; dotnet run only
// shows the facts behind it)
using System.Collections.Frozen;

Console.WriteLine($"DayOfWeek is IComparable<DayOfWeek>: {default(DayOfWeek) is IComparable<DayOfWeek>}");
Console.WriteLine($"System.Collections.Immutable is a {(typeof(FrozenSet).Assembly.GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false).Length > 0 && ((System.Diagnostics.DebuggableAttribute)typeof(FrozenSet).Assembly.GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false)[0]).IsJITOptimizerDisabled ? "Debug" : "Release")} build");
FrozenSet<DayOfWeek> set = new[] { DayOfWeek.Monday, DayOfWeek.Friday }.ToFrozenSet(); // Debug builds abort here.
Console.WriteLine($"{set.GetType().Name}: Contains(Friday) = {set.Contains(DayOfWeek.Friday)}, Contains(Sunday) = {set.Contains(DayOfWeek.Sunday)}");
Console.WriteLine(set.GetType().Name.StartsWith("SmallValueTypeComparableFrozenSet") ? "REPRODUCED (the enum set uses the class whose constructor asserts IComparable<T>)" : "NOT REPRODUCED");
