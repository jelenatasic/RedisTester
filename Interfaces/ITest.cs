using RedisTester.Models;
using StackExchange.Redis;

namespace RedisTester.Interfaces
{
    interface ITest
    {
        TestResults RunTest(int testLoad);

        void RunParallelTest(TestResults testResults);
    }
}
