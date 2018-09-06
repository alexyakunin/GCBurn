using GCBurn.BurnTest;
using Xunit;

namespace GCBurn.Tests
{
    public class GarbageAllocatorTests
    {
        [Fact]
        public void TestSizeConversion()
        {
            const int MinSize = GarbageAllocator.MinAllocationSize;
            const int ItemSize = GarbageAllocator.ArrayItemSize;
            
            Assert.Equal(0, GarbageAllocator.ByteSizeToArraySize(0));
            Assert.Equal(0, GarbageAllocator.ByteSizeToArraySize(MinSize));
            Assert.Equal(1, GarbageAllocator.ByteSizeToArraySize(MinSize + 1));
            Assert.Equal(1, GarbageAllocator.ByteSizeToArraySize(MinSize + ItemSize - 1));
            Assert.Equal(1, GarbageAllocator.ByteSizeToArraySize(MinSize + ItemSize));
            Assert.Equal(2, GarbageAllocator.ByteSizeToArraySize(MinSize + ItemSize + 1));
            
            Assert.Equal(MinSize, GarbageAllocator.ArraySizeToByteSize(0));
            Assert.Equal(MinSize + ItemSize, GarbageAllocator.ArraySizeToByteSize(1));
            Assert.Equal(MinSize + 2*ItemSize, GarbageAllocator.ArraySizeToByteSize(2));
        }
        
    }
}
