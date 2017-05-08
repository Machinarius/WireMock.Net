using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NFluent;
using WireMock.Client;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace WireMock.Net.Tests
{
    public class FluentMockServerClientTests
    {
        FluentMockServerClient client;
        FluentMockServer server;

        int defaultMappingsCount;
        int serverPort;

        public FluentMockServerClientTests()
        {
            server = FluentMockServer.StartWithAdminInterface();
            serverPort = server.Ports.First();

            client = new FluentMockServerClient("localhost", serverPort);
            defaultMappingsCount = server.Mappings.Count();
        }

        [Fact]
        [Trait("Component", "Client")]
        public async Task CallingGivenWithValidParametersShouldSendTheMapppingToTheServer()
        {
            var mappingGuid = Guid.NewGuid();
            var mappingTitle = "testMapping";
            var requestBody = "Hello World";
            var requestPath = "/test";
            var testResponseContent = "Success";
            var testRequest = Request.Create().WithPath(requestPath).UsingGet().WithBody(requestBody);
            var testResponse = Response.Create().WithBody(testResponseContent).WithSuccess();

            client
                .Given(testRequest)
                .WithGuid(mappingGuid)
                .WithTitle(mappingTitle)
                .RespondWith(testResponse);

            Check.That(server.Mappings.Any(mapping => mapping.Guid == mappingGuid)).IsTrue();

            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{serverPort}{requestPath}");
            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Check.Equals(response.StatusCode, 200);
            Check.Equals(responseContent, testResponseContent);
        }

        [Fact]
        [Trait("Component", "Client")]
        public void CallingDeleteShouldEraseTheDesiredMapping()
        {
            var tempMappingGuid = Guid.NewGuid();
            var mappingTitle = "testMapping";
            var requestBody = "Hello World";
            var requestPath = "/test";
            var testResponseContent = "Success";
            var testRequest = Request.Create().WithPath(requestPath).UsingGet().WithBody(requestBody);
            var testResponse = Response.Create().WithBody(testResponseContent).WithSuccess();

            client
                .Given(testRequest)
                .WithGuid(tempMappingGuid)
                .WithTitle(mappingTitle)
                .RespondWith(testResponse);

            var permanentMappingGuid = Guid.NewGuid();
            client
                .Given(testRequest)
                .WithGuid(permanentMappingGuid)
                .WithTitle(mappingTitle)
                .RespondWith(testResponse);

            Check.That(server.Mappings.Where(mapping => !mapping.IsAdminInterface)).HasSize(2);
            Check.That(server.Mappings.Any(mapping => mapping.Guid == tempMappingGuid)).IsTrue();
            Check.That(server.Mappings.Any(mapping => mapping.Guid == permanentMappingGuid)).IsTrue();

            client.DeleteMapping(tempMappingGuid);
            Check.That(server.Mappings.Where(mapping => !mapping.IsAdminInterface)).HasSize(1);
            Check.That(server.Mappings.Any(mapping => mapping.Guid == tempMappingGuid)).IsFalse();
            Check.That(server.Mappings.Any(mapping => mapping.Guid == permanentMappingGuid)).IsTrue();
        }
    }
}
