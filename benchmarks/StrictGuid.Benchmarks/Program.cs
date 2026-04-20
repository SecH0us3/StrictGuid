using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using StrictGuid.Library;
using System;

namespace StrictGuid.Benchmarks
{
    public enum BenchmarkEntity : byte
    {
        User = 1
    }

    [MemoryDiagnoser]
    public class GuidBenchmarks
    {
        [Benchmark(Baseline = true)]
        public Guid StandardNewGuid() => Guid.NewGuid();

        [Benchmark]
        public Guid StrictGuidFast() => BenchmarkEntity.User.NewStrictGuid(StrictGuidEntropy.Fast);

        [Benchmark]
        public Guid StrictGuidSecure() => BenchmarkEntity.User.NewStrictGuid(StrictGuidEntropy.Secure);

        [Benchmark]
        public Guid StrictGuidSequential() => BenchmarkEntity.User.NewStrictGuid(StrictGuidEntropy.Sequential);

        [Benchmark]
        public BenchmarkEntity ExtractType()
        {
            var id = BenchmarkEntity.User.NewStrictGuid();
            return id.GetEntityType<BenchmarkEntity>();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<GuidBenchmarks>();
        }
    }
}
