using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

ManualConfig config = ManualConfig.CreateMinimumViable()
                .AddJob(Job.ShortRun.WithEvaluateOverhead(false));

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
