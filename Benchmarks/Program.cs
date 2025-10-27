using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Runtime.CompilerServices;

namespace FuncOrValBenchmark
{
    // Option 1: Value or Func with null-coalescing
    public readonly struct FuncOrVal<T>
    {
        private readonly T? value;
        private readonly Func<T>? funcVal;
        public FuncOrVal(T value)
        {
            this.value = value;
            this.funcVal = null;
        }
        public FuncOrVal(Func<T> func)
        {
            this.value = default;
            this.funcVal = func;
        }
        public static implicit operator FuncOrVal<T>(T value)
        {
            return new FuncOrVal<T>(value);
        }
        public static implicit operator FuncOrVal<T>(Func<T> func)
        {
            return new FuncOrVal<T>(func);
        }
        public T Value => value ?? funcVal!();
    }

    // Option 2: Func-only
    public readonly struct FuncOrValFuncOnly<T>
    {
        private readonly Func<T> funcVal;
        public FuncOrValFuncOnly(T value)
        {
            this.funcVal = () => value;
        }
        public FuncOrValFuncOnly(Func<T> func)
        {
            this.funcVal = func;
        }
        public static implicit operator FuncOrValFuncOnly<T>(T value)
        {
            return new FuncOrValFuncOnly<T>(value);
        }
        public static implicit operator FuncOrValFuncOnly<T>(Func<T> func)
        {
            return new FuncOrValFuncOnly<T>(func);
        }
        public T Value => funcVal();
    }

    [MemoryDiagnoser] // To measure memory allocations
    [DisassemblyDiagnoser] // To inspect JIT-generated assembly (optional, requires .NET Core)
    public class FuncOrValBenchmarks
    {
        private  FuncOrVal<int> valueOption1;
        private  FuncOrVal<int> funcOption1;
        private  FuncOrValFuncOnly<int> valueOption2;
        private  FuncOrValFuncOnly<int> funcOption2;
        private  int constantValue = 42;
        private  Func<int> constantFunc;

        private readonly Random random = new Random();

        public FuncOrValBenchmarks()
        {
            valueOption1 = new FuncOrVal<int>(constantValue);
            funcOption1 = new FuncOrVal<int>(() => constantValue);
            valueOption2 = new FuncOrValFuncOnly<int>(constantValue);
            funcOption2 = new FuncOrValFuncOnly<int>(() => constantValue);
            constantFunc = () => constantValue;
        }

        [Benchmark(Baseline = true)]
        public int Option1_Value()
        {
            return valueOption1.Value;
        }

        [Benchmark]
        public int Option1_Func()
        {
            return funcOption1.Value;
        }

        [Benchmark]
        public int Option1_Branching()
        {
          var f = random.NextSingle();

            FuncOrVal<int> x;

            if(f>0.5)
            {
                x = random.Next(0, 10);
            }
            else
            {
                x = new Func<int>(() => random.Next(0, 10));
            }

            return x.Value;
        }


        [Benchmark]
        public int Option2_Value()
        {
            return valueOption2.Value;
        }

        [Benchmark]
        public int Option2_Func()
        {
            return funcOption2.Value;
        }

        // Optional: Benchmark a direct delegate invocation for comparison
        [Benchmark]
        public int DirectFunc()
        {
            return constantFunc();
        }

        [Benchmark]
        public int Option2_Branching()
        {
            var f = random.NextSingle();

            FuncOrValFuncOnly<int> x;

            if (f > 0.5)
            {
                x = random.Next(0, 10);
            }
            else
            {
                x = new Func<int>(() => random.Next(0, 10));
            }

            return x.Value;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<FuncOrValBenchmarks>();
        }
    }
}