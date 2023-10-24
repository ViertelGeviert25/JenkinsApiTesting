using System;

namespace JenkinsApiWrapper
{
    /// <summary>
    /// Class for all JenkinsApiWrapper exceptions.
    /// </summary>
    public class JenkinsNetException : Exception
    {
        internal JenkinsNetException(string message) : base(message) { }
        internal JenkinsNetException(string message, Exception innerException) : base(message, innerException) { }
    }
}