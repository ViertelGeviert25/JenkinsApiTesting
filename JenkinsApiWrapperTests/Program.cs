﻿using JenkinsApiWrapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace JenkinsApiWrapperTests
{
    internal class Program
    {
        #region Fields
        private static readonly string CS_JENKINS_BASE_URL = ConfigurationManager.AppSettings["baseUrl"];
        private static readonly string CS_JENKINS_USERNAME = ConfigurationManager.AppSettings["username"];
        private static readonly string CS_JENKINS_API_TOKEN = ConfigurationManager.AppSettings["apiToken"];
        #endregion


        public static async void TestDoesJobExist()
        {
            var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
            var jobName = "wuseldusel/dummy";
            var jobExists = await jenkins.DoesJobExist(jobName);
            Console.WriteLine(jobExists);
        }

        public static async void TestCrateJob()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var jobName = "wuseldusel/dummy";
                var pipelineScript = $@"pipeline {{
    agent any
    stages {{
        stage('Default') {{
            steps {{
                script {{
                    bat '""{@"H:\\dev\\JenkinsApiTesting\\DoSomething\\bin\\Debug\\DoSomething.exe"}""'
                }}
        }}
    }}
}}
}}";
                var jobProperties = new JenkinsJobProperties()
                {
                    CrontabSpecification = "H H 1,4 1-11 *"
                };
                await jenkins.CreatePipelineProject(jobName, pipelineScript, jobProperties);
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestCreatePipelineProjectFromScm()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var jobName = "wuseldusel/dummy";
                var parameters = new Dictionary<string, object>
                {
                    { "TASK_ID", "abcd" }
                };
                var jobProperties = new JenkinsJobProperties()
                {
                    CrontabSpecification = "0 0 1,4 1-11 *",
                    DaysToKeepLogs = 10,
                    NumToKeepLogs = 5,
                    Parameters = parameters
                };
                await jenkins.CreatePipelineProjectFromScm(jobName, "https://github.com/ViertelGeviert25/JenkinsApiTesting", jobProperties, "*/master", "jenkinsfile.groovy");
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestDeleteJob()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var jobName = "wuseldusel/dummy";
                await jenkins.DeleteJob(jobName);
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestBuildJob()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var jobName = "wuseldusel/dummy scm";
                await jenkins.BuildProject(jobName);
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestBuildJobWithParameters()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var jobName = "wuseldusel/dummy";
                var parameters = new Dictionary<string, object>
                {
                    { "TASK_ID", "eeff" }
                };
                await jenkins.BuildProjectWithParameters(jobName, parameters);
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestScheduleCronJob()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                await jenkins.SetCrontabSpecification("wuseldusel/dummy scm", "0 0 1,13 1-11 *");

                // expectation: H 0 1,12 1-11 *
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestRemoveCronJob()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                await jenkins.RemoveCrontabSpecification("wuseldusel/dummy");
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestCancelCurrentBuild()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                await jenkins.CancelCurrentBuild("wuseldusel/dummy");
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestGetJobDescription()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var jobDescription = await jenkins.GetProjectDescription("wuseldusel/dummy");
                Console.WriteLine(jobDescription);
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestUpdateJobDescription()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                await jenkins.UpdateProjectDescription("wuseldusel/dummy", "my very important job description 2");
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestRetrieveBuilds()
        {
            try
            {
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var builds = await jenkins.RetrieveBuilds("wuseldusel/dummy");
                Console.WriteLine(builds.ToString());
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async void TestGetLastBuildStatus()
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();
                var jenkins = new JenkinsApi(CS_JENKINS_BASE_URL, CS_JENKINS_USERNAME, CS_JENKINS_API_TOKEN);
                var lastBuildStatus = await jenkins.GetLastBuildStatus("wuseldusel/dummy scm", TimeSpan.FromMinutes(1));
                Console.WriteLine(lastBuildStatus.Status);
                Console.WriteLine(stopWatch.ElapsedMilliseconds);
                stopWatch.Stop();
            }
            catch (JenkinsNetException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void Main(string[] args)
        {
            //TestCreatePipelineProjectFromScm();
            //TestCreatePipelineProjectFromScm();
            TestBuildJobWithParameters();
            //TestGetLastBuildStatus();
            //TestScheduleCronJob();


            Console.WriteLine("Done.");
            Console.ReadKey();
        }
    }
}
