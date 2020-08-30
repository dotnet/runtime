// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SslStress.Utils
{
    public interface IErrorType
    {
        string ErrorMessage { get; }

        IReadOnlyCollection<(DateTime timestamp, string? metadata)> Occurrences { get; }
    }

    public sealed class ErrorAggregator
    {
        private readonly ConcurrentDictionary<(Type exception, string message, string callSite)[], ErrorType> _failureTypes;

        public ErrorAggregator()
        {
            _failureTypes = new ConcurrentDictionary<(Type, string, string)[], ErrorType>(new StructuralEqualityComparer<(Type, string, string)[]>());
        }

        public int TotalErrorTypes => _failureTypes.Count;
        public IReadOnlyCollection<IErrorType> ErrorTypes => ErrorTypes.ToArray();
        public long TotalErrorCount => _failureTypes.Values.Select(c => (long)c.Occurrences.Count).Sum();

        public void RecordError(Exception exception, string? metadata = null, DateTime? timestamp = null)
        {
            timestamp ??= DateTime.Now;

            (Type, string, string)[] key = ClassifyFailure(exception);

            ErrorType failureType = _failureTypes.GetOrAdd(key, _ => new ErrorType(exception.ToString()));
            failureType.OccurencesQueue.Enqueue((timestamp.Value, metadata));

            // classify exception according to type, message and callsite of itself and any inner exceptions
            static (Type exception, string message, string callSite)[] ClassifyFailure(Exception exn)
            {
                var acc = new List<(Type exception, string message, string callSite)>();

                for (Exception? e = exn; e != null;)
                {
                    acc.Add((e.GetType(), e.Message ?? "", new StackTrace(e, true).GetFrame(0)?.ToString() ?? ""));
                    e = e.InnerException;
                }

                return acc.ToArray();
            }
        }

        public void PrintFailureTypes()
        {
            if (_failureTypes.Count == 0)
                return;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"There were a total of {TotalErrorCount} failures classified into {TotalErrorTypes} different types:");
            Console.WriteLine();
            Console.ResetColor();

            int i = 0;
            foreach (ErrorType failure in _failureTypes.Values.OrderByDescending(x => x.Occurrences.Count))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Failure Type {++i}/{_failureTypes.Count}:");
                Console.ResetColor();
                Console.WriteLine(failure.ErrorMessage);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (IGrouping<string?, (DateTime timestamp, string? metadata)> grouping in failure.Occurrences.GroupBy(o => o.metadata))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"\t{(grouping.Key ?? "").PadRight(30)}");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Fail: ");
                    Console.ResetColor();
                    Console.Write(grouping.Count());
                    Console.WriteLine($"\tTimestamps: {string.Join(", ", grouping.Select(x => x.timestamp.ToString("HH:mm:ss")))}");
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\t    TOTAL".PadRight(31));
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"Fail: ");
                Console.ResetColor();
                Console.WriteLine(TotalErrorTypes);
                Console.WriteLine();
            }
        }

        /// <summary>Aggregate view of a particular stress failure type</summary>
        private sealed class ErrorType : IErrorType
        {
            public string ErrorMessage { get; }
            public ConcurrentQueue<(DateTime, string?)> OccurencesQueue = new ConcurrentQueue<(DateTime, string?)>();

            public ErrorType(string errorText)
            {
                ErrorMessage = errorText;
            }

            public IReadOnlyCollection<(DateTime timestamp, string? metadata)> Occurrences => OccurencesQueue;
        }

        private class StructuralEqualityComparer<T> : IEqualityComparer<T> where T : IStructuralEquatable
        {
            public bool Equals([AllowNull] T left, [AllowNull] T right) => left != null && left.Equals(right, StructuralComparisons.StructuralEqualityComparer);
            public int GetHashCode([DisallowNull] T value) => value.GetHashCode(StructuralComparisons.StructuralEqualityComparer);
        }
    }
}
