using Newtonsoft.Json;
using System.Collections.Generic;

namespace RedisTester.Models
{
    public class TestResults
    {
        [JsonProperty("testStatus")]
        public string TestStatus { get; set; }

        [JsonProperty("testLoad")]
        public int TestLoadPerThread { get; set; }

        [JsonProperty("testParams")]
        public TestParams TestParams { get; set; }

        [JsonProperty("testDetails")]
        public List<string> TestDetails { get; set; }  

        public TestResults(int testLoad)
        {
            this.TestLoadPerThread = testLoad;
            this.TestDetails = new List<string>();
            this.TestParams = new TestParams();
        }

        public TestResults(string testStatus)
        {
            this.TestStatus = testStatus;
            this.TestDetails = new List<string>();
            this.TestParams = new TestParams();
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

        [JsonProperty("removeTime")]
        public long RemoveTime { get; set; }
    }

}
