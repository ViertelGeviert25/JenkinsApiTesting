using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsApiWrapper
{
    public class JenkinsJobProperties
    {
        /// <summary>
        /// A crontab expression. Will be optimized internally. If null, no trigger is set.
        /// </summary>
        public string CrontabSpecification { get; set; } = null;

        /// <summary>
        /// Supported: StringParameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
        public int DaysToKeepLogs { get; set; } = 90;
        public int NumToKeepLogs { get; set; } = 30;
        public int ArtifactDaysToKeepLogs { get; set; } = -1;
        public int ArtifactNumToKeepLogs { get; set; } = -1;
    }
}
