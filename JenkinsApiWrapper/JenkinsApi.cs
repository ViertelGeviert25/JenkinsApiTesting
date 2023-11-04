using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;

namespace JenkinsApiWrapper
{
    /// <summary>
    /// This class provides wrapper functions of the Jenkins remote access API.
    /// </summary>
    public class JenkinsApi
    {
        #region Properties
        /// <summary>
        /// The address of the Jenkins instance.
        /// ie: http://localhost:8080
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Jenkins Username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Jenkins ApiToken for the <see cref="Username"/>.
        /// </summary>
        public string ApiToken { get; set; }

        private string Credentials
        {
            get
            {
                if (string.IsNullOrEmpty(Username))
                    throw new ArgumentNullException("Invalid username!", nameof(Username));
                if (string.IsNullOrEmpty(ApiToken))
                    throw new ArgumentException("Invalid API token!", nameof(ApiToken));
                return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{ApiToken}"));
            }
        }
        #endregion

        /// <summary>
        /// Constructor for the JenkinsApi class.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="username">Username</param>
        /// <param name="apiToken">API token</param>
        public JenkinsApi(string baseUrl, string username, string apiToken)
        {
            BaseUrl = baseUrl;
            Username = username;
            ApiToken = apiToken;
        }

        /// <summary>
        /// Checks if folder exists and sets container folder path for the subsequent requests, otherwise will throw an exception.
        /// </summary>
        /// <param name="fullProjectName"></param>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private async Task<Tuple<string, string>> CheckProject(string fullProjectName)
        {
            if (string.IsNullOrEmpty(fullProjectName)) // check sanity
                throw new ArgumentException($"Project name cannot be null or empty.", nameof(fullProjectName));

            fullProjectName = HttpUtility.UrlPathEncode(fullProjectName);

            var items = fullProjectName.Split('/').Where(path => !string.IsNullOrEmpty(path));
            var folderPath = items
                .Take(items.Count() - 1)
                .Select(path => "job/" + path.Trim('/'))
                .Aggregate((a, b) => string.Concat(a, '/', b));

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/api/xml";
            var response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
                throw new JenkinsNetException($"Folder '{folderPath}' does not exist!");

            return new Tuple<string, string>(folderPath, items.Last());
        }

        /// <summary>
        /// Returns true if Jenkins project exists.
        /// </summary>
        /// <param name="fullProjectName"></param>
        /// <returns>True if job exists.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<bool> DoesJobExist(string fullProjectName)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);
            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/api/xml";
            var response = await client.GetAsync(apiUrl);

            return response.IsSuccessStatusCode;
        }

        private async Task CreateJob(string fullProjectName, Func<XDocument> setConfig)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/createItem?name={jobName}";

            var jobTemplateConfig = setConfig.Invoke();

            using var content = GenerateStringContent(jobTemplateConfig);
            var response = await client.PostAsync(apiUrl, content);
            EvaluateResponseMessage(response, "Creating Jenkins Job encountered a problem: ");
        }

        /// <summary>
        /// Creates a new pipeline project.
        /// </summary>
        /// <param name="fullProjectName">The full project name.</param>
        /// <param name="pipelineScript">The pipeline script.</param>
        /// <param name="cronExpression">A crontab expression. Will be optimizied internally. If null, no trigger is set.</param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task CreatePipelineProject(string fullProjectName, string pipelineScript, string cronExpression = null)
        {
            await CreateJob(fullProjectName, () =>
            {
                var jobTemplateConfig = GetJobTemplateConfig(PipelineDefinition.PipelineScript);
                SetCrontabSpec(jobTemplateConfig, cronExpression);
                SetPipelineScript(jobTemplateConfig, pipelineScript);
                return jobTemplateConfig;
            });
        }

        /// <summary>
        /// Creates a new pipeline project from SCM.
        /// </summary>
        /// <param name="fullProjectName">The full project name. Ensure job folders already exist, otherwise an exception will be thrown.</param>
        /// <param name="repositoryUrl">Repository URL</param>
        /// <param name="branchSpec">Branch specification, e.g. */master</param>
        /// <param name="scriptPath">The script path, e.g. Jenkinsfile</param>
        /// <param name="cronExpression">A crontab expression. Will be optimized internally. If null, no trigger is set.</param>
        /// <returns></returns>
        public async Task CreatePipelineProjectFromScm(string fullProjectName, string repositoryUrl, string branchSpec = "*/master", string scriptPath = "Jenkinsfile", string cronExpression = null)
        {
            await CreateJob(fullProjectName, () =>
            {
                var jobTemplateConfig = GetJobTemplateConfig(PipelineDefinition.PipelineScriptFromScm);
                SetCrontabSpec(jobTemplateConfig, cronExpression);
                SetPipelineScriptFromScm(jobTemplateConfig, repositoryUrl, branchSpec, scriptPath);
                return jobTemplateConfig;
            });
        }

        public async Task<string> GetJobDescription(string fullProjectName)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/description";
            var jobDescription = await client.GetStringAsync(apiUrl);
            return jobDescription;
        }

        public async Task UpdateJobDescription(string fullProjectName, string description)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/description";
            var formData = new KeyValuePair<string, string>("description", description);
            var formContent = new FormUrlEncodedContent(new[] { formData });
            var response = await client.PostAsync(apiUrl, formContent);
            EvaluateResponseMessage(response, $"Updating job description encountered a problem: ");
        }

        /// <summary>
        /// Returns the 50 newest builds. This prevents Jenkins from having to load all builds from disk.
        /// </summary>
        /// <param name="fullProjectName"></param>
        /// <returns></returns>
        public async Task<XDocument> RetrieveBuilds(string fullProjectName)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/api/xml/builds";
            var payload = await client.GetStringAsync(apiUrl);
            payload = RemoveXmlDeclaration(payload);
            var builds = XDocument.Parse(payload);
            return builds;
        }

        /// <summary>
        /// Triggers a jenkins project.
        /// </summary>
        /// <param name="fullProjectName">The full project name.</param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task TriggerJob(string fullProjectName)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/build?delay=0sec";
            var response = await client.PostAsync(apiUrl, null);
            EvaluateResponseMessage(response, $"Triggering job '{fullProjectName}' encountered a problem: ");
        }

        /// <summary>
        /// Updates the timer trigger specification of the Jenkins project configuration.
        /// </summary>
        /// <param name="fullProjectName">The full project name.</param>
        /// <param name="cronExpression">crontab expression</param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task SetCrontabSpecification(string fullProjectName, string cronExpression)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/config.xml";
            var currentConfig = await client.GetStringAsync(apiUrl);
            currentConfig = RemoveXmlDeclaration(currentConfig);
            var currentXConfig = XDocument.Parse(currentConfig);
            SetCrontabSpec(currentXConfig, cronExpression);

            using var content = GenerateStringContent(currentXConfig);
            var response = await client.PostAsync(apiUrl, content);
            EvaluateResponseMessage(response, $"Updating job configuration encountered a problem: ");
        }

        /// <summary>
        /// Removes the timer trigger specification from the job configuration.
        /// </summary>
        /// <param name="fullProjectName">The full project name.</param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        public async Task RemoveCrontabSpecification(string fullProjectName)
        {
            await SetCrontabSpecification(fullProjectName, null);
        }

        /// <summary>
        /// Stops current build.
        /// </summary>
        /// <param name="fullProjectName">The full project name.</param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task CancelCurrentBuild(string fullProjectName)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/lastBuild/api/xml";
            var response = await client.GetAsync(apiUrl);
            var lastBuildDetails = await response.Content.ReadAsStringAsync();
            var currentXConfig = XDocument.Parse(lastBuildDetails);
            var lastBuiltNumber = int.Parse(currentXConfig.Document.XPathSelectElement("workflowRun/number").Value);

            var stopUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/{lastBuiltNumber}/stop";
            response = await client.PostAsync(stopUrl, null);
            EvaluateResponseMessage(response, "Stopping job encountered a problem: ");
        }

        /// <summary>
        /// Returns last build status, and console output.
        /// </summary>
        /// <param name="fullProjectName"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        public async Task<JobBuild> GetLastBuildStatus(string fullProjectName, TimeSpan timeout)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var stopWatch = Stopwatch.StartNew();
            var inProgress = true;
            XDocument xDocument = null;
            do
            {
                var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}/lastBuild/api/xml";
                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return new JobBuild(BuildStatus.UNDEFINED);
                }
                var lastBuildDetails = await response.Content.ReadAsStringAsync();
                xDocument = XDocument.Parse(lastBuildDetails);
                inProgress = bool.Parse(xDocument.Document.XPathSelectElement("workflowRun/inProgress").Value);
                if (inProgress)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            } while (inProgress && stopWatch.Elapsed < timeout);

            if (stopWatch.Elapsed > timeout)
            {
                throw new JenkinsNetException("Reading last build status encountered a problem: Timeout occured!");
            }

            var result = xDocument.Document.XPathSelectElement("workflowRun/result").Value;
            var lastBuildStatus = BuildStatus.UNDEFINED;
            lastBuildStatus = result switch
            {
                "ABORTED" => BuildStatus.ABORTED,
                "FAILURE" => BuildStatus.FAILED,
                "STABLE" => BuildStatus.STABLE,
                "SUCCESS" => BuildStatus.SUCCESSFUL,
                "UNSTABLE" => BuildStatus.UNSTABLE,
                _ => BuildStatus.UNDEFINED,
            };

            var apiUrl2 = $"{BaseUrl}/{folderPath}/job/{jobName}/lastBuild/consoleText";
            var response2 = await client.GetAsync(apiUrl2);
            var consoleText = await response2.Content.ReadAsStringAsync();

            return new JobBuild(lastBuildStatus, consoleText);
        }

        /// <summary>
        /// Deletes a job.
        /// </summary>
        /// <param name="fullProjectName">The full project name.</param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task DeleteJob(string fullProjectName)
        {
            var (folderPath, jobName) = await CheckProject(fullProjectName);

            using var client = new HttpClient();
            await AuthorizeJenkins(client);

            var apiUrl = $"{BaseUrl}/{folderPath}/job/{jobName}";
            var response = await client.DeleteAsync(apiUrl);
            EvaluateResponseMessage(response, $"Deleting jenkins job '{fullProjectName}' encountered a problem: ");
        }

        /// <summary>
        /// Authorizes Jenkins user.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <returns></returns>
        /// <exception cref="JenkinsNetException"></exception>
        private async Task AuthorizeJenkins(HttpClient httpClient)
        {
            var url = new Uri(BaseUrl);
            var isJenkinsUp = Ping(url.Host, url.Port, TimeSpan.FromSeconds(5));
            if (!isJenkinsUp)
                throw new JenkinsNetException($"Could not connect to jenkins server!");

            // Set the authorization header
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Credentials);

            // Set the crumb header if CSRF protection is enabled
            var crumbUrl = $"{BaseUrl}/crumbIssuer/api/xml?xpath=concat(//crumbRequestField,\":\",//crumb)";
            var crumbResponse = await httpClient.GetAsync(crumbUrl);
            crumbResponse.EnsureSuccessStatusCode();
            var crumb = await crumbResponse.Content.ReadAsStringAsync();
            var crumbItems = crumb.Trim().Split(':');
            httpClient.DefaultRequestHeaders.Add(crumbItems[0], crumbItems[1]);
        }

        /// <summary>
        /// Generates string content by XML document object.
        /// </summary>
        /// <param name="xmlDoc">The XML document</param>
        /// <returns>Returns string content for HTTP requests.</returns>
        private static StringContent GenerateStringContent(XDocument xmlDoc)
        {
            var xml = xmlDoc.Document.ToString();
            var content = new StringContent(xml);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            return content;
        }

        /// <summary>
        /// Evaluates HTTP response message object.
        /// </summary>
        /// <param name="response">The response message object.</param>
        /// <param name="errorPrefix">Error message prefix for the JenkinsNetException</param>
        /// <exception cref="JenkinsNetException"></exception>
        private static void EvaluateResponseMessage(HttpResponseMessage response, string errorPrefix)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = response.StatusCode.ToString();
                if (response.Headers.TryGetValues("X-Error", out IEnumerable<string> values))
                    errorMsg = values.FirstOrDefault();

                throw new JenkinsNetException(errorPrefix + errorMsg);
            }
        }

        enum PipelineDefinition
        {
            PipelineScript,
            PipelineScriptFromScm
        }

        /// <summary>
        /// Returns an XML document representing the project template configuration.
        /// </summary>
        /// <returns>XML document representing the project template configuration.</returns>
        /// <exception cref="FileNotFoundException"></exception>
        private static XDocument GetJobTemplateConfig(PipelineDefinition pipelineDef)
        {
            // fyi: need to set file to "Copy always to output"
            var curLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string templateXmlConfigFilePath;
            if (pipelineDef == PipelineDefinition.PipelineScript)
            {
                templateXmlConfigFilePath = Path.Combine(curLocation, "job.item.xml");
            }
            else if (pipelineDef == PipelineDefinition.PipelineScriptFromScm)
            {
                templateXmlConfigFilePath = Path.Combine(curLocation, "job.item-scm.xml");
            }
            else
            {
                throw new ArgumentException("Invalid paramter value");
            }

            if (!File.Exists(templateXmlConfigFilePath))
                throw new FileNotFoundException("Template Jenkins config file not found!");

            return XDocument.Load(templateXmlConfigFilePath);
        }

        /// <summary>
        /// Updates timer trigger specification.
        /// Note: Checks also a prospective crontab specification to see if it could benefit from balanced hashes.
        /// </summary>
        /// <param name="crontabSpec"></param>
        private static void SetCrontabSpec(XDocument jobConfigNode, string crontabSpec)
        {
            var hashifiedCronTabSpec = HashifyCrontabSpec(crontabSpec);
            if (!string.IsNullOrEmpty(hashifiedCronTabSpec))
                crontabSpec = hashifiedCronTabSpec;

            const string hudsonTriggerTimerTriggerNode = "hudson.triggers.TimerTrigger";
            const string pipelineTriggerPropertyNode = "org.jenkinsci.plugins.workflow.job.properties.PipelineTriggersJobProperty";
            const string timerTriggerSpecNode = "spec";
            const string triggersNode = "triggers";
            const string propertiesNode = "properties";

            var timerTriggerElement = jobConfigNode.Document
                .Descendants(hudsonTriggerTimerTriggerNode)
                .FirstOrDefault();
            var isCronExpressionStringEmpty = string.IsNullOrEmpty(crontabSpec);

            if (timerTriggerElement == null)
            {
                if (!isCronExpressionStringEmpty)
                {
                    var specElement = new XElement(timerTriggerSpecNode, crontabSpec);
                    var timerTrigger = new XElement(hudsonTriggerTimerTriggerNode, specElement);
                    var trigger = new XElement(triggersNode, timerTrigger);
                    var pipelineTrigger = new XElement(pipelineTriggerPropertyNode, trigger);
                    var propertiesElement = jobConfigNode.Document
                        .Descendants(propertiesNode)
                        .FirstOrDefault();

                    propertiesElement.RemoveNodes(); // Ensure nothing else is there but the hudson timer trigger
                    propertiesElement.Add(pipelineTrigger);
                }
            }
            else
            {
                if (isCronExpressionStringEmpty)
                {
                    var triggerElement = jobConfigNode.Document
                        .Descendants(pipelineTriggerPropertyNode)
                        .FirstOrDefault();
                    triggerElement?.Remove();
                }
                else
                {
                    var timerTriggerSpecElement = timerTriggerElement
                        .Descendants(timerTriggerSpecNode)
                        .SingleOrDefault();
                    // Updates the spec attribute with the new value
                    timerTriggerSpecElement?.SetValue(crontabSpec);
                }
            }
        }

        /// <summary>
        /// Sets pipeline script.
        /// </summary>
        /// <param name="jobConfigNode">XML document representing the project configuration.</param>
        /// <param name="pipelineScript">The pipeline script</param>
        private static void SetPipelineScript(XDocument jobConfigNode, string pipelineScript)
        {
            var scriptElement = jobConfigNode.Document.XPathSelectElement("flow-definition/definition/script");
            scriptElement?.SetValue(pipelineScript);
        }

        private static void SetPipelineScriptFromScm(XDocument jobConfigNode, string repositoryUrl, string branchSpec, string scriptPath)
        {
            var repositoryUrlElement = jobConfigNode.Document.XPathSelectElement("flow-definition/definition/scm/userRemoteConfigs/hudson.plugins.git.UserRemoteConfig/url");
            repositoryUrlElement?.SetValue(repositoryUrl);
            var branchSpecElement = jobConfigNode.Document.XPathSelectElement("flow-definition/definition/scm/branches/hudson.plugins.git.BranchSpec/name");
            branchSpecElement?.SetValue(branchSpec);
            var scriptPathElement = jobConfigNode.Document.XPathSelectElement("flow-definition/definition/scriptPath");
            scriptPathElement?.SetValue(scriptPath);
        }

        /// <summary>
        /// Pings server.
        /// </summary>
        /// <param name="host">host name</param>
        /// <param name="port">port</param>
        /// <param name="timeout">timeout</param>
        /// <returns></returns>
        private static bool Ping(string host, int port, TimeSpan timeout)
        {
            using var tcp = new TcpClient();
            var result = tcp.BeginConnect(host, port, null, null);
            var wait = result.AsyncWaitHandle;
            var ok = true;

            try
            {
                if (!result.AsyncWaitHandle.WaitOne(timeout, false))
                {
                    tcp.Close();
                    ok = false;
                }

                tcp.EndConnect(result);
            }
            catch
            {
                ok = false;
            }
            finally
            {
                wait.Close();
            }
            return ok;
        }

        /// <summary>
        /// Checks a prospective crontab specification to see if it could benefit from balanced hashes.
        /// </summary>
        /// <param name="spec">a (legal) spec</param>
        /// <returns>a similar spec that uses a hash, if such a transformation is necessary; null if it is OK as is</returns>
        private static string HashifyCrontabSpec(string spec)
        {
            if (string.IsNullOrEmpty(spec))
                return null;

            // Modified from https://github.com/jenkinsci/jenkins/blob/master/core/src/main/java/hudson/scheduler/CronTab.java
            if (spec.Contains("H"))
            {
                // if someone is already using H, presumably he knows what it is, so a warning is likely false positive
                return null;
            }
            else if (spec.StartsWith("*/"))
            {
                // "*/15 ...." (every N minutes) to hash
                return "H" + spec.Substring(1);
            }
            else if (Regex.Match(spec, "\\d+ .+").Success)
            {
                // "0 ..." (certain minute) to hash
                return "H " + spec.Substring(spec.IndexOf(' ') + 1);
            }
            else
            {
                var r = new Regex("0(,(\\d+)(,\\d+)*)( .+)");
                var m = r.Match(spec);
                if (m.Success)
                {
                    // 0,15,30,45 to H/15
                    int period = int.Parse(m.Groups[2].Value);
                    if (period > 0)
                    {
                        var b = new StringBuilder();
                        for (int i = period; i < 60; i += period)
                        {
                            b.Append(',').Append(i);
                        }
                        if (b.ToString().Equals(m.Groups[1].Value))
                        {
                            return "H/" + period + m.Groups[4].Value;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Removes XML declaration from XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns></returns>
        private static string RemoveXmlDeclaration(string xml)
        {
            return Regex.Replace(xml, @"<\?xml[^\>]*\?>", string.Empty);
        }
    }
}
