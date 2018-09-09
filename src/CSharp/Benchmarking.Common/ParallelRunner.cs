using System;
using System.Linq;
using System.Threading;

namespace Benchmarking.Common
{
    public interface IActivity
    {
        void Run();
    }
    
    public static class ParallelRunner
    {
        public static int ThreadCount = Environment.ProcessorCount;
        
        public static ParallelRunner<TActivity> New<TActivity>(
            Func<int, TActivity> factory, int? threadCount = null) 
            where TActivity : class, IActivity
            => New(factory, a => a.Run(), threadCount);

        public static ParallelRunner<TActivity> New<TActivity>(
            Func<int, TActivity> factory, Action<TActivity> runner, int? threadCount = null) 
            => new ParallelRunner<TActivity>(factory, runner, threadCount ?? ThreadCount);
    }
    
    public class ParallelRunner<TActivity>
    {
        public TActivity[] Activities;
        public Thread[] Threads;
        
        public ParallelRunner(Func<int, TActivity> factory, Action<TActivity> runner, int threadCount)
        {
            Activities = Enumerable.Range(0, threadCount).Select(factory).ToArray();
            Threads = Activities.Select(a => new Thread(_ => runner.Invoke(a))).ToArray();
        }

        public TActivity[] Run()
        {
            foreach (var thread in Threads)
                thread.Start();
            foreach (var thread in Threads)
                thread.Join();
            return Activities;
        }
    }
}
