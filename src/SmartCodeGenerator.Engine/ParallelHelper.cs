using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCodeGenerator.Engine
{
    internal static class ParallelHelper
    {
        public static async Task ProcessInParallelAsync<T>(this IEnumerable<T> elements, Func<T, Task> function)
        {
            var allJObs = Partitioner.Create(elements).GetPartitions(Environment.ProcessorCount).Select(async partition =>
            {
                while (partition.MoveNext())
                {
                    await function(partition.Current);
                }
            });
            await Task.WhenAll(allJObs);
        }
    }
}