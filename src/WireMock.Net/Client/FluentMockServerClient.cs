using System;
using System.Net.Http;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using WireMock.Client.Internals;
using WireMock.Matchers.Request;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Serialization;
using WireMock.Server;

namespace WireMock.Client
{
    /// <summary>
    /// A client that uses the WireMock Admin API to drive a remote WireMock server
    /// </summary>
    /// <remarks>
    /// This is a work in progress
    /// </remarks>
    public class FluentMockServerClient : IWireMockHost
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        private string hostname;
        private int port;

        private HttpClient httpClient;

        /// <summary>
        /// Construct a new WireMock Admin API client
        /// </summary>
        /// <param name="hostname">The hostname the server is running on</param>
        /// <param name="port">The port the server is listening on</param>
        [PublicAPI]
        public FluentMockServerClient(string hostname, int port)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            this.hostname = hostname;
            this.port = port;

            httpClient = new HttpClient();
        }

        /// <summary>
        /// Deletes a mapping from the server
        /// </summary>
        /// <remarks>
        /// This method assumes the server always deletes the mapping unless a non-success
        /// status code is returned
        /// </remarks>
        /// <param name="guid">The guid of the mapping to delete</param>
        /// <returns>True if the mapping was deleted, false otherwise</returns>
        [PublicAPI]
        public bool DeleteMapping(Guid guid)
        {
            var deleteMessage = new HttpRequestMessage(HttpMethod.Delete, $"http://{hostname}:{port}/__admin/mappings/{guid}");
            var response = httpClient.SendAsync(deleteMessage).Result;

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            } 
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Configures a mock stub on the server once a response is configured
        /// </summary>
        /// <param name="requestMatcher">The request to match the response with</param>
        /// <returns>A fluent API to specify the response and mapping attributes</returns>
        [PublicAPI]
        public IRespondWithAProvider Given(IRequestMatcher requestMatcher)
        {
            var request = requestMatcher as Request;
            if (request == null)
            {
                throw new InvalidOperationException($"The {nameof(requestMatcher)} argument value is of an unknown class - " +
                    $"custom {nameof(IRequestMatcher)} implementations are not supported");
            }

            var respondWithAProvider = new ResponseStoreRespondWithAProvider();
            respondWithAProvider.ResponseConfigured += (source, args) =>
            {
                SendMappingData(request, respondWithAProvider);
            };

            return respondWithAProvider;
        }

        private void SendMappingData([NotNull] Request request, 
            [NotNull] ResponseStoreRespondWithAProvider responseStore)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (responseStore == null)
            {
                throw new ArgumentNullException(nameof(responseStore));
            }

            if (responseStore.ResponseProvider == null)
            {
                throw new InvalidOperationException("No response was configured");
            }

            var response = responseStore.ResponseProvider as Response;
            if (response == null)
            {
                throw new InvalidOperationException($"The provided response is invalid: Custom {nameof(IResponseProvider)} implementations are not supported");
            }

            Guid mappingGuid;
            if (responseStore.Guid.HasValue)
            {
                mappingGuid = responseStore.Guid.Value;
            }
            else
            {
                mappingGuid = Guid.NewGuid();
            }

            var mappingModel = MappingConverter.ToMappingModel(request, response, mappingGuid, responseStore.Title, responseStore.Priority);
            var mappingJson = JsonConvert.SerializeObject(mappingModel, _serializerSettings);
            
            var mappingMessage = new HttpRequestMessage(HttpMethod.Post, $"http://{hostname}:{port}/__admin/mappings");
            mappingMessage.Content = new StringContent(mappingJson, Encoding.UTF8, "application/json");

            var responseMessage = httpClient.SendAsync(mappingMessage).Result;
            responseMessage.EnsureSuccessStatusCode();
        }
    }
}
