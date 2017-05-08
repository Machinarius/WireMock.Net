using System;
using WireMock.Server;

namespace WireMock.Client.Internals
{
    internal class ResponseStoreRespondWithAProvider : IRespondWithAProvider
    {
        public event EventHandler ResponseConfigured;

        public string Title { get; private set; }

        public int Priority { get; private set; }

        public Guid? Guid { get; private set; }

        public IResponseProvider ResponseProvider { get; private set; }

        public IRespondWithAProvider AtPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public void RespondWith(IResponseProvider provider)
        {
            ResponseProvider = provider;
            ResponseConfigured?.Invoke(this, null);
        }

        public IRespondWithAProvider WithGuid(Guid guid)
        {
            Guid = guid;
            return this;
        }

        public IRespondWithAProvider WithGuid(string guid)
        {
            var parseResult = new Guid(guid);
            return WithGuid(parseResult);
        }

        public IRespondWithAProvider WithTitle(string title)
        {
            Title = title;
            return this;
        }
    }
}
