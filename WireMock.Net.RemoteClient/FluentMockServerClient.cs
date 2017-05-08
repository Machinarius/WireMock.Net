using System;
using JetBrains.Annotations;
using WireMock.Matchers.Request;
using WireMock.Server;

namespace WireMock.Net.RemoteClient
{
    class FluentMockServerClient
    {
        private string hostname;
        private int port;

        [PublicAPI]
        public FluentMockServerClient(string hostname, int port)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            this.hostname = hostname;
            this.port = port;
        }

        [PublicAPI]
        public bool DeleteMapping(Guid guid)
        {
            throw new NotImplementedException();
        }

        [PublicAPI]
        public void AddGlobalProcessingDelay(TimeSpan delay)
        {
            throw new NotImplementedException();
        }

        [PublicAPI]
        public void SetBasicAuthentication([NotNull] string username, [NotNull] string password)
        {
            throw new NotImplementedException();
        }

        [PublicAPI]
        public IRespondWithAProvider Given(IRequestMatcher requestMatcher)
        {
            throw new NotImplementedException();
        }
    }
}
