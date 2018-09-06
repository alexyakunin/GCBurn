using System.Collections.Generic;
using System.Linq;

namespace Benchmarking.Common
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<(T, int)> WithIndexes<T>(this IEnumerable<T> source) =>
            source.Select((item, index) => (item, index));
    }
}
