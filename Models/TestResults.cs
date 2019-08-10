using Newtonsoft.Json;

namespace RedisTester.Models
{
    public class TestResults
    {
        [JsonProperty("testStatus")]
        public string TestStatus { get; set; }

        [JsonProperty("testLoad")]
        public int TestLoad { get; set; }

        [JsonProperty("stringTest")]
        public TestParams StringTest { get; set; }

        [JsonProperty("listTest")]
        public TestParams ListTest { get; set; }

        [JsonProperty("setTest")]
        public TestParams SetTest { get; set; }

        [JsonProperty("sortedSetTest")]
        public TestParams SortedSetTest { get; set; }

        [JsonProperty("hashTest")]
        public TestParams HashTest { get; set; }

        public TestResults(int testLoad)
        {
            this.TestLoad = testLoad;

            this.StringTest = new TestParams();
            this.ListTest = new TestParams();
            this.SetTest = new TestParams();
            this.SortedSetTest = new TestParams();
            this.HashTest = new TestParams();
        }

    }

    public class TestParams
    {
        [JsonProperty("writeTime")]
        public long WriteTime { get; set; }

        [JsonProperty("readTime")]
        public long ReadTime { get; set; }

        [JsonProperty("updateTime")]
        public long UpdateTime { get; set; }

        [JsonProperty("cleanUpTime")]
        public long CleanUpTime { get; set; }

        [JsonProperty("lostWriteCount")]
        public int LostWriteCount { get; set; }
    }

}
