using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarking.Common
{
    public readonly struct Interval : IEquatable<Interval>
    {
        public static readonly Interval Empty = default;
        public static readonly Interval Unit = new Interval(0, 1);
        public static readonly Interval All = new Interval(double.MinValue, double.MaxValue);

        public double Start { get; } // Inclusive
        public double End { get; }  // Exclusive
        public double Size => End - Start;
        public bool IsEmpty => Size == 0;

        public Interval(double start, double end)
        {
            if (end <= start)
                Start = End = 0;
            else {
                Start = start;
                End = end;
            }
        }

        public override string ToString() => $"[{Start} .. {End})";

        // Equality
        public override bool Equals(object obj) => 
            !ReferenceEquals(null, obj) && (obj is Interval interval && Equals(interval));
        public bool Equals(Interval other) => Start == other.Start && End == other.End;
        public override int GetHashCode() => unchecked ((Start.GetHashCode() * 397) ^ End.GetHashCode());
        public static bool operator ==(Interval left, Interval right) => left.Equals(right);
        public static bool operator !=(Interval left, Interval right) => !left.Equals(right);

        // MoveBy / expand
        public Interval MoveBy(double offset) => new Interval(Start + offset, End + offset);
        public Interval ExpandBy(double expansion) => new Interval(Start, End + expansion);
        public Interval ScaleBy(double factor) => new Interval(Start * factor, End * factor);

        // Comparison
        public bool Contains(double x) => Start <= x && x < End;
        public bool Contains(Interval other) => Contains(other.Start) && Contains(other.End - 1) && !other.IsEmpty;
        public bool OverlapsWith(Interval other) => CompareForOverlap(other) == 0;
        public bool UnionsWith(Interval other) => CompareForUnion(other) == 0;
        public static bool AnyIsEmpty(Interval first, Interval second) => first.IsEmpty || second.IsEmpty; 

        // -1 = this goes before other
        // 0 = intervals overlap
        // 1 = this goes after other
        public int CompareForOverlap(Interval other) => End <= other.Start ? -1 : (other.End <= Start ? 1 : 0);
        // 0 = intervals can be united
        public int CompareForUnion(Interval other) => 
            End < other.Start 
                ? (AnyIsEmpty(this, other) ? 0 : -1) 
                : (other.End < Start ? (AnyIsEmpty(this, other) ? 0 : 1) : 0);

        // Union & intersect
        public Interval Intersect(Interval other) => 
            new Interval(Math.Max(Start, other.Start), Math.Min(End, other.End));

        public Interval UnionExtend(Interval other) =>
            new Interval(Math.Min(Start, other.Start), Math.Max(End, other.End));

        public Interval Union(Interval other) =>
            TryUnion(other, out var merged)
                ? merged
                : throw new ArgumentException("Can't merge non-overlapping intervals", nameof(other));
        
        public bool TryUnion(Interval other, out Interval union)
        {
            union = default(Interval);
            if (!UnionsWith(other))
                return false;
            union = UnionExtend(other);
            return true;
        }
    }

    public static class LongIntervalHelpers
    {
        public static IEnumerable<Interval> ToLongIntervals(this IEnumerable<(double, double)> source) =>
            source.Select(p => new Interval(p.Item1, p.Item2)).ToCanonicalSorted();

        public static IEnumerable<Interval> ToCanonicalSorted(this IEnumerable<Interval> source)
        {
            var last = Interval.Empty;
            foreach (var current in source) {
                if (last.Start > current.Start && !last.IsEmpty && !current.IsEmpty)
                    throw new ArgumentOutOfRangeException(nameof(source), "The sequence is expected to be sorted.");
                if (last.UnionsWith(current))
                    last = last.UnionExtend(current);
                else if (!last.IsEmpty) {
                    yield return last;
                    last = current;
                }
            }
            if (!last.IsEmpty)
                yield return last;
        }

        public static IEnumerable<Interval> IntersectSortedPairs(this IEnumerable<Interval> first, IEnumerable<Interval> second)
        {
            using (var e1 = first.ToCanonicalSorted().GetEnumerator())
            using (var e2 = second.ToCanonicalSorted().GetEnumerator()) {
                if (!e1.MoveNext())
                    yield break;
                if (!e2.MoveNext())
                    yield break;
                while (true) {
                    var intersection = e1.Current.Intersect(e2.Current);
                    if (!intersection.IsEmpty)
                        yield return intersection;
                    var moveTarget = e1.Current.End < e2.Current.End ? e1 : e2;
                    if (!moveTarget.MoveNext())
                        yield break;
                }
            }
        }

        public static IEnumerable<Interval> IntersectSortedTuples(this IEnumerable<IEnumerable<Interval>> all)
        {
            var list = all.ToList();
            switch (list.Count) {
            case 0:
                return Enumerable.Empty<Interval>();
            case 1:
                return list[0];
            default:
                var even = list.Where((s, i) => (i % 2) == 0).IntersectSortedTuples();
                var odd = list.Where((s, i) => (i % 2) != 0).IntersectSortedTuples();
                return even.IntersectSortedPairs(odd);
            }
        }
    }
}
