using Xunit.Abstractions;

namespace GCBurn.Tests 
{
    public abstract class TestBase
    {
        public ITestOutputHelper Out { get; }

        protected TestBase(ITestOutputHelper @out) => Out = @out;
    }
}
