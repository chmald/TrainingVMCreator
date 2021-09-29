using Microsoft.Identity.Client;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;

namespace TrainingVMCreator
{
    public class AzAuthHelper
    {
        public AzAuthHelper() {}

        public static AzureCredentials AcquireAzureCredentials(string authFile)
        {
            try
            {
                return SdkContext.AzureCredentialsFactory.FromFile(authFile);
            }
            catch(MsalServiceException ex)
            {
                string errorCode = ex.ErrorCode;
                throw;
            }
            catch (OperationCanceledException ex)
            {
                throw;
            }
            catch (MsalClientException ex)
            {
                throw;
            }
        }

        public static IAzure LogIntoAzure(AzureCredentials credentials)
        {
            return Azure.Configure().WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic).Authenticate(credentials).WithDefaultSubscription();
        }
    }
}
