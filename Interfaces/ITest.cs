using RedisTester.Models;
using StackExchange.Redis;

namespace RedisTester.Interfaces
{
    public interface ITest
    {
        TestResults RunTest(int testLoad);

        void RunParallelTest(TestResults testResults);
    }
}
