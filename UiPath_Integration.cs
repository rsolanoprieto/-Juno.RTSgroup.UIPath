using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class UiPathFunction
{
    [FunctionName("StartUiPathProcess")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        // Replace with your tenant name, client ID, and user key
        string tenantName = "DefaultTenant";
        string clientId = "8DEv1AMNXczW3y4U15LL3jYf62jK93n5";
        string userKey = "VqUp4aXACCksLWhWQ8VivEw0szG-w1reZUbg-a5wah1eB";

        // Replace with your Orchestrator URL, process name, and environment name
        string orchestratorUrl = "https://cloud.uipath.com/robottechnologysolutionssl/DefaultTenant/orchestrator_";
        string processName = "OP_001_020_ValidacionDocumentos";
        string environmentName = "environment_name";

        string token = await GetAccessTokenAsync(tenantName, clientId, userKey, orchestratorUrl);
        if (token == null)
        {
            return new BadRequestObjectResult("Failed to get access token.");
        }

        bool processStarted = await StartProcessAsync(token, orchestratorUrl, processName, environmentName);
        if (processStarted)
        {
            return new OkObjectResult("UiPath process started successfully.");
        }
        else
        {
            return new BadRequestObjectResult("Failed to start UiPath process.");
        }
    }

    private static async Task<string> GetAccessTokenAsync(string tenantName, string clientId, string userKey, string orchestratorUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            string tokenEndpoint = $"{orchestratorUrl}/identity_/connect/token";
            string tokenRequestBody = $"grant_type=client_credentials&client_id={clientId}&client_secret={userKey}&scope=Orchestrator.{tenantName}";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            requestMessage.Content = new StringContent(tokenRequestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                JObject responseObject = JObject.Parse(responseContent);
                return responseObject["access_token"].ToString();
            }
            else
            {
                return null;
            }
        }
    }

    private static async Task<bool> StartProcessAsync(string accessToken, string orchestratorUrl, string processName, string environmentName)
    {
        using (HttpClient client = new HttpClient())
        {
            string startJobEndpoint = $"{orchestratorUrl}/odata/Jobs/UiPath.Server.Configuration.OData.StartJobs";

            JObject startInfo = new JObject
            {
                ["startInfo"] = new JObject
                {
                    ["ReleaseKey"] = await GetReleaseKeyAsync(accessToken, orchestratorUrl, processName),
                    ["Strategy"] = "All",
                    ["RobotIds"] = new JArray(),
                    ["NoOfRobots"]
