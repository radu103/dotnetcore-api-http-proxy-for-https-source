using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TenantProxy.Services
{
    public class TenantXSODataProxyService
    {
        private string _baseHref = string.Empty;
        private string _newBaseHref = string.Empty;

        private string _tenantId = string.Empty;
        private string _tenantColumnName = string.Empty;

        private string _filterTenantTemplate = "{TENANT_COLUMN_NAME} eq '{TENANT_ID}'";
        
        private IMemoryCache _memoryCache = null;
        private TimeSpan expirationInterval = TimeSpan.FromMinutes(1);        

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

        public string NewBaseHref
        {
            get
            {
                return _newBaseHref;
            }
            set
            {
                if (value != null)
                    _newBaseHref = value.ToString();
                else
                    _newBaseHref = string.Empty;
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

        public TenantXSODataProxyService(IMemoryCache memoryCache, string tenantId = "", string tenantColumnName = "tenant", string baseHref = "", string newBaseHref = "")
        {
            if (!string.IsNullOrEmpty(baseHref))
            {
                _baseHref = baseHref;
            }

            if (!string.IsNullOrEmpty(newBaseHref))
            {
                _newBaseHref = newBaseHref;
            }

            _tenantId = tenantId;
            _tenantColumnName = tenantColumnName;

            _memoryCache = memoryCache;
        }

        public string GetServiceDefinition()
        {
            if (string.IsNullOrEmpty(_baseHref))
            {
                throw new Exception("BaseHref not set !");
            }

            var url = _baseHref + "?$format=json";

            var md5 = ComputeMD5Hash(url);
            if (_memoryCache != null)
            {
                string cacheValue = string.Empty;
                _memoryCache.TryGetValue(md5, out cacheValue);
                if (!string.IsNullOrEmpty(cacheValue))
                {
                    return cacheValue;
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

            if (_memoryCache != null)
            {
                var cacheEntry = _memoryCache.GetOrCreate(md5, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = expirationInterval;
                    return result;
                });
            }

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

            var md5 = ComputeMD5Hash(url);
            if (_memoryCache != null)
            {
                string cacheValue = string.Empty;
                _memoryCache.TryGetValue(md5, out cacheValue);
                if (!string.IsNullOrEmpty(cacheValue))
                {
                    return cacheValue;
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

            var reducedJson = string.Empty;
            dynamic jsonObject = JsonConvert.DeserializeObject(result);

            if (jsonObject.d.results != null)
            {
                reducedJson = JsonConvert.SerializeObject(jsonObject.d.results).ToString();
            }
            else
            {
                reducedJson = JsonConvert.SerializeObject(jsonObject.d).ToString();
            }

            // replace base href
            if (!string.IsNullOrEmpty(_newBaseHref))
            {
                reducedJson = reducedJson.Replace(_baseHref, _newBaseHref);
            }

            if (_memoryCache != null)
            {
                var cacheEntry = _memoryCache.GetOrCreate(md5, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = expirationInterval;
                    return reducedJson;
                });
            }

            return reducedJson;
        }

        private string ComputeMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
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
