using System;
using System.IO;
using System.Net;

namespace TenantProxy.Services
{
    public class TenantXSODataProxyService
    {
        private string _baseHref = string.Empty;
        private string _tenantId = string.Empty;
        private string _tenantColumnName = string.Empty;

        private string _filterTenantTemplate = "{TENANT_COLUMN_NAME} eq '{TENANT_ID}'";

        public string BaseHref {
            get
            {
                return _baseHref;
            }
            set
            {
                if(value != null)
                    _baseHref = value.ToString();
                else
                    _baseHref = string.Empty;
            }
        }

        public string TenantId
        {
            get
            {
                return _tenantId;
            }
            set
            {
                if (value != null)
                    _tenantId = value.ToString();
                else
                    _tenantId = string.Empty;
            }
        }

        public string TenantColumnName
        {
            get
            {
                return _tenantColumnName;
            }
            set
            {
                if (value != null)
                    _tenantColumnName = value.ToString();
                else
                    _tenantColumnName = string.Empty;
            }
        }

        public string FilterTenantTemplate
        {
            get
            {
                return _filterTenantTemplate;
            }
            set
            {
                if (value != null)
                    _filterTenantTemplate = value.ToString();
                else
                    _filterTenantTemplate = string.Empty;
            }
        }

        public TenantXSODataProxyService(string tenantId = "", string baseHref = "", string tenantColumnName = "tenant")
        {
            if (!string.IsNullOrEmpty(baseHref))
            {
                _baseHref = baseHref;
            }

            _tenantId = tenantId;
            _tenantColumnName = tenantColumnName;
        }

        public string GetServiceDefinition()
        {
            if (string.IsNullOrEmpty(_baseHref))
            {
                throw new Exception("BaseHref not set !");
            }

            var request = GetRequest(_baseHref + "?$format=json");

            var response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            string strStatus = ((HttpWebResponse)response).StatusDescription;
            StreamReader streamReader = new StreamReader(responseStream);
            var result = streamReader.ReadToEnd();

            streamReader.Close();
            responseStream.Close();
            response.Close();

            return result;
        }

        public string GetEntitySetData(string entitySet, string originalRequest = "")
        {
            if (string.IsNullOrEmpty(_baseHref))
            {
                throw new Exception("BaseHref not set !");
            }

            if (string.IsNullOrEmpty(entitySet))
            {
                throw new Exception("EntitySet not named !");
            }

            var url = _baseHref + "/" + entitySet;

            if (string.IsNullOrEmpty(originalRequest))
            {
                var filterStr = _filterTenantTemplate.Replace("{TENANT_COLUMN_NAME}", _tenantColumnName);
                filterStr = filterStr.Replace("{TENANT_ID}", _tenantId);
                url += "?$format=json&$filter=" + filterStr;
            }
            else
            if(originalRequest.IndexOf("$filter=") >= 0)
            {
                var aux = originalRequest.Split(new char[] { '&', '?' }, StringSplitOptions.RemoveEmptyEntries);
                for(var i=0; i < aux.Length; i++)
                {
                    if(aux[i].ToLower().IndexOf("$filter=") >= 0)
                    {
                        var filterStr = _filterTenantTemplate.Replace("{TENANT_COLUMN_NAME}", _tenantColumnName);
                        filterStr = filterStr.Replace("{TENANT_ID}", _tenantId);
                        aux[i] += " and " + filterStr;
                    }
                }
                
                originalRequest = "?" + string.Join('&', aux);
                
                url += originalRequest;
                
                if (url.ToLower().IndexOf("$format=json") < 0)
                {
                    url += "&$format=json";
                }
            }
            else
            {
                url = _baseHref +  "/" + entitySet + originalRequest;

                if (url.ToLower().IndexOf("$format=json") < 0)
                {
                    url += "&$format=json";
                }
            }

            var request = GetRequest(url);

            var response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            string strStatus = ((HttpWebResponse)response).StatusDescription;
            StreamReader streamReader = new StreamReader(responseStream);
            var result = streamReader.ReadToEnd();

            streamReader.Close();
            responseStream.Close();
            response.Close();

            return result;
        }

        private HttpWebRequest GetRequest(string remoteUrl)
        {
            CookieContainer cookieContainer = new CookieContainer();
            Uri url = new Uri(remoteUrl);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            request.Method = WebRequestMethods.Http.Get;
            request.UserAgent = "TenantODataProxy";
            request.KeepAlive = true;
            request.CookieContainer = cookieContainer;
            request.PreAuthenticate = true;
            request.AllowAutoRedirect = false;
            request.Accept = "application/json";

            return request;
        }
    }
}
