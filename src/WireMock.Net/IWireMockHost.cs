using System;
using WireMock.Matchers.Request;
using WireMock.Server;

namespace WireMock
{
    /// <summary>
    /// Abstracts away the difference between a local WireMock server and
    /// a WireMock server client, exposing only the common functionality 
    /// implemented by both
    /// </summary>
    public interface IWireMockHost
    {
        /// <summary>
        /// Configures a mock stub on the host once the response is set 
        /// </summary>
        /// <param name="requestMatcher">The request to match the response to</param>
        /// <returns>A fluent API to configure the request mapping and the response</returns>
        IRespondWithAProvider Given(IRequestMatcher requestMatcher);

        /// <summary>
        /// Attempts to delete a mapping from the WireMock host
        /// </summary>
        /// <param name="guid">The guid that identifies the mapping</param>
        /// <returns>True if the mapping was deleted, false otherwise</returns>
        bool DeleteMapping(Guid guid);
    }
}
