using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using WireMock.Http;
using WireMock.Matchers;
using WireMock.Matchers.Request;
using WireMock.RequestBuilders;
using WireMock.Validation;
using WireMock.Owin;

namespace WireMock.Server
{
    /// <summary>
    /// The fluent mock server.
    /// </summary>
    public partial class FluentMockServer : IDisposable
    {
        private readonly IOwinSelfHost _httpServer;
        private readonly object _syncRoot = new object();
        private readonly WireMockMiddlewareOptions _options = new WireMockMiddlewareOptions();

        /// <summary>
        /// Gets the ports.
        /// </summary>
        /// <value>
        /// The ports.
        /// </value>
        [PublicAPI]
        public List<int> Ports { get; }

        /// <summary>
        /// Gets the urls.
        /// </summary>
        [PublicAPI]
        public string[] Urls { get; }

        /// <summary>
        /// Gets the mappings.
        /// </summary>
        [PublicAPI]
        public IEnumerable<Mapping> Mappings
        {
            get
            {
                lock (((ICollection)_options.Mappings).SyncRoot)
                {
                    return new ReadOnlyCollection<Mapping>(_options.Mappings);
                }
            }
        }

        #region Start/Stop
        /// <summary>
        /// Starts the specified settings.
        /// </summary>
        /// <param name="settings">The FluentMockServerSettings.</param>
        /// <returns>The <see cref="FluentMockServer"/>.</returns>
        [PublicAPI]
        public static FluentMockServer Start(FluentMockServerSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            return new FluentMockServer(settings);
        }

        /// <summary>
        /// Start this FluentMockServer.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="ssl">The SSL support.</param>
        /// <returns>The <see cref="FluentMockServer"/>.</returns>
        [PublicAPI]
        public static FluentMockServer Start([CanBeNull] int? port = 0, bool ssl = false)
        {
            return new FluentMockServer(new FluentMockServerSettings
            {
                Port = port,
                UseSSL = ssl
            });
        }

        /// <summary>
        /// Start this FluentMockServer.
        /// </summary>
        /// <param name="urls">The urls to listen on.</param>
        /// <returns>The <see cref="FluentMockServer"/>.</returns>
        [PublicAPI]
        public static FluentMockServer Start(params string[] urls)
        {
            Check.NotEmpty(urls, nameof(urls));

            return new FluentMockServer(new FluentMockServerSettings
            {
                Urls = urls
            });
        }

        /// <summary>
        /// Start this FluentMockServer with the admin interface.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="ssl">The SSL support.</param>
        /// <returns>The <see cref="FluentMockServer"/>.</returns>
        [PublicAPI]
        public static FluentMockServer StartWithAdminInterface(int? port = 0, bool ssl = false)
        {
            return new FluentMockServer(new FluentMockServerSettings
            {
                Port = port,
                UseSSL = ssl,
                StartAdminInterface = true
            });
        }

        /// <summary>
        /// Start this FluentMockServer with the admin interface.
        /// </summary>
        /// <param name="urls">The urls.</param>
        /// <returns>The <see cref="FluentMockServer"/>.</returns>
        [PublicAPI]
        public static FluentMockServer StartWithAdminInterface(params string[] urls)
        {
            Check.NotEmpty(urls, nameof(urls));

            return new FluentMockServer(new FluentMockServerSettings
            {
                Urls = urls,
                StartAdminInterface = true
            });
        }

        /// <summary>
        /// Start this FluentMockServer with the admin interface and read static mappings.
        /// </summary>
        /// <param name="urls">The urls.</param>
        /// <returns>The <see cref="FluentMockServer"/>.</returns>
        [PublicAPI]
        public static FluentMockServer StartWithAdminInterfaceAndReadStaticMappings(params string[] urls)
        {
            Check.NotEmpty(urls, nameof(urls));

            return new FluentMockServer(new FluentMockServerSettings
            {
                Urls = urls,
                StartAdminInterface = true,
                ReadStaticMappings = true
            });
        }

        private FluentMockServer(FluentMockServerSettings settings)
        {
            if (settings.Urls != null)
            {
                Urls = settings.Urls;
            }
            else
            {
                int port = settings.Port > 0 ? settings.Port.Value : PortUtil.FindFreeTcpPort();
                Urls = new[] { (settings.UseSSL == true ? "https" : "http") + "://localhost:" + port + "/" };
            }

#if NET45
            _httpServer = new OwinSelfHost(_options, Urls);
#else
            _httpServer = new AspNetCoreSelfHost(_options, Urls);
#endif
            Ports = _httpServer.Ports;

            _httpServer.StartAsync();

            if (settings.StartAdminInterface == true)
            {
                InitAdmin();
            }

            if (settings.ReadStaticMappings == true)
            {
                ReadStaticMappings();
            }
        }

        /// <summary>
        /// Stop this server.
        /// </summary>
        [PublicAPI]
        public void Stop()
        {
            _httpServer?.StopAsync();
        }
        #endregion

        /// <summary>
        /// Adds the catch all mapping.
        /// </summary>
        [PublicAPI]
        public void AddCatchAllMapping()
        {
            Given(Request.Create().WithPath("/*").UsingAnyVerb())
                .WithGuid(Guid.Parse("90008000-0000-4444-a17e-669cd84f1f05"))
                .AtPriority(1000)
                .RespondWith(new DynamicResponseProvider(request => new ResponseMessage { StatusCode = 404, Body = "No matching mapping found" }));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_httpServer != null && _httpServer.IsStarted)
            {
                _httpServer.StopAsync();
            }
        }

        /// <summary>
        /// Resets LogEntries and Mappings.
        /// </summary>
        [PublicAPI]
        public void Reset()
        {
            ResetLogEntries();

            ResetMappings();
        }

        /// <summary>
        /// Resets the Mappings.
        /// </summary>
        [PublicAPI]
        public void ResetMappings()
        {
            lock (((ICollection)_options.Mappings).SyncRoot)
            {
                _options.Mappings = _options.Mappings.Where(m => m.Provider is DynamicResponseProvider).ToList();
            }
        }

        /// <summary>
        /// Deletes the mapping.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        [PublicAPI]
        public bool DeleteMapping(Guid guid)
        {
            lock (((ICollection)_options.Mappings).SyncRoot)
            {
                // Check a mapping exists with the same GUID, if so, remove it.
                var existingMapping = _options.Mappings.FirstOrDefault(m => m.Guid == guid);
                if (existingMapping != null)
                {
                    _options.Mappings.Remove(existingMapping);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// The add request processing delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        [PublicAPI]
        public void AddGlobalProcessingDelay(TimeSpan delay)
        {
            lock (_syncRoot)
            {
                _options.RequestProcessingDelay = delay;
            }
        }

        /// <summary>
        /// Allows the partial mapping.
        /// </summary>
        [PublicAPI]
        public void AllowPartialMapping()
        {
            lock (_syncRoot)
            {
                _options.AllowPartialMapping = true;
            }
        }

        /// <summary>
        /// Sets the basic authentication.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        [PublicAPI]
        public void SetBasicAuthentication([NotNull] string username, [NotNull] string password)
        {
            Check.NotNull(username, nameof(username));
            Check.NotNull(password, nameof(password));

            string authorization = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            _options.AuthorizationMatcher = new RegexMatcher("^(?i)BASIC " + authorization + "$");
        }

        /// <summary>
        /// The given.
        /// </summary>
        /// <param name="requestMatcher">The request matcher.</param>
        /// <returns>The <see cref="IRespondWithAProvider"/>.</returns>
        [PublicAPI]
        public IRespondWithAProvider Given(IRequestMatcher requestMatcher)
        {
            return new RespondWithAProvider(RegisterMapping, requestMatcher);
        }

        /// <summary>
        /// The register mapping.
        /// </summary>
        /// <param name="mapping">
        /// The mapping.
        /// </param>
        private void RegisterMapping(Mapping mapping)
        {
            lock (((ICollection)_options.Mappings).SyncRoot)
            {
                // Check a mapping exists with the same GUID, if so, remove it first.
                DeleteMapping(mapping.Guid);

                _options.Mappings.Add(mapping);
            }
        }
    }
}