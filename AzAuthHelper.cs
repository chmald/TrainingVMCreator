using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Newtonsoft.Json;
using System;
using System.IO;

namespace TrainingVMCreator
{
    public class AzAuthHelper
    {
        public AzAuthHelper() {}

        public static ClientSecretCredential AcquireAzureCredentials(string authFile)
        {
            try
            {
                var authFileContent = JsonConvert.DeserializeObject<AuthFile>(File.ReadAllText(authFile));
                return new ClientSecretCredential(authFileContent.TenantId, authFileContent.ClientId, authFileContent.ClientSecret);
            }
            catch(Exception ex)
            {
                string errorCode = ex.Message;
                throw;
            }
        }

        public static ArmClient LogIntoAzure(ClientSecretCredential credentials)
        {
            return new ArmClient(credentials);
        }
    }
}
