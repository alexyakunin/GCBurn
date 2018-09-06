using System.Linq;
using Benchmarking.Common;
using Xunit;
using Xunit.Abstractions;

namespace GCBurn.Tests
{
    public class IntervalTests : TestBase
    {
        public IntervalTests(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void CombinedBasicTest()
        {
            var s1 = new [] {(0.0, 0.0), (1, 1)}.ToLongIntervals();
            Assert.Equal(Enumerable.Empty<Interval>(), s1);
            Assert.Equal(s1, s1.ToCanonicalSorted());

            s1 = new [] {(0.0, 1.0), (1, 10)}.ToLongIntervals();
            Assert.Equal(new [] {(0.0, 10.0)}.ToLongIntervals(), s1);
            Assert.Equal(s1, s1.ToCanonicalSorted());

            var s2 = new [] {(0.0, 1.0), (1, 2), (3, 4), (5, 6)}.ToLongIntervals();
            Assert.Equal(3, s2.Count());
            Assert.Equal(s2, s2.ToCanonicalSorted());

            Assert.Equal(s2, s1.IntersectSortedPairs(s2));
        }
    }
}
