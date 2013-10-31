using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Two10.Azure.SelfDestruct
{

    public class SelfDestruct
    {

        /// <summary>
        /// Deletes the current Cloud Service instance after a countdown!
        /// </summary>
        /// <param name="publishSettingsFilePath">The path to a publish settings file (http://go.microsoft.com/fwlink/?LinkId=254432)</param>
        /// <param name="countdown">Timespan to count down</param>
        /// <returns></returns>
        public static Task DeleteInstance(string publishSettingsFilePath, TimeSpan countdown)
        {
            return Task.Delay(countdown).ContinueWith((x) => {
                DeleteInstance(publishSettingsFilePath);
            });
        }

        /// <summary>
        /// Deletes the current Cloud Service instance
        /// </summary>
        /// <param name="publishSettingsFilePath">The path to a publish settings file (http://go.microsoft.com/fwlink/?LinkId=254432)</param>
        public static void DeleteInstance(string publishSettingsFilePath)
        {
            if (string.IsNullOrWhiteSpace(publishSettingsFilePath)) throw new ArgumentNullException("publishSettingsFilePath");
            if (!RoleEnvironment.IsAvailable) throw new ApplicationException("Not running in Azure");

            var cert = GetCert(publishSettingsFilePath);
            if (null == cert) throw new ApplicationException("No certificate");

            var subscriptionId = GetSubscriptionId(publishSettingsFilePath);
            if (string.IsNullOrWhiteSpace(subscriptionId)) throw new ApplicationException("No SubscriptionId");

            var hostedService = GetHostedServiceName(subscriptionId, cert);
            if (string.IsNullOrWhiteSpace(hostedService)) throw new ApplicationException("Could not find role details");

            DeleteRoleInstances(subscriptionId, hostedService, cert, new string[] { RoleEnvironment.CurrentRoleInstance.Id });
        }

        private static string GetHostedServiceName(string subscriptionId, X509Certificate2 certificate)
        {
            var hostedServices = GetHostedServices(subscriptionId, certificate);
            var deploymentId = RoleEnvironment.DeploymentId;

            foreach (var hostedService in hostedServices)
            {
                var xe = GetHostedServiceProperties(subscriptionId, certificate, hostedService);
                if (xe == null) continue;

                var deploymentXElements = xe.Elements(XName.Get("Deployments", "http://schemas.microsoft.com/windowsazure")).Elements(XName.Get("Deployment", "http://schemas.microsoft.com/windowsazure")).ToList();
                if (deploymentXElements == null || deploymentXElements.Count == 0) continue;

                foreach (var deployment in deploymentXElements)
                {
                    var currentDeploymentId = deployment.Element(XName.Get("PrivateID", "http://schemas.microsoft.com/windowsazure")).Value;
                    if (currentDeploymentId == deploymentId) return hostedService;
                }
            }
            return null;
        }


        private static X509Certificate2 GetCert(string filename)
        {
            var managementCertbase64string = XDocument.Load(filename).Descendants("Subscription").First().Attribute("ManagementCertificate").Value;
            return new X509Certificate2(Convert.FromBase64String(managementCertbase64string));
        }

        private static string GetSubscriptionId(string filename)
        {
            return XDocument.Load(filename).Descendants("Subscription").First().Attribute("Id").Value;
        }


        private static string versionNumber = "2013-08-01";
        private static XNamespace wa = "http://schemas.microsoft.com/windowsazure";
        private static string getHostedServicesOperationUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices";
        private static string getHostedServicePropertyOperationUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices/{1}?embed-detail=true";
        private static string deleteRoleInstancesUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/production/roleinstances/?comp=delete";

        private static IEnumerable<string> GetHostedServices(string subscriptionId, X509Certificate2 certificate)
        {
            string uri = string.Format(getHostedServicesOperationUrlTemplate, subscriptionId);
            XElement xe = PerformGetOperation(uri, certificate);
            if (xe == null) return new string[] { };
            var serviceNameElements = xe.Elements().Elements(XName.Get("ServiceName", wa.ToString()));
            return serviceNameElements.Select(x => x.Value);
        }

        private static XElement GetHostedServiceProperties(string subscriptionId, X509Certificate2 certificate, string hostedServiceName)
        {
            var uri = string.Format(getHostedServicePropertyOperationUrlTemplate, subscriptionId, hostedServiceName);
            return PerformGetOperation(uri, certificate);
        }

        private static HttpWebRequest CreateHttpWebRequest(Uri uri, X509Certificate2 certificate, string httpWebRequestMethod)
        {
            var httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
            httpWebRequest.Method = httpWebRequestMethod;
            httpWebRequest.Headers.Add("x-ms-version", versionNumber);
            httpWebRequest.ClientCertificates.Add(certificate);
            httpWebRequest.ContentType = "application/xml";
            return httpWebRequest;
        }

        private static XElement PerformGetOperation(string uri, X509Certificate2 certificate)
        {
            var requestUri = new Uri(uri);
            var httpWebRequest = CreateHttpWebRequest(requestUri, certificate, "GET");
            using (var response = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                return XElement.Load(responseStream);
            }
        }

        private static string PerformPostOperation(string uri, X509Certificate2 certificate, string body)
        {
            var requestUri = new Uri(uri);
            var httpWebRequest = CreateHttpWebRequest(requestUri, certificate, "POST");

            var requestBody = Encoding.UTF8.GetBytes(body);
            using (var stream = httpWebRequest.GetRequestStream())
            {
                stream.Write(requestBody, 0, requestBody.Length);
            }

            using (var resp = httpWebRequest.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }

        private static string DeleteRoleInstances(string subscriptionId, string cloudServiceName, X509Certificate2 cert, string[] roleInstanceNames)
        {
            var uri = string.Format(deleteRoleInstancesUrlTemplate, subscriptionId, cloudServiceName);

            var requestBodyFormat = @"<RoleInstances xmlns=""http://schemas.microsoft.com/windowsazure"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">{0}</RoleInstances>";
            var namesXml = string.Join("", roleInstanceNames.Select(x => string.Format("<Name>{0}</Name>", x)));

            return PerformPostOperation(uri, cert, string.Format(requestBodyFormat, namesXml));
        }

    }
}