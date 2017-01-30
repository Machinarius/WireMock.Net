using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Logging;
using WireMock.Matchers;
using WireMock.Matchers.Request;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace WireMock.Server
{
    /// <summary>
    /// The fluent mock server.
    /// </summary>
    public partial class FluentMockServer
    {
        private const string AdminMappings = "/__admin/mappings";
        private const string AdminRequests = "/__admin/requests";
        private readonly RegexMatcher _adminMappingsGuidPathMatcher = new RegexMatcher(@"^\/__admin\/mappings\/(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$");
        private readonly RegexMatcher _adminRequestsGuidPathMatcher = new RegexMatcher(@"^\/__admin\/requests\/(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$");

        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        private void InitAdmin()
        {
            // __admin/mappings
            Given(Request.Create().WithPath(AdminMappings).UsingGet()).RespondWith(new DynamicResponseProvider(MappingsGet));
            Given(Request.Create().WithPath(AdminMappings).UsingPost()).RespondWith(new DynamicResponseProvider(MappingsPost));
            Given(Request.Create().WithPath(AdminMappings).UsingDelete()).RespondWith(new DynamicResponseProvider(MappingsDelete));

            // __admin/mappings/{guid}
            Given(Request.Create().WithPath(_adminMappingsGuidPathMatcher).UsingGet()).RespondWith(new DynamicResponseProvider(MappingGet));
            Given(Request.Create().WithPath(_adminMappingsGuidPathMatcher).UsingPut().WithHeader("Content-Type", "application/json")).RespondWith(new DynamicResponseProvider(MappingPut));
            Given(Request.Create().WithPath(_adminMappingsGuidPathMatcher).UsingDelete()).RespondWith(new DynamicResponseProvider(MappingDelete));


            // __admin/requests
            Given(Request.Create().WithPath(AdminRequests).UsingGet()).RespondWith(new DynamicResponseProvider(RequestsGet));
            Given(Request.Create().WithPath(AdminRequests).UsingDelete()).RespondWith(new DynamicResponseProvider(RequestsDelete));

            // __admin/request/{guid}
            Given(Request.Create().WithPath(_adminRequestsGuidPathMatcher).UsingGet()).RespondWith(new DynamicResponseProvider(RequestGet));
        }

        #region Mapping
        private ResponseMessage MappingGet(RequestMessage requestMessage)
        {
            Guid guid = Guid.Parse(requestMessage.Path.Substring(AdminMappings.Length + 1));
            var mapping = Mappings.FirstOrDefault(m => !(m.Provider is DynamicResponseProvider) && m.Guid == guid);

            if (mapping == null)
                return new ResponseMessage { StatusCode = 404, Body = "Mapping not found" };

            var model = ToMappingModel(mapping);

            return ToJson(model);
        }

        private ResponseMessage MappingPut(RequestMessage requestMessage)
        {
            Guid guid = Guid.Parse(requestMessage.Path.TrimStart(AdminMappings.ToCharArray()));
            var mappingModel = JsonConvert.DeserializeObject<MappingModel>(requestMessage.Body);

            if (mappingModel.Request == null)
                return new ResponseMessage { StatusCode = 400, Body = "Request missing" };

            if (mappingModel.Response == null)
                return new ResponseMessage { StatusCode = 400, Body = "Response missing" };

            var requestBuilder = InitRequestBuilder(mappingModel);
            var responseBuilder = InitResponseBuilder(mappingModel);

            Given(requestBuilder)
                .WithGuid(guid)
                .RespondWith(responseBuilder);

            return new ResponseMessage { Body = "Mapping updated" };
        }

        private ResponseMessage MappingDelete(RequestMessage requestMessage)
        {
            Guid guid = Guid.Parse(requestMessage.Path.Substring(AdminMappings.Length + 1));

            DeleteMapping(guid);

            return new ResponseMessage { Body = "Mapping removed" };
        }
        #endregion Mapping

        #region Mappings
        private ResponseMessage MappingsGet(RequestMessage requestMessage)
        {
            var result = new List<MappingModel>();
            foreach (var mapping in Mappings.Where(m => !(m.Provider is DynamicResponseProvider)))
            {
                var model = ToMappingModel(mapping);
                result.Add(model);
            }

            return ToJson(result);
        }

        private ResponseMessage MappingsPost(RequestMessage requestMessage)
        {
            var mappingModel = JsonConvert.DeserializeObject<MappingModel>(requestMessage.Body);

            if (mappingModel.Request == null)
                return new ResponseMessage { StatusCode = 400, Body = "Request missing" };

            if (mappingModel.Response == null)
                return new ResponseMessage { StatusCode = 400, Body = "Response missing" };

            var requestBuilder = InitRequestBuilder(mappingModel);
            var responseBuilder = InitResponseBuilder(mappingModel);

            IRespondWithAProviderGuid respondProvider = Given(requestBuilder);

            if (mappingModel.Guid != null && mappingModel.Guid != Guid.Empty)
                respondProvider = respondProvider.WithGuid(mappingModel.Guid.Value);

            respondProvider.RespondWith(responseBuilder);

            return new ResponseMessage { Body = "Mapping added" };
        }

        private ResponseMessage MappingsDelete(RequestMessage requestMessage)
        {
            ResetMappings();

            return new ResponseMessage { Body = "Mappings deleted" };
        }
        #endregion Mappings

        #region Request
        private ResponseMessage RequestGet(RequestMessage requestMessage)
        {
            Guid guid = Guid.Parse(requestMessage.Path.Substring(AdminRequests.Length + 1));
            var entry = LogEntries.FirstOrDefault(r => !r.RequestMessage.Path.StartsWith("/__admin/") && r.Guid == guid);

            if (entry == null)
                return new ResponseMessage { StatusCode = 404, Body = "Request not found" };

            var model = ToLogEntryModel(entry);

            return ToJson(model);
        }
        #endregion Request

        #region Requests
        private ResponseMessage RequestsGet(RequestMessage requestMessage)
        {
            var result = LogEntries
                .Where(r => !r.RequestMessage.Path.StartsWith("/__admin/"))
                .Select(ToLogEntryModel);

            return ToJson(result);
        }

        private LogEntryModel ToLogEntryModel(LogEntry logEntry)
        {
            return new LogEntryModel
            {
                Guid = logEntry.Guid,
                Request = new LogRequestModel
                {
                    DateTime = logEntry.RequestMessage.DateTime,
                    Url = logEntry.RequestMessage.Path,
                    AbsoleteUrl = logEntry.RequestMessage.Url,
                    Query = logEntry.RequestMessage.Query,
                    Method = logEntry.RequestMessage.Method,
                    Body = logEntry.RequestMessage.Body,
                    Headers = logEntry.RequestMessage.Headers,
                    Cookies = logEntry.RequestMessage.Cookies
                },
                Response = new LogResponseModel
                {
                    StatusCode = logEntry.ResponseMessage.StatusCode,
                    Body = logEntry.ResponseMessage.Body,
                    BodyOriginal = logEntry.ResponseMessage.BodyOriginal,
                    Headers = logEntry.ResponseMessage.Headers
                }
            };
        }

        private ResponseMessage RequestsDelete(RequestMessage requestMessage)
        {
            ResetLogEntries();

            return new ResponseMessage { Body = "Requests deleted" };
        }
        #endregion Requests

        private IRequestBuilder InitRequestBuilder(MappingModel mappingModel)
        {
            IRequestBuilder requestBuilder = Request.Create();
            string path = mappingModel.Request.Path as string;
            if (path != null)
                requestBuilder = requestBuilder.WithPath(path);
            else
            {
                JToken pathToken = (JToken)mappingModel.Request.Path;
                PathModel pathModel = pathToken.ToObject<PathModel>();
                if (pathModel?.Matchers != null)
                    requestBuilder = requestBuilder.WithPath(pathModel.Matchers.Select(Map).ToArray());
            }

            if (mappingModel.Request.Methods != null)
                requestBuilder = requestBuilder.UsingVerb(mappingModel.Request.Methods);
            else
                requestBuilder = requestBuilder.UsingAnyVerb();

            if (mappingModel.Request.Headers != null)
            {
                foreach (var headerModel in mappingModel.Request.Headers.Where(h => h.Matchers != null))
                {
                    requestBuilder = requestBuilder.WithHeader(headerModel.Name, headerModel.Matchers.Select(Map).ToArray());
                }
            }

            if (mappingModel.Request.Cookies != null)
            {
                foreach (var cookieModel in mappingModel.Request.Cookies.Where(c => c.Matchers != null))
                {
                    requestBuilder = requestBuilder.WithCookie(cookieModel.Name, cookieModel.Matchers.Select(Map).ToArray());
                }
            }

            if (mappingModel.Request.Params != null)
            {
                foreach (var paramModel in mappingModel.Request.Params.Where(p => p.Values != null))
                {
                    requestBuilder = requestBuilder.WithParam(paramModel.Name, paramModel.Values.ToArray());
                }
            }

            if (mappingModel.Request.Body?.Matcher != null)
            {
                var bodyMatcher = Map(mappingModel.Request.Body.Matcher);
                requestBuilder = requestBuilder.WithBody(bodyMatcher);
            }

            return requestBuilder;
        }

        private IResponseBuilder InitResponseBuilder(MappingModel mappingModel)
        {
            IResponseBuilder responseBuilder = Response.Create();

            if (mappingModel.Response.StatusCode.HasValue)
                responseBuilder = responseBuilder.WithStatusCode(mappingModel.Response.StatusCode.Value);

            if (mappingModel.Response.Headers != null)
                responseBuilder = responseBuilder.WithHeaders(mappingModel.Response.Headers);

            if (mappingModel.Response.Body != null)
                responseBuilder = responseBuilder.WithBody(mappingModel.Response.Body);
            else if (mappingModel.Response.BodyAsJson != null)
                responseBuilder = responseBuilder.WithBodyAsJson(mappingModel.Response.BodyAsJson);
            else if (mappingModel.Response.BodyAsBase64 != null)
                responseBuilder = responseBuilder.WithBodyAsBase64(mappingModel.Response.BodyAsBase64);

            if (mappingModel.Response.UseTransformer)
                responseBuilder = responseBuilder.WithTransformer();
            return responseBuilder;
        }

        private MappingModel ToMappingModel(Mapping mapping)
        {
            var request = (Request)mapping.RequestMatcher;
            var response = (Response)mapping.Provider;

            var pathMatchers = request.GetRequestMessageMatchers<RequestMessagePathMatcher>();
            var headerMatchers = request.GetRequestMessageMatchers<RequestMessageHeaderMatcher>();
            var cookieMatchers = request.GetRequestMessageMatchers<RequestMessageCookieMatcher>();
            var paramsMatchers = request.GetRequestMessageMatchers<RequestMessageParamMatcher>();
            var bodyMatcher = request.GetRequestMessageMatcher<RequestMessageBodyMatcher>();
            var methodMatcher = request.GetRequestMessageMatcher<RequestMessageMethodMatcher>();

            return new MappingModel
            {
                Guid = mapping.Guid,
                Request = new RequestModel
                {
                    Path = new PathModel
                    {
                        Matchers = pathMatchers != null ? Map(pathMatchers.Where(m => m.Matchers != null).SelectMany(m => m.Matchers)) : null
                    },
                    Methods = methodMatcher != null ? methodMatcher.Methods : new[] { "any" },
                    Headers = headerMatchers?.Select(hm => new HeaderModel
                    {
                        Name = hm.Name,
                        Matchers = Map(hm.Matchers)
                    }).ToList(),
                    Cookies = cookieMatchers?.Select(hm => new CookieModel
                    {
                        Name = hm.Name,
                        Matchers = Map(hm.Matchers)
                    }).ToList(),
                    Params = paramsMatchers?.Select(hm => new ParamModel
                    {
                        Name = hm.Key,
                        Values = hm.Values?.ToList()
                    }).ToList(),
                    Body = new BodyModel
                    {
                        Matcher = bodyMatcher != null ? Map(bodyMatcher.Matcher) : null
                    }
                },
                Response = new ResponseModel
                {
                    StatusCode = response.ResponseMessage.StatusCode,
                    Headers = response.ResponseMessage.Headers,
                    Body = response.ResponseMessage.Body,
                    UseTransformer = response.UseTransformer
                }
            };
        }

        private MatcherModel[] Map([CanBeNull] IEnumerable<IMatcher> matchers)
        {
            return matchers?.Select(Map).Where(x => x != null).ToArray();
        }

        private MatcherModel Map([CanBeNull] IMatcher matcher)
        {
            if (matcher == null)
                return null;

            return new MatcherModel
            {
                Name = matcher.GetType().Name,
                Pattern = matcher.GetPattern()
            };
        }

        private IMatcher Map([CanBeNull] MatcherModel matcher)
        {
            if (matcher == null)
                return null;

            switch (matcher.Name)
            {
                case "RegexMatcher":
                    return new RegexMatcher(matcher.Pattern);

                case "JsonPathMatcher":
                    return new JsonPathMatcher(matcher.Pattern);

                case "XPathMatcher":
                    return new XPathMatcher(matcher.Pattern);

                default:
                    return new WildcardMatcher(matcher.Pattern, matcher.IgnoreCase == true);
            }
        }

        private ResponseMessage ToJson<T>(T result)
        {
            return new ResponseMessage
            {
                Body = JsonConvert.SerializeObject(result, _settings),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}