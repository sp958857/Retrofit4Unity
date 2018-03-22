using System;
using System.Collections.Generic;
using System.Net;

#if NETFX_CORE
using Windows.System.Threading;
#else
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
#endif

namespace CI.HttpClient
{
    public class HttpClientRequest
    {
        private const int DEFAULT_BLOCK_SIZE = 10000;
        private const int DEFAULT_TIMEOUT = 100000;
        private const int DEFAULT_READ_WRITE_TIMEOUT = 300000;
        private const bool DEFAULT_KEEP_ALIVE = true;

        /// <summary>
        /// Chunk size when downloading data. Default is 10,000 bytes (10 kilobytes)
        /// </summary>
        public int DownloadBlockSize { get; set; }

        /// <summary>
        /// Chunk size when uploading data. Default is 10,000 bytes (10 kilobytes)
        /// </summary>
        public int UploadBlockSize { get; set; }

        /// <summary>
        /// Timeout value in milliseconds for opening read / write streams to the server. The default value is 100,000 milliseconds (100 seconds). Set by the system for Windows Store
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Timeout value in milliseconds when reading or writing data to / from the server. The default value is 300,000 milliseconds (5 minutes). Set by the system for Windows Store
        /// </summary>
        public int ReadWriteTimeout { get; set; }

#if !NETFX_CORE
        /// <summary>
        /// The cache policy that will be associated with requests. Not available for Windows Store
        /// </summary>
        public RequestCachePolicy Cache { get; set; }

        /// <summary>
        /// The collection of security certificates that will be associated with requests. Not available for Windows Store
        /// </summary>
        public X509CertificateCollection Certificates { get; set; }
#endif

        /// <summary>
        /// Cookies that will be associated with requests
        /// </summary>
        public CookieContainer Cookies { get; set; }

        /// <summary>
        /// Authentication information that will be associated with requests
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Indicates whether to make a persistent connection to the Internet resource. The default is true. Set by the system for Windows Store
        /// </summary>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// Specifies a collection of the name/value pairs that make up the standard HTTP headers
        /// </summary>
        public IDictionary<HttpRequestHeader, string> Headers { get; set; }

        /// <summary>
        /// Specifies a collection of the name/value pairs that make up user defined HTTP headers
        /// </summary>
        public IDictionary<string, string> CustomHeaders { get; set; }

        /// <summary>
        /// Proxy information that will be associated with requests
        /// </summary>
        public IWebProxy Proxy { get; set; }

        public HttpWebRequest Request
        {
            get { return _request; }
        }
        private readonly HttpWebRequest _request;
        private HttpHandler _httpHandler;
        private Action<HttpResponseMessage<string>> _responseCallback;
        private Action<UploadStatusMessage> _uploadStatusCallback;
        private IHttpContent _httpContent;
        private void SetUp()
        {
            DownloadBlockSize = DEFAULT_BLOCK_SIZE;
            UploadBlockSize = DEFAULT_BLOCK_SIZE;
            Timeout = DEFAULT_TIMEOUT;
            ReadWriteTimeout = DEFAULT_READ_WRITE_TIMEOUT;
            KeepAlive = DEFAULT_KEEP_ALIVE;
            Headers = new Dictionary<HttpRequestHeader, string>();
            CustomHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        /// Provides a class for sending HTTP requests and receiving HTTP responses from a resource identified by a URI
        /// </summary>
        public HttpClientRequest(Uri uri, Action<HttpResponseMessage<string>> responseCallback):base()
        {
            SetUp();
            _request = CreateRequest(uri);
            _httpHandler = new HttpHandler(_request);
            _responseCallback = responseCallback;
        }

        /// <summary>
        /// Aborts all requests on this instance
        /// </summary>
        public void Abort()
        {
           
                _request.Abort();
        }
        public void SetMethod(HttpAction httpAction)
        {
            _request.Method = httpAction.ToString().ToUpper();
        }

        public void SetHttpContent(IHttpContent httpContent)
        {
            _httpContent = httpContent;
        }

        public void SetProxy(IWebProxy iWebProxy)
        {
            Proxy = iWebProxy;
            if (Proxy != null)
            {
                _request.Proxy = Proxy;
            }
        }
        public void SetUploadStatusCallback(Action<UploadStatusMessage> uploadStatusCallback)
        {
            _uploadStatusCallback = uploadStatusCallback;
        }
        public void Send()
        {
            try
            {
                if (_httpContent == null)
                {
                    _httpHandler.HandleStringResponseRead(_responseCallback);

                }
                else
                {
                    _httpHandler.SetContentHeaders(_httpContent);
                    _httpHandler.HandleRequestWrite(_httpContent, _uploadStatusCallback, UploadBlockSize);
                    _httpHandler. HandleStringResponseRead(_responseCallback);
                }
            }
            catch (Exception e)
            {
                _httpHandler.RaiseErrorResponse(_responseCallback, e);
            }
        }


        private HttpWebRequest CreateRequest(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            DisableWriteStreamBuffering(request);
            AddCache(request);
            AddCertificates(request);
            AddCookies(request);
            AddCredentials(request);
            AddKeepAlive(request);
            AddHeaders(request);
            AddProxy(request);
            AddTimeouts(request);
            return request;
        }

        private void DisableWriteStreamBuffering(HttpWebRequest request)
        {
#if !NETFX_CORE
            request.AllowWriteStreamBuffering = false;
#endif
        }

        private void AddCache(HttpWebRequest request)
        {
#if !NETFX_CORE
            if (Cache != null)
            {
                request.CachePolicy = Cache;
            }
#endif
        }

        private void AddCertificates(HttpWebRequest request)
        {
#if !NETFX_CORE
            if (Certificates != null)
            {
                request.ClientCertificates = Certificates;
            }
#endif
        }

        private void AddCookies(HttpWebRequest request)
        {
            if (Cookies != null)
            {
                request.CookieContainer = Cookies;
            }
        }

        private void AddCredentials(HttpWebRequest request)
        {
            if (Credentials != null)
            {
                request.Credentials = Credentials;
            }
        }

        private void AddKeepAlive(HttpWebRequest request)
        {
#if !NETFX_CORE
            request.KeepAlive = KeepAlive;
#endif
        }

        private void AddHeaders(HttpWebRequest request)
        {
            if (Headers != null)
            {
                foreach (KeyValuePair<HttpRequestHeader, string> header in Headers)
                {
#if NETFX_CORE
                    request.Headers[header.Key] = header.Value;
#else
                    switch (header.Key)
                    {
                        case HttpRequestHeader.Accept:
                            request.Accept = header.Value;
                            break;
                        case HttpRequestHeader.Connection:
                            request.Connection = header.Value;
                            break;
                        case HttpRequestHeader.ContentLength:
                            throw new NotSupportedException("Content Length is set automatically");
                        case HttpRequestHeader.ContentType:
                            throw new NotSupportedException("Content Type is set automatically");
                        case HttpRequestHeader.Expect:
                            request.Expect = header.Value;
                            break;
                        case HttpRequestHeader.Date:
                            throw new NotSupportedException("Date is automatically set by the system to the current date");
                        case HttpRequestHeader.Host:
                            throw new NotSupportedException("Host is automatically set by the system to current host information");
                        case HttpRequestHeader.IfModifiedSince:
                            request.IfModifiedSince = DateTime.Parse(header.Value);
                            break;
                        case HttpRequestHeader.Range:
                            int range = int.Parse(header.Value);
                            request.AddRange(range);
                            break;
                        case HttpRequestHeader.Referer:
                            request.Referer = header.Value;
                            break;
                        case HttpRequestHeader.TransferEncoding:
                            throw new NotSupportedException("Transfer Encoding is not currently supported");
                        case HttpRequestHeader.UserAgent:
                            request.UserAgent = header.Value;
                            break;
                        default:
                            request.Headers.Add(header.Key, header.Value);
                            break;
                    }
#endif
                }
            }

            if (CustomHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in CustomHeaders)
                {
#if NETFX_CORE
                    request.Headers[header.Key] = header.Value;
#else
                    request.Headers.Add(header.Key, header.Value);
#endif
                }
            }
        }

        private void AddProxy(HttpWebRequest request)
        {
            if (Proxy != null)
            {
                request.Proxy = Proxy;
            }
        }

        private void AddTimeouts(HttpWebRequest request)
        {
#if !NETFX_CORE
            request.Timeout = Timeout;
            request.ReadWriteTimeout = ReadWriteTimeout;
#endif
        }

        private void RaiseErrorResponse<T>(Action<HttpResponseMessage<T>> action, Exception exception)
        {
            if (action != null)
            {
                action(new HttpResponseMessage<T>()
                {
                    Exception = exception,
                });
            }
        }
    }
}