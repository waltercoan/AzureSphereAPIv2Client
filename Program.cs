using System;

namespace apiazuresphere
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    // You install the Microsoft.Identity.Client reference by using Nuget,
    // starting at https://www.nuget.org/packages/Microsoft.Identity.Client.
    // Follow the instructions to install using Package Manager.
    using Microsoft.Identity.Client;
    using Newtonsoft.Json;

    class Program
    {
        private readonly List<string> Scopes = new List<string>() { "https://sphere.azure.net/api/user_impersonation" };
        private const string ClientApplicationId = "0B1C8F7E-28D2-4378-97E2-7D7D63F7C87F";
        public const string Tenant = "7d71c83c-ccdf-45b7-b3c9-9c41b94406d9";
        private static readonly Uri AzureSphereApiUri = new Uri("https://prod.core.sphere.azure.net/");
        private static string accessToken = String.Empty;
        private static CancellationTokenSource cancellationTokenSource;
        private static int Main()
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();

                Program program = new Program();

                program.ExecuteAsync(cancellationTokenSource.Token)
                    .GetAwaiter()
                    .GetResult();

                program.CliFunction();
                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return -1;
            }
            return 0;
        }

        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            IPublicClientApplication publicClientApp = PublicClientApplicationBuilder
                .Create(ClientApplicationId)
                .WithAuthority(AzureCloudInstance.AzurePublic, Tenant)
                .WithRedirectUri("http://localhost")
                .Build();

            AuthenticationResult authResult = await publicClientApp.AcquireTokenInteractive(Scopes)
                .ExecuteAsync();

            accessToken = authResult.AccessToken;

        }

        private static async Task<string> GetAsync(string accessToken, string relativeUrl, CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                Uri uri = new Uri(AzureSphereApiUri, relativeUrl);

                using (HttpResponseMessage response = await client.GetAsync(uri, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
        private void CliFunction(){
            var tenant = String.Empty;
            List<string> deviceList = new List<string>();
            while(true){
                
                var apiurl = String.Empty;
                var result = String.Empty;
                Console.WriteLine("Azure Sphere API Interactive Test Client");
                Console.WriteLine("API URL https://prod.core.sphere.azure.net");
                Console.WriteLine("1-> Get Tenant [/v2/tenants]");
                Console.WriteLine("2-> Get Devices [/v2/tenants/{tenantid}/devices]");
                Console.WriteLine("3-> Get Images [/v2/tenants/{tenantid}/devices/{deviceid}/images]");
                Console.WriteLine("4-> Error Reporting [/v2/tenants/{tenantId}/getDeviceInsights]");
                var option = Console.ReadLine();
                switch (option)
                {
                    case "1":
                        apiurl = String.Format("v2/tenants");
                        result = GetAsync(accessToken, apiurl, cancellationTokenSource.Token).GetAwaiter().GetResult();
                        dynamic jsonTenant  = JsonConvert.DeserializeObject(result);
                        tenant = jsonTenant[0].Id;
                        
                        Console.WriteLine("Tenant ID:{0}...", tenant.Substring(0,4));
                        Console.WriteLine("Name:{0}", jsonTenant[0].Name);
                        Console.WriteLine("Roles:{0}", jsonTenant[0].Roles);
                    break; 
                    case "2":
                        if (tenant == String.Empty){
                            Console.WriteLine("Use option 1 tenant first!");
                            continue;
                        }
                        apiurl = String.Format("v2/tenants/{0}/devices",tenant);
                        result = GetAsync(accessToken, apiurl, cancellationTokenSource.Token).GetAwaiter().GetResult();
                        dynamic jsonDevices  = JsonConvert.DeserializeObject(result);
                        deviceList.Clear();
                        foreach(var device in jsonDevices.Items){
                            string deviceId = device.DeviceId;
                            deviceList.Add(deviceId);
                            Console.WriteLine("Device ID: {0}...", deviceId.Substring(0,4));
                        }
                        
                    break;
                    case "3":
                        if (tenant == String.Empty){
                            Console.WriteLine("Use option 1 tenant first!");
                            continue;
                        }
                        foreach(var device in deviceList){
                            apiurl = String.Format("v2/tenants/{0}/devices/{1}/images",tenant,device);
                            result = GetAsync(accessToken, apiurl, cancellationTokenSource.Token).GetAwaiter().GetResult();
                            Console.WriteLine("++Device: {0}...", device.Substring(0,4));
                            dynamic jsonDeviceImage  = JsonConvert.DeserializeObject(result);
                            foreach(var image in jsonDeviceImage.Items){
                                Console.WriteLine("Name: {0}",image.Name);
                            }
                            Console.WriteLine("--Device: {0}...", device.Substring(0,4));
                        }
                    break;
                    case "4":
                        if (tenant == String.Empty){
                            Console.WriteLine("Use option 1 tenant first!");
                            continue;
                        }
                        
                        apiurl = String.Format("v2/tenants/{0}/getDeviceInsights",tenant);
                        result = GetAsync(accessToken, apiurl, cancellationTokenSource.Token).GetAwaiter().GetResult();
                        dynamic jsonError  = JsonConvert.DeserializeObject(result);
                        foreach(var error in jsonError.Items){
                            Console.WriteLine(new String('-',20));
                            string devId = error.DeviceId;
                            Console.WriteLine("Device Id: {0}...", devId.Substring(0,4));
                            string desc = error.Description;
                            Console.WriteLine("Description: {0}...", desc.Substring(0,15));
                            string eventClass = error.EventClass;
                            Console.WriteLine("EventClass: {0}", eventClass);
                            string eventCat=error.EventCategory;
                            Console.WriteLine("EventCategory: {0}", eventCat);
                            string eventType = error.EventType;
                            Console.WriteLine("EventType: {0}", eventType);
                            double unixTimeStamp = Convert.ToDouble(error.StartTimestampInUnix);
                            System.DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);
                            dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
                            Console.WriteLine("StartTimestampInUnix: {0}", dtDateTime);
                            Console.WriteLine(new String('-',20));
                        }
                        
                    break;
                    default:
                        Console.WriteLine("Opção inválida!");
                    break;
                }
            }
        }
    }
}
