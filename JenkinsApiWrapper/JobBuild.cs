using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsApiWrapper
{
    public enum BuildStatus
    {
        /// <summary>
        /// The Build Status is undefined (default value).
        /// </summary>
        UNDEFINED = 0,

        /// <summary>
        /// The Build was interrupted before it reaches its expected end. For example, the user has stopped it manually or there was a time-out.
        /// </summary>
        ABORTED,

        /// <summary>
        /// The Build had a fatal error.
        /// </summary>
        FAILED,

        /// <summary>
        /// The Build was Successful and no Publisher reports it as Unstable.
        /// </summary>
        STABLE,

        /// <summary>
        /// The Build has no compilation errors.
        /// </summary>
        SUCCESSFUL,

        /// <summary>
        /// The Build had some errors but they were not fatal. A Build is unstable if it was built successfully and one or more publishers report it unstable. For example if the JUnit publisher is configured and a test fails then the Build will be marked unstable.
        /// </summary>
        UNSTABLE
    }

    /// <summary>
    /// This class provides data about a Jenkins job build.
    /// </summary>
    public class JobBuild
    {
        /// <summary>
        /// Returns the build status.
        /// </summary>
        public BuildStatus Status { get; set; }

        /// <summary>
        /// Returns the Console Text.
        /// </summary>
        public string ConsoleText { get; set; }

        public JobBuild()
        {

        }

        public JobBuild(BuildStatus status, string consoleText)
        {
            Status = status;
            ConsoleText = consoleText;
        }

        public JobBuild(BuildStatus status)
        {
            Status = status;
        }
    }
}
